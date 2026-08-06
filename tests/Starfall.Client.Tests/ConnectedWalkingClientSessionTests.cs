using System.Net;
using ChronoFall.Network.Transport;
using Starfall.Client.Networking;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Combat;
using Starfall.Protocol.Compatibility;
using Starfall.Protocol.Monsters;
using Starfall.Protocol.Movement;
using Starfall.Protocol.Networking;

namespace Starfall.Client.Tests;

public sealed class ConnectedWalkingClientSessionTests
{
    [Fact]
    public void Connected_options_require_literal_loopback_and_complete_arguments()
    {
        ConnectedClientLaunchOptions options = ConnectedClientLaunchOptions.Parse(
        [
            "--connect-address", "127.0.0.1",
            "--connect-port", "7777",
            "--join-ticket-file", "ticket.txt",
        ]);
        Assert.Equal(IPAddress.Loopback, options.Address);
        Assert.Equal(7777, options.Port);
        Assert.Throws<ArgumentException>(() => ConnectedClientLaunchOptions.Parse(
        [
            "--connect-address", "localhost", "--connect-port", "7777", "--join-ticket-file", "ticket",
        ]));
        Assert.Throws<ArgumentException>(() => ConnectedClientLaunchOptions.Parse(
        [
            "--connect-address", "192.0.2.1", "--connect-port", "7777", "--join-ticket-file", "ticket",
        ]));
    }

    [Fact]
    public void Session_admits_accepts_latest_snapshot_and_sends_monotonic_commands()
    {
        var transport = new ScriptedTransport();
        var session = new ConnectedWalkingClientSession(transport, "ticket");
        transport.OnPoll = handler =>
        {
            if (transport.PollCount == 1)
            {
                handler.Connected(transport.Peer, new NetworkEndpoint("127.0.0.1", 7777));
                handler.PacketReceived(
                    transport.Peer,
                    WorldJoinAdmissionCodec.EncodeAccepted(
                        new WorldJoinAccepted(
                            StarfallGameplayProtocol.CurrentVersion,
                            new GameplaySessionId(Guid.NewGuid()))),
                    NetworkDelivery.ReliableOrdered,
                    StarfallNetworkChannels.Admission);
                handler.PacketReceived(
                    transport.Peer,
                    Snapshot(1, 0, acknowledged: null),
                    NetworkDelivery.Sequenced,
                    StarfallNetworkChannels.MovementSnapshots);
            }
        };

        session.ConnectAndAwaitInitialSnapshot(new(IPAddress.Loopback, 7777, "unused"));
        Assert.True(session.IsReady);
        Assert.Equal(StarfallGameplayProtocol.CurrentVersion, session.ProtocolVersion);
        Assert.Equal(0UL, session.Snapshot!.Value.Tick);
        Assert.Contains(transport.Sent, static value => value.Channel == StarfallNetworkChannels.Admission);

        session.SendMovementIntent(new GroundPoint(110, 25));
        session.SendMovementIntent(new GroundPoint(120, 25));
        SentPacket[] commands = transport.Sent.Where(static value => value.Channel == StarfallNetworkChannels.MovementCommands).ToArray();
        Assert.Equal(2, commands.Length);
        Assert.All(commands, static value => Assert.Equal(NetworkDelivery.ReliableSequenced, value.Delivery));
        Assert.True(ConnectedWalkingCodec.TryDecodeCommand(commands[0].Payload, out GroundMovementCommand? first));
        Assert.True(ConnectedWalkingCodec.TryDecodeCommand(commands[1].Payload, out GroundMovementCommand? second));
        Assert.Equal(1UL, first.Sequence.Value);
        Assert.Equal(2UL, second.Sequence.Value);

        session.PacketReceived(
            transport.Peer,
            Snapshot(2, 1, new MovementIntentSequence(2)),
            NetworkDelivery.Sequenced,
            StarfallNetworkChannels.MovementSnapshots);
        Assert.Equal(1UL, session.Snapshot.Value.Tick);
        session.PacketReceived(
            transport.Peer,
            Snapshot(1, 0, null),
            NetworkDelivery.Sequenced,
            StarfallNetworkChannels.MovementSnapshots);
        Assert.Equal(1UL, session.Snapshot.Value.Tick);
    }

    [Fact]
    public void Session_retains_valid_initial_snapshot_until_cross_channel_acceptance_arrives()
    {
        var transport = new ScriptedTransport();
        var session = new ConnectedWalkingClientSession(transport, "ticket");
        transport.OnPoll = handler =>
        {
            if (transport.PollCount != 1)
                return;
            handler.Connected(transport.Peer, new NetworkEndpoint("127.0.0.1", 7777));
            handler.PacketReceived(
                transport.Peer,
                Snapshot(1, 0, acknowledged: null),
                NetworkDelivery.Sequenced,
                StarfallNetworkChannels.MovementSnapshots);
            Assert.False(session.IsReady);
            handler.PacketReceived(
                transport.Peer,
                WorldJoinAdmissionCodec.EncodeAccepted(
                    new WorldJoinAccepted(
                        StarfallGameplayProtocol.CurrentVersion,
                        new GameplaySessionId(Guid.NewGuid()))),
                NetworkDelivery.ReliableOrdered,
                StarfallNetworkChannels.Admission);
        };

        session.ConnectAndAwaitInitialSnapshot(new(IPAddress.Loopback, 7777, "unused"));

        Assert.True(session.IsReady);
        Assert.Equal(0UL, session.Snapshot!.Value.Tick);
    }

    [Fact]
    public void Session_rejects_an_accepted_version_other_than_the_one_it_offered()
    {
        var transport = new ScriptedTransport();
        var session = new ConnectedWalkingClientSession(transport, "ticket");
        transport.OnPoll = handler =>
        {
            if (transport.PollCount != 1)
                return;
            handler.Connected(transport.Peer, new NetworkEndpoint("127.0.0.1", 7777));
            handler.PacketReceived(
                transport.Peer,
                WorldJoinAdmissionCodec.EncodeAccepted(
                    new WorldJoinAccepted(new ProtocolVersion(2), new GameplaySessionId(Guid.NewGuid()))),
                NetworkDelivery.ReliableOrdered,
                StarfallNetworkChannels.Admission);
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            session.ConnectAndAwaitInitialSnapshot(
                new(IPAddress.Loopback, 7777, "unused"),
                TimeSpan.FromSeconds(1)));

        Assert.Contains("incompatible gameplay protocol version 2", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(session.ProtocolVersion);
        Assert.Null(session.SessionId);
        SentPacket requestPacket = Assert.Single(
            transport.Sent,
            static value => value.Channel == StarfallNetworkChannels.Admission);
        Assert.True(WorldJoinAdmissionCodec.TryDecodeRequest(requestPacket.Payload, out WorldJoinRequest? request));
        Assert.Equal(StarfallGameplayProtocol.CurrentVersion, request.ProtocolVersion);
    }

    [Fact]
    public void Session_retains_latest_monster_snapshot_across_admission_reordering()
    {
        var transport = new ScriptedTransport();
        var session = new ConnectedWalkingClientSession(transport, "ticket");
        transport.OnPoll = handler =>
        {
            if (transport.PollCount != 1)
                return;
            handler.Connected(transport.Peer, new NetworkEndpoint("127.0.0.1", 7777));
            handler.PacketReceived(
                transport.Peer,
                MonsterSnapshot(1, 0),
                NetworkDelivery.Sequenced,
                StarfallNetworkChannels.MonsterSnapshots);
            handler.PacketReceived(
                transport.Peer,
                WorldJoinAdmissionCodec.EncodeAccepted(
                    new WorldJoinAccepted(
                        StarfallGameplayProtocol.CurrentVersion,
                        new GameplaySessionId(Guid.NewGuid()))),
                NetworkDelivery.ReliableOrdered,
                StarfallNetworkChannels.Admission);
            handler.PacketReceived(
                transport.Peer,
                Snapshot(1, 0, acknowledged: null),
                NetworkDelivery.Sequenced,
                StarfallNetworkChannels.MovementSnapshots);
        };

        session.ConnectAndAwaitInitialSnapshot(new(IPAddress.Loopback, 7777, "unused"));
        session.PacketReceived(
            transport.Peer,
            MonsterSnapshot(2, 1),
            NetworkDelivery.Sequenced,
            StarfallNetworkChannels.MonsterSnapshots);

        Assert.True(session.IsReady);
        Assert.False(session.IsDisconnected);
        Assert.Equal(0UL, session.Snapshot!.Value.Tick);
        Assert.Equal(2UL, session.MonsterSnapshot!.Sequence.Value);
        Assert.Equal(1UL, session.MonsterSnapshot.SimulationTick);

        session.PacketReceived(
            transport.Peer,
            MonsterSnapshot(1, 0),
            NetworkDelivery.Sequenced,
            StarfallNetworkChannels.MonsterSnapshots);
        Assert.Equal(2UL, session.MonsterSnapshot.Sequence.Value);
    }

    [Fact]
    public void Session_rejects_a_newer_monster_sequence_with_a_backward_tick_independently_of_movement()
    {
        var transport = new ScriptedTransport();
        var session = new ConnectedWalkingClientSession(transport, "ticket");
        transport.OnPoll = handler =>
        {
            if (transport.PollCount != 1)
                return;
            handler.Connected(transport.Peer, new NetworkEndpoint("127.0.0.1", 7777));
            handler.PacketReceived(
                transport.Peer,
                WorldJoinAdmissionCodec.EncodeAccepted(
                    new WorldJoinAccepted(
                        StarfallGameplayProtocol.CurrentVersion,
                        new GameplaySessionId(Guid.NewGuid()))),
                NetworkDelivery.ReliableOrdered,
                StarfallNetworkChannels.Admission);
            handler.PacketReceived(
                transport.Peer,
                Snapshot(1, 100, acknowledged: null),
                NetworkDelivery.Sequenced,
                StarfallNetworkChannels.MovementSnapshots);
            handler.PacketReceived(
                transport.Peer,
                MonsterSnapshot(1, 5),
                NetworkDelivery.Sequenced,
                StarfallNetworkChannels.MonsterSnapshots);
        };

        session.ConnectAndAwaitInitialSnapshot(new(IPAddress.Loopback, 7777, "unused"));
        Assert.Equal(100UL, session.Snapshot!.Value.Tick);
        Assert.Equal(5UL, session.MonsterSnapshot!.SimulationTick);

        session.PacketReceived(
            transport.Peer,
            MonsterSnapshot(2, 4),
            NetworkDelivery.Sequenced,
            StarfallNetworkChannels.MonsterSnapshots);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(session.Poll);
        Assert.Contains("monster snapshot tick moved backwards", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1UL, session.MonsterSnapshot.Sequence.Value);
    }

    [Theory]
    [InlineData(NetworkDelivery.ReliableOrdered)]
    [InlineData(NetworkDelivery.Unreliable)]
    public void Session_rejects_misdelivered_monster_snapshots(NetworkDelivery delivery)
    {
        var transport = new ScriptedTransport();
        var session = new ConnectedWalkingClientSession(transport, "ticket");
        transport.OnPoll = handler =>
        {
            if (transport.PollCount != 1)
                return;
            handler.Connected(transport.Peer, new NetworkEndpoint("127.0.0.1", 7777));
            handler.PacketReceived(
                transport.Peer,
                MonsterSnapshot(1, 0),
                delivery,
                StarfallNetworkChannels.MonsterSnapshots);
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            session.ConnectAndAwaitInitialSnapshot(
                new(IPAddress.Loopback, 7777, "unused"),
                TimeSpan.FromSeconds(1)));

        Assert.Contains("monster snapshot", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(session.IsDisconnected);
    }

    [Fact]
    public void Session_rejects_malformed_sequenced_monster_snapshots()
    {
        var transport = new ScriptedTransport();
        var session = new ConnectedWalkingClientSession(transport, "ticket");
        transport.OnPoll = handler =>
        {
            if (transport.PollCount != 1)
                return;
            handler.Connected(transport.Peer, new NetworkEndpoint("127.0.0.1", 7777));
            handler.PacketReceived(
                transport.Peer,
                new byte[] { 1, 2, 3 },
                NetworkDelivery.Sequenced,
                StarfallNetworkChannels.MonsterSnapshots);
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            session.ConnectAndAwaitInitialSnapshot(
                new(IPAddress.Loopback, 7777, "unused"),
                TimeSpan.FromSeconds(1)));

        Assert.Contains("monster snapshot", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(session.IsDisconnected);
    }

    [Fact]
    public void Admission_rejection_and_timeout_fail_without_reconnect()
    {
        var rejectedTransport = new ScriptedTransport();
        var rejected = new ConnectedWalkingClientSession(rejectedTransport, "ticket");
        rejectedTransport.OnPoll = handler =>
        {
            if (rejectedTransport.PollCount != 1)
                return;
            handler.Connected(rejectedTransport.Peer, new NetworkEndpoint("127.0.0.1", 7777));
            handler.PacketReceived(
                rejectedTransport.Peer,
                WorldJoinAdmissionCodec.EncodeRejected(
                    new WorldJoinRejected(WorldJoinRejectionReason.ExpiredTicket)),
                NetworkDelivery.ReliableOrdered,
                StarfallNetworkChannels.Admission);
        };
        InvalidOperationException rejection = Assert.Throws<InvalidOperationException>(() =>
            rejected.ConnectAndAwaitInitialSnapshot(
                new(IPAddress.Loopback, 7777, "unused"),
                TimeSpan.FromSeconds(1)));
        Assert.Contains("ExpiredTicket", rejection.Message, StringComparison.Ordinal);
        Assert.Equal(1, rejectedTransport.ConnectCount);

        var timeoutTransport = new ScriptedTransport();
        var timedOut = new ConnectedWalkingClientSession(timeoutTransport, "ticket");
        InvalidOperationException timeout = Assert.Throws<InvalidOperationException>(() =>
            timedOut.ConnectAndAwaitInitialSnapshot(
                new(IPAddress.Loopback, 7777, "unused"),
                TimeSpan.FromMilliseconds(10)));
        Assert.Contains("timed out", timeout.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, timeoutTransport.ConnectCount);
    }

    [Fact]
    public void Session_sends_basic_arrow_commands_and_accepts_authoritative_resolution()
    {
        ConnectedWalkingClientSession session = CreateReadySession(out ScriptedTransport transport);

        CombatCommandSequence sequence = session.SendBasicArrowIntent(new WorldEntityId(10));
        SentPacket commandPacket = Assert.Single(
            transport.Sent,
            static packet => packet.Channel == StarfallNetworkChannels.BasicArrowCommands);
        Assert.Equal(NetworkDelivery.ReliableSequenced, commandPacket.Delivery);
        Assert.True(ConnectedBasicArrowCodec.TryDecodeCommand(commandPacket.Payload, out BasicArrowCommand? command));
        Assert.Equal(sequence, command.Sequence);
        Assert.Equal(10UL, command.TargetEntityId.Value);

        session.PacketReceived(
            transport.Peer,
            MonsterSnapshot(1, 1),
            NetworkDelivery.Sequenced,
            StarfallNetworkChannels.MonsterSnapshots);
        session.PacketReceived(
            transport.Peer,
            ConnectedBasicArrowCodec.EncodeAccepted(new BasicArrowAccepted(
                sequence, new WorldEntityId(1), new WorldEntityId(10), 5, 17)),
            NetworkDelivery.ReliableOrdered,
            StarfallNetworkChannels.BasicArrowOutcomes);
        Assert.Equal(ConnectedBasicArrowOutcomeKind.Accepted, session.LastBasicArrowOutcome!.Kind);

        session.PacketReceived(
            transport.Peer,
            MonsterSnapshot(2, 2),
            NetworkDelivery.Sequenced,
            StarfallNetworkChannels.MonsterSnapshots);
        session.PacketReceived(
            transport.Peer,
            ConnectedBasicArrowCodec.EncodeResolved(new BasicArrowResolved(
                sequence,
                new WorldEntityId(1),
                new WorldEntityId(10),
                5,
                17,
                requestedDamageUnits: 300,
                effectiveDamageUnits: 300,
                targetDefeated: false)),
            NetworkDelivery.ReliableOrdered,
            StarfallNetworkChannels.BasicArrowOutcomes);

        ConnectedBasicArrowOutcome resolved = Assert.IsType<ConnectedBasicArrowOutcome>(session.LastBasicArrowOutcome);
        Assert.Equal(ConnectedBasicArrowOutcomeKind.Resolved, resolved.Kind);
        Assert.Equal(300, resolved.EffectiveDamageUnits);
        Assert.False(resolved.TargetDefeated);
        Assert.False(session.IsDisconnected);
    }

    [Fact]
    public void Session_handles_rejection_and_cancellation_with_separate_monotonic_combat_sequences()
    {
        ConnectedWalkingClientSession session = CreateReadySession(out ScriptedTransport transport);
        CombatCommandSequence acceptedSequence = session.SendBasicArrowIntent(new WorldEntityId(10));
        CombatCommandSequence rejectedSequence = session.SendBasicArrowIntent(new WorldEntityId(11));
        Assert.Equal(1UL, acceptedSequence.Value);
        Assert.Equal(2UL, rejectedSequence.Value);

        session.PacketReceived(
            transport.Peer,
            ConnectedBasicArrowCodec.EncodeAccepted(new BasicArrowAccepted(
                acceptedSequence, new WorldEntityId(1), new WorldEntityId(10), 5, 17)),
            NetworkDelivery.ReliableOrdered,
            StarfallNetworkChannels.BasicArrowOutcomes);
        session.PacketReceived(
            transport.Peer,
            ConnectedBasicArrowCodec.EncodeRejected(new BasicArrowRejected(
                rejectedSequence,
                new WorldEntityId(1),
                new WorldEntityId(11),
                6,
                BasicArrowRejectionReason.ActionAlreadyPending)),
            NetworkDelivery.ReliableOrdered,
            StarfallNetworkChannels.BasicArrowOutcomes);
        Assert.Equal(ConnectedBasicArrowOutcomeKind.Rejected, session.LastBasicArrowOutcome!.Kind);

        session.PacketReceived(
            transport.Peer,
            ConnectedBasicArrowCodec.EncodeCanceled(new BasicArrowCanceled(
                acceptedSequence,
                new WorldEntityId(1),
                new WorldEntityId(10),
                5,
                17,
                8,
                BasicArrowCancellationReason.CanceledByMovement)),
            NetworkDelivery.ReliableOrdered,
            StarfallNetworkChannels.BasicArrowOutcomes);
        Assert.Equal(ConnectedBasicArrowOutcomeKind.Canceled, session.LastBasicArrowOutcome!.Kind);
        Assert.Equal(BasicArrowCancellationReason.CanceledByMovement, session.LastBasicArrowOutcome.CancellationReason);
    }

    [Fact]
    public void Session_rejects_inconsistent_or_misrouted_basic_arrow_outcomes()
    {
        ConnectedWalkingClientSession session = CreateReadySession(out ScriptedTransport transport);
        CombatCommandSequence sequence = session.SendBasicArrowIntent(new WorldEntityId(10));
        session.PacketReceived(
            transport.Peer,
            ConnectedBasicArrowCodec.EncodeAccepted(new BasicArrowAccepted(
                sequence, new WorldEntityId(99), new WorldEntityId(10), 5, 17)),
            NetworkDelivery.ReliableOrdered,
            StarfallNetworkChannels.BasicArrowOutcomes);
        InvalidOperationException mismatch = Assert.Throws<InvalidOperationException>(session.Poll);
        Assert.Contains("Basic Arrow", mismatch.Message, StringComparison.Ordinal);

        ConnectedWalkingClientSession misrouted = CreateReadySession(out ScriptedTransport otherTransport);
        CombatCommandSequence otherSequence = misrouted.SendBasicArrowIntent(new WorldEntityId(10));
        misrouted.PacketReceived(
            otherTransport.Peer,
            ConnectedBasicArrowCodec.EncodeRejected(new BasicArrowRejected(
                otherSequence,
                new WorldEntityId(1),
                new WorldEntityId(10),
                5,
                BasicArrowRejectionReason.TargetOutOfRange)),
            NetworkDelivery.Sequenced,
            StarfallNetworkChannels.BasicArrowOutcomes);
        Assert.Throws<InvalidOperationException>(misrouted.Poll);
    }

    [Fact]
    public void Session_handles_sequenced_command_gaps_and_fails_explicitly_at_sequence_exhaustion()
    {
        ConnectedWalkingClientSession session = CreateReadySession(out ScriptedTransport transport);
        _ = session.SendBasicArrowIntent(new WorldEntityId(10));
        CombatCommandSequence latest = session.SendBasicArrowIntent(new WorldEntityId(11));
        session.PacketReceived(
            transport.Peer,
            ConnectedBasicArrowCodec.EncodeRejected(new BasicArrowRejected(
                latest,
                new WorldEntityId(1),
                new WorldEntityId(11),
                5,
                BasicArrowRejectionReason.TargetOutOfRange)),
            NetworkDelivery.ReliableOrdered,
            StarfallNetworkChannels.BasicArrowOutcomes);
        Assert.False(session.IsDisconnected);

        ConnectedWalkingClientSession exhausted = CreateReadySession(out _, ulong.MaxValue);
        Assert.Equal(ulong.MaxValue, exhausted.SendBasicArrowIntent(new WorldEntityId(10)).Value);
        Assert.Throws<InvalidOperationException>(() => exhausted.SendBasicArrowIntent(new WorldEntityId(11)));

        ConnectedWalkingClientSession bounded = CreateReadySession(out _);
        for (ulong identity = 10; identity < 10 + ConnectedWalkingClientSession.MaximumOutstandingBasicArrowCommands; identity++)
            _ = bounded.SendBasicArrowIntent(new WorldEntityId(identity));
        Assert.Throws<InvalidOperationException>(() => bounded.SendBasicArrowIntent(new WorldEntityId(1000)));
    }

    private static ConnectedWalkingClientSession CreateReadySession(
        out ScriptedTransport transport,
        ulong initialCombatSequence = 1)
    {
        transport = new ScriptedTransport();
        var session = new ConnectedWalkingClientSession(transport, "ticket", initialCombatSequence);
        ScriptedTransport captured = transport;
        captured.OnPoll = handler =>
        {
            if (captured.PollCount != 1)
                return;
            handler.Connected(captured.Peer, new NetworkEndpoint("127.0.0.1", 7777));
            handler.PacketReceived(
                captured.Peer,
                WorldJoinAdmissionCodec.EncodeAccepted(new WorldJoinAccepted(
                    StarfallGameplayProtocol.CurrentVersion,
                    new GameplaySessionId(Guid.NewGuid()))),
                NetworkDelivery.ReliableOrdered,
                StarfallNetworkChannels.Admission);
            handler.PacketReceived(
                captured.Peer,
                Snapshot(1, 0, acknowledged: null),
                NetworkDelivery.Sequenced,
                StarfallNetworkChannels.MovementSnapshots);
        };
        session.ConnectAndAwaitInitialSnapshot(new(IPAddress.Loopback, 7777, "unused"));
        return session;
    }

    private static byte[] Snapshot(ulong sequence, ulong tick, MovementIntentSequence? acknowledged) =>
        ConnectedWalkingCodec.EncodeSnapshot(new PlayerMovementSnapshot(
            new MovementSnapshotSequence(sequence),
            tick,
            new WorldEntityId(1),
            new GroundPosition(100 + tick, 25),
            new System.Numerics.Vector2(4, 0),
            System.Numerics.Vector2.UnitX,
            new PlayerCollisionCapsule(0.4f, 1.8f),
            acknowledged));

    private static byte[] MonsterSnapshot(ulong sequence, ulong tick) =>
        BoundedMonsterSnapshotCodec.Encode(new BoundedMonsterSnapshot(
            new MonsterSnapshotSequence(sequence),
            tick,
            [],
            []));

    private sealed class ScriptedTransport : INetworkTransport
    {
        internal NetworkPeerId Peer { get; } = new(4);
        internal int PollCount
        {
            get; private set;
        }

        internal int ConnectCount
        {
            get; private set;
        }
        internal Action<INetworkEventHandler>? OnPoll
        {
            get; set;
        }
        internal List<SentPacket> Sent { get; } = [];
        public void Start(int port)
        {
        }
        public NetworkPeerId Connect(NetworkEndpoint endpoint)
        {
            ConnectCount++;
            return Peer;
        }
        public void Send(NetworkPeerId peerId, ReadOnlySpan<byte> packet, NetworkDelivery delivery, byte channel = 0) =>
            Sent.Add(new(packet.ToArray(), delivery, channel));
        public void Disconnect(NetworkPeerId peerId)
        {
        }
        public void Poll(INetworkEventHandler handler)
        {
            PollCount++;
            OnPoll?.Invoke(handler);
        }
        public void Dispose()
        {
        }
    }

    private sealed record SentPacket(byte[] Payload, NetworkDelivery Delivery, byte Channel);
}
