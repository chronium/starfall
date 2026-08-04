using System.Net.Sockets;
using System.Security.Cryptography;
using ChronoFall.Network.Transport;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Movement;
using Starfall.Protocol.Networking;
using Starfall.World.Lifecycle;
using Starfall.World.Networking;

namespace Starfall.World.Tests;

public sealed class WorldConnectedWalkingNetworkHostTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeMilliseconds(1_800_000_000_000);

    [Fact]
    public void Loopback_peer_admits_binds_moves_corrects_publishes_and_disconnects_atomically()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var transport = new RecordingTransport();
        WorldChannelRuntime runtime = CreateRuntime();
        using var runtimeScope = new RuntimeScope(runtime);
        using var host = new WorldConnectedWalkingNetworkHost(
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
        Assert.Equal(1, runtime.ActiveSessionCount);
        Assert.Equal(1, runtime.PlayerCount);
        Assert.Contains(transport.Sent, static value => value.Channel == StarfallNetworkChannels.MovementSnapshots);

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
    public void Non_loopback_is_disconnected_and_malformed_admission_is_rejected()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var transport = new RecordingTransport();
        WorldChannelRuntime runtime = CreateRuntime();
        using var runtimeScope = new RuntimeScope(runtime);
        using var host = new WorldConnectedWalkingNetworkHost(transport, runtime, Ring(key));
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
        using var host = new WorldConnectedWalkingNetworkHost(transport, runtime, Ring(key), clock);
        var peer = new NetworkPeerId(3);
        host.Connected(peer, new NetworkEndpoint("127.0.0.1", 40000));

        clock.Advance(WorldConnectedWalkingNetworkHost.AdmissionTimeout);
        host.Pump();

        Assert.Contains(peer, transport.Disconnected);
        Assert.Equal(0, runtime.ActiveSessionCount);
        Assert.Equal(WorldChannelLifecycleState.Running, runtime.State);
    }

    private static WorldChannelRuntime CreateRuntime()
    {
        var runtime = new WorldChannelRuntime(
            new WorldId("world_1"),
            new ChannelId("channel_1"),
            new WorldInstanceId(Guid.Parse("44444444-4444-4444-4444-444444444444")),
            Draft0GrayboxCatalog.FirstPlayable);
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
        return new WorldJoinRequest(WorldJoinTicketCodec.Issue(claims, "development", key));
    }

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
        public void Start(int port)
        {
        }
        public NetworkPeerId Connect(NetworkEndpoint endpoint) => new(0);
        public void Send(NetworkPeerId peerId, ReadOnlySpan<byte> packet, NetworkDelivery delivery, byte channel = 0) =>
            Sent.Add(new(peerId, packet.ToArray(), delivery, channel));
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
