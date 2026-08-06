using System.Net.Sockets;
using System.Security.Cryptography;
using ChronoFall.Network.Transport;
using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Combat;
using Starfall.Protocol.Compatibility;
using Starfall.Protocol.Monsters;
using Starfall.Protocol.Movement;
using Starfall.Protocol.Networking;
using Starfall.Simulation.Movement;
using Starfall.World.Entities;
using Starfall.World.Lifecycle;
using Starfall.World.Networking;

namespace Starfall.World.Tests;

public sealed class WorldGameplayNetworkHostTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeMilliseconds(1_800_000_000_000);

    [Fact]
    public void Loopback_peer_admits_binds_moves_corrects_publishes_and_disconnects_atomically()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var transport = new RecordingTransport();
        WorldChannelRuntime runtime = CreateRuntime();
        using var runtimeScope = new RuntimeScope(runtime);
        using var host = new WorldGameplayNetworkHost(
            transport,
            runtime,
            Ring(key),
            new FixedTimeProvider(Now));
        var peer = new NetworkPeerId(7);

        host.Connected(peer, new NetworkEndpoint("127.0.0.1", 40000));
        host.PacketReceived(
            peer,
            WorldJoinAdmissionCodec.EncodeRequest(Issue(runtime, key)),
            NetworkDelivery.ReliableOrdered,
            StarfallNetworkChannels.Admission);

        SentPacket accepted = Assert.Single(transport.Sent, static value => value.Channel == 0);
        Assert.Equal(NetworkDelivery.ReliableOrdered, accepted.Delivery);
        Assert.True(WorldJoinAdmissionCodec.TryDecodeAccepted(accepted.Payload, out WorldJoinAccepted? admission));
        Assert.Equal(StarfallGameplayProtocol.CurrentVersion, admission.SelectedProtocolVersion);
        Assert.Equal(1, runtime.ActiveSessionCount);
        Assert.Equal(1, runtime.PlayerCount);
        Assert.Contains(transport.Sent, static value => value.Channel == StarfallNetworkChannels.MovementSnapshots);
        SentPacket monsterPacket = Assert.Single(
            transport.Sent,
            static value => value.Channel == StarfallNetworkChannels.MonsterSnapshots);
        Assert.Equal(NetworkDelivery.Sequenced, monsterPacket.Delivery);
        Assert.True(BoundedMonsterSnapshotCodec.TryDecode(
            monsterPacket.Payload,
            out BoundedMonsterSnapshot? initialMonsters));
        Assert.NotNull(initialMonsters);
        Assert.Equal(1UL, initialMonsters.Sequence.Value);
        Assert.Equal(0UL, initialMonsters.SimulationTick);
        Assert.Equal(10, initialMonsters.LiveMonsters.Length);

        transport.Sent.Clear();
        var command = new GroundMovementCommand(
            new MovementIntentSequence(1),
            new GroundPosition(110, 25));
        host.PacketReceived(
            peer,
            ConnectedWalkingCodec.EncodeCommand(command),
            NetworkDelivery.ReliableSequenced,
            StarfallNetworkChannels.MovementCommands);
        runtime.Step();
        host.Pump();
        Assert.Contains(transport.Sent, static value =>
            value.Channel == StarfallNetworkChannels.MovementSnapshots &&
            value.Delivery == NetworkDelivery.Sequenced);
        Assert.Contains(transport.Sent, static value =>
            value.Channel == StarfallNetworkChannels.MonsterSnapshots &&
            value.Delivery == NetworkDelivery.Sequenced);

        transport.Sent.Clear();
        var invalid = new GroundMovementCommand(
            new MovementIntentSequence(2),
            new GroundPosition(-1, -1));
        host.PacketReceived(
            peer,
            ConnectedWalkingCodec.EncodeCommand(invalid),
            NetworkDelivery.ReliableSequenced,
            StarfallNetworkChannels.MovementCommands);
        Assert.Contains(transport.Sent, static value =>
            value.Channel == StarfallNetworkChannels.MovementCorrections &&
            value.Delivery == NetworkDelivery.ReliableOrdered);

        host.Disconnected(peer, NetworkDisconnectReason.RemoteConnectionClose);
        Assert.Equal(0, runtime.ActiveSessionCount);
        Assert.Equal(0, runtime.PlayerCount);
        Assert.False(runtime.TryGetGameplaySession(admission.SessionId, out _));
    }

    [Fact]
    public void Incompatible_protocol_is_rejected_without_creating_a_session_or_consuming_the_ticket()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var transport = new RecordingTransport();
        WorldChannelRuntime runtime = CreateRuntime();
        using var runtimeScope = new RuntimeScope(runtime);
        using var host = new WorldGameplayNetworkHost(transport, runtime, Ring(key), new FixedTimeProvider(Now));
        var peer = new NetworkPeerId(8);
        WorldJoinRequest compatible = Issue(runtime, key);
        var incompatible = new WorldJoinRequest(new ProtocolVersion(2), compatible.Ticket);

        host.Connected(peer, new NetworkEndpoint("127.0.0.1", 40000));
        host.PacketReceived(
            peer,
            WorldJoinAdmissionCodec.EncodeRequest(incompatible),
            NetworkDelivery.ReliableOrdered,
            StarfallNetworkChannels.Admission);

        SentPacket rejection = Assert.Single(transport.Sent);
        Assert.True(WorldJoinAdmissionCodec.TryDecodeRejected(rejection.Payload, out WorldJoinRejected? decoded));
        Assert.Equal(WorldJoinRejectionReason.IncompatibleProtocolVersion, decoded.Reason);
        Assert.Equal(0, runtime.ActiveSessionCount);
        Assert.Equal(0, runtime.PlayerCount);
        Assert.Equal(0, runtime.ConsumedTicketCount);
    }

    [Fact]
    public void Non_loopback_is_disconnected_and_malformed_admission_is_rejected()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var transport = new RecordingTransport();
        WorldChannelRuntime runtime = CreateRuntime();
        using var runtimeScope = new RuntimeScope(runtime);
        using var host = new WorldGameplayNetworkHost(transport, runtime, Ring(key));
        var remote = new NetworkPeerId(1);
        host.Connected(remote, new NetworkEndpoint("192.0.2.10", 40000));
        Assert.Contains(remote, transport.Disconnected);

        var loopback = new NetworkPeerId(2);
        host.Connected(loopback, new NetworkEndpoint("::1", 40000));
        host.PacketReceived(loopback, new byte[] { 1 }, NetworkDelivery.Sequenced, StarfallNetworkChannels.MovementSnapshots);
        SentPacket rejection = Assert.Single(transport.Sent, static value => value.Channel == StarfallNetworkChannels.Admission);
        Assert.True(WorldJoinAdmissionCodec.TryDecodeRejected(rejection.Payload, out WorldJoinRejected? decoded));
        Assert.Equal(WorldJoinRejectionReason.InvalidTicket, decoded.Reason);
    }

    [Fact]
    public void Pending_admission_expires_after_ten_seconds_without_affecting_world()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var transport = new RecordingTransport();
        WorldChannelRuntime runtime = CreateRuntime();
        using var runtimeScope = new RuntimeScope(runtime);
        var clock = new AdjustableTimeProvider(Now);
        using var host = new WorldGameplayNetworkHost(transport, runtime, Ring(key), clock);
        var peer = new NetworkPeerId(3);
        host.Connected(peer, new NetworkEndpoint("127.0.0.1", 40000));

        clock.Advance(WorldGameplayNetworkHost.AdmissionTimeout);
        host.Pump();

        Assert.Contains(peer, transport.Disconnected);
        Assert.Equal(0, runtime.ActiveSessionCount);
        Assert.Equal(WorldChannelLifecycleState.Running, runtime.State);
    }

    [Fact]
    public void Basic_arrow_is_requester_only_and_uses_dedicated_reliable_channels()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var transport = new RecordingTransport();
        WorldChannelRuntime runtime = CreateRuntime();
        using var runtimeScope = new RuntimeScope(runtime);
        using var host = new WorldGameplayNetworkHost(transport, runtime, Ring(key), new FixedTimeProvider(Now));
        var firstPeer = new NetworkPeerId(10);
        var secondPeer = new NetworkPeerId(11);
        WorldJoinAccepted firstAdmission = Admit(host, transport, runtime, key, firstPeer);
        _ = Admit(host, transport, runtime, key, secondPeer);
        Assert.True(runtime.TryGetGameplaySession(firstAdmission.SessionId, out var firstSession));
        Assert.NotNull(firstSession);
        WorldMonsterState target = PrepareCombatFixture(runtime, firstSession!);
        transport.Sent.Clear();

        host.PacketReceived(
            firstPeer,
            EncodeBasicArrow(1, target.EntityId.Value),
            NetworkDelivery.ReliableSequenced,
            StarfallNetworkChannels.BasicArrowCommands);

        SentPacket acceptedPacket = Assert.Single(transport.Sent);
        Assert.Equal(firstPeer, acceptedPacket.PeerId);
        Assert.Equal(StarfallNetworkChannels.BasicArrowOutcomes, acceptedPacket.Channel);
        Assert.Equal(NetworkDelivery.ReliableOrdered, acceptedPacket.Delivery);
        Assert.True(ConnectedBasicArrowCodec.TryDecodeAccepted(acceptedPacket.Payload, out BasicArrowAccepted? accepted));
        Assert.Equal(firstSession!.PlayerEntityId.Value, accepted!.ActorEntityId.Value);

        transport.Sent.Clear();
        while (runtime.CurrentTick < accepted.ResolveTick)
        {
            runtime.Step();
            host.PublishBasicArrowTickOutcomes();
        }

        SentPacket resolvedPacket = Assert.Single(transport.Sent);
        Assert.Equal(firstPeer, resolvedPacket.PeerId);
        Assert.True(ConnectedBasicArrowCodec.TryDecodeResolved(resolvedPacket.Payload, out BasicArrowResolved? resolved));
        Assert.Equal(300, resolved!.EffectiveDamageUnits);
        Assert.DoesNotContain(transport.Sent, packet => packet.PeerId == secondPeer);
    }

    [Fact]
    public void Movement_arriving_after_acceptance_cancels_basic_arrow_immediately()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var transport = new RecordingTransport();
        WorldChannelRuntime runtime = CreateRuntime();
        using var runtimeScope = new RuntimeScope(runtime);
        using var host = new WorldGameplayNetworkHost(transport, runtime, Ring(key), new FixedTimeProvider(Now));
        var peer = new NetworkPeerId(12);
        WorldJoinAccepted admission = Admit(host, transport, runtime, key, peer);
        Assert.True(runtime.TryGetGameplaySession(admission.SessionId, out var session));
        WorldMonsterState target = PrepareCombatFixture(runtime, session!);
        transport.Sent.Clear();
        host.PacketReceived(
            peer,
            EncodeBasicArrow(1, target.EntityId.Value),
            NetworkDelivery.ReliableSequenced,
            StarfallNetworkChannels.BasicArrowCommands);
        transport.Sent.Clear();

        host.PacketReceived(
            peer,
            ConnectedWalkingCodec.EncodeCommand(new GroundMovementCommand(
                new MovementIntentSequence(1),
                new GroundPosition(70.0f, 70.0f))),
            NetworkDelivery.ReliableSequenced,
            StarfallNetworkChannels.MovementCommands);

        SentPacket canceledPacket = Assert.Single(
            transport.Sent,
            packet => packet.Channel == StarfallNetworkChannels.BasicArrowOutcomes);
        Assert.True(ConnectedBasicArrowCodec.TryDecodeCanceled(canceledPacket.Payload, out BasicArrowCanceled? canceled));
        Assert.Equal(BasicArrowCancellationReason.CanceledByMovement, canceled!.Reason);
        Assert.Equal(0, runtime.PendingBasicArrowCount);
    }

    [Fact]
    public void Malformed_basic_arrow_is_a_protocol_violation()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var transport = new RecordingTransport();
        WorldChannelRuntime runtime = CreateRuntime();
        using var runtimeScope = new RuntimeScope(runtime);
        using var host = new WorldGameplayNetworkHost(transport, runtime, Ring(key), new FixedTimeProvider(Now));
        var peer = new NetworkPeerId(13);
        _ = Admit(host, transport, runtime, key, peer);

        host.PacketReceived(
            peer,
            new byte[] { 1, 2, 3 },
            NetworkDelivery.ReliableSequenced,
            StarfallNetworkChannels.BasicArrowCommands);

        Assert.Contains(peer, transport.Disconnected);
        Assert.Equal(0, runtime.ActiveSessionCount);
    }

    [Fact]
    public void Wrong_delivery_basic_arrow_is_a_protocol_violation()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var transport = new RecordingTransport();
        WorldChannelRuntime runtime = CreateRuntime();
        using var runtimeScope = new RuntimeScope(runtime);
        using var host = new WorldGameplayNetworkHost(transport, runtime, Ring(key), new FixedTimeProvider(Now));
        var peer = new NetworkPeerId(15);
        _ = Admit(host, transport, runtime, key, peer);

        host.PacketReceived(
            peer,
            EncodeBasicArrow(1, runtime.Monsters[0].EntityId.Value),
            NetworkDelivery.ReliableOrdered,
            StarfallNetworkChannels.BasicArrowCommands);

        Assert.Contains(peer, transport.Disconnected);
        Assert.Equal(0, runtime.ActiveSessionCount);
    }

    [Fact]
    public void Basic_arrow_send_failure_disconnects_and_cleans_up_the_session()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var transport = new RecordingTransport();
        WorldChannelRuntime runtime = CreateRuntime();
        using var runtimeScope = new RuntimeScope(runtime);
        using var host = new WorldGameplayNetworkHost(transport, runtime, Ring(key), new FixedTimeProvider(Now));
        var peer = new NetworkPeerId(14);
        WorldJoinAccepted admission = Admit(host, transport, runtime, key, peer);
        Assert.True(runtime.TryGetGameplaySession(admission.SessionId, out var session));
        WorldMonsterState target = PrepareCombatFixture(runtime, session!);
        transport.ThrowOnChannel = StarfallNetworkChannels.BasicArrowOutcomes;

        host.PacketReceived(
            peer,
            EncodeBasicArrow(1, target.EntityId.Value),
            NetworkDelivery.ReliableSequenced,
            StarfallNetworkChannels.BasicArrowCommands);

        Assert.Contains(peer, transport.Disconnected);
        Assert.Equal(0, runtime.ActiveSessionCount);
        Assert.Equal(0, runtime.PendingBasicArrowCount);
    }

    private static WorldChannelRuntime CreateRuntime()
    {
        var runtime = new WorldChannelRuntime(
            new WorldId("world_1"),
            new ChannelId("channel_1"),
            new WorldInstanceId(Guid.Parse("44444444-4444-4444-4444-444444444444")),
            Draft0GrayboxCatalog.FirstPlayable,
            Draft0StarterMonsterCatalog.FirstPlayable,
            Draft0CampPolicyCatalog.FirstPlayable);
        runtime.Start();
        return runtime;
    }

    private static WorldJoinTicketVerificationKeyRing Ring(ECDsa key) => new(
    [
        new WorldJoinTicketVerificationKey("development", key.ExportSubjectPublicKeyInfo()),
    ]);

    private static WorldJoinRequest Issue(WorldChannelRuntime runtime, ECDsa key)
    {
        long now = Now.ToUnixTimeMilliseconds();
        var claims = new WorldJoinTicketClaims(
            new JoinTicketId(Guid.NewGuid()),
            new AccountId(Guid.NewGuid()),
            new CharacterId(Guid.NewGuid()),
            runtime.WorldId,
            runtime.ChannelId,
            runtime.InstanceId,
            now,
            now + 30_000);
        return new WorldJoinRequest(
            StarfallGameplayProtocol.CurrentVersion,
            WorldJoinTicketCodec.Issue(claims, "development", key));
    }

    private static WorldJoinAccepted Admit(
        WorldGameplayNetworkHost host,
        RecordingTransport transport,
        WorldChannelRuntime runtime,
        ECDsa key,
        NetworkPeerId peer)
    {
        host.Connected(peer, new NetworkEndpoint("127.0.0.1", 40000));
        int before = transport.Sent.Count;
        host.PacketReceived(
            peer,
            WorldJoinAdmissionCodec.EncodeRequest(Issue(runtime, key)),
            NetworkDelivery.ReliableOrdered,
            StarfallNetworkChannels.Admission);
        SentPacket packet = transport.Sent
            .Skip(before)
            .Single(value => value.PeerId == peer && value.Channel == StarfallNetworkChannels.Admission);
        Assert.True(WorldJoinAdmissionCodec.TryDecodeAccepted(packet.Payload, out WorldJoinAccepted? accepted));
        return accepted!;
    }

    private static WorldMonsterState PrepareCombatFixture(
        WorldChannelRuntime runtime,
        Starfall.World.Admission.WorldGameplaySession session)
    {
        MovePlayerTo(runtime, session.PlayerEntityId, new GroundPoint(100.0f, 70.0f));
        MovePlayerTo(runtime, session.PlayerEntityId, new GroundPoint(70.0f, 65.0f));
        return Assert.Single(runtime.Monsters, static monster => monster.SpawnId == "spawn_easy_03");
    }

    private static void MovePlayerTo(
        WorldChannelRuntime runtime,
        Starfall.Simulation.Entities.WorldEntityId playerId,
        GroundPoint destination)
    {
        Assert.Equal(GroundMovementIntentDisposition.Accepted, runtime.SubmitMovementIntent(playerId, destination));
        for (var tick = 0; tick < 2_000; tick++)
        {
            runtime.Step();
            Assert.True(runtime.TryGetPlayer(playerId, out WorldPlayerState? player));
            if (player!.Position == destination)
                return;
        }
        throw new InvalidOperationException("Connected player did not reach the combat fixture position.");
    }

    private static byte[] EncodeBasicArrow(ulong sequence, ulong target) =>
        ConnectedBasicArrowCodec.EncodeCommand(new BasicArrowCommand(
            new CombatCommandSequence(sequence),
            new Starfall.Protocol.Movement.WorldEntityId(target)));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset current = now;
        public override DateTimeOffset GetUtcNow() => current;
        internal void Advance(TimeSpan amount) => current += amount;
    }

    private sealed class RecordingTransport : INetworkTransport
    {
        internal List<SentPacket> Sent { get; } = [];
        internal List<NetworkPeerId> Disconnected { get; } = [];
        internal byte? ThrowOnChannel
        {
            get; set;
        }
        public void Start(int port)
        {
        }
        public NetworkPeerId Connect(NetworkEndpoint endpoint) => new(0);
        public void Send(NetworkPeerId peerId, ReadOnlySpan<byte> packet, NetworkDelivery delivery, byte channel = 0)
        {
            if (ThrowOnChannel == channel)
                throw new InvalidOperationException("Injected send failure.");
            Sent.Add(new(peerId, packet.ToArray(), delivery, channel));
        }
        public void Disconnect(NetworkPeerId peerId) => Disconnected.Add(peerId);
        public void Poll(INetworkEventHandler handler)
        {
        }
        public void Dispose()
        {
        }
    }

    private sealed class RuntimeScope(WorldChannelRuntime runtime) : IDisposable
    {
        public void Dispose() => runtime.Stop();
    }

    private sealed record SentPacket(
        NetworkPeerId PeerId,
        byte[] Payload,
        NetworkDelivery Delivery,
        byte Channel);
}
