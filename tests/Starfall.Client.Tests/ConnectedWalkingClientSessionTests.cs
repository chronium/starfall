using System.Net;
using ChronoFall.Network.Transport;
using Starfall.Client.Networking;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
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
                        new WorldJoinAccepted(new GameplaySessionId(Guid.NewGuid()))),
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
                    new WorldJoinAccepted(new GameplaySessionId(Guid.NewGuid()))),
                NetworkDelivery.ReliableOrdered,
                StarfallNetworkChannels.Admission);
        };

        session.ConnectAndAwaitInitialSnapshot(new(IPAddress.Loopback, 7777, "unused"));

        Assert.True(session.IsReady);
        Assert.Equal(0UL, session.Snapshot!.Value.Tick);
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
                    new WorldJoinAccepted(new GameplaySessionId(Guid.NewGuid()))),
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
                    new WorldJoinAccepted(new GameplaySessionId(Guid.NewGuid()))),
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
