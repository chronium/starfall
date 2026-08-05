using System.Diagnostics;
using System.Net.Sockets;
using ChronoFall.Network.Transport;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Monsters;
using Starfall.Protocol.Movement;
using Starfall.Protocol.Networking;

namespace Starfall.Client.Networking;

internal sealed class ConnectedWalkingClientSession : INetworkEventHandler, IDisposable
{
    internal static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(10);
    private readonly INetworkTransport transport;
    private readonly WorldJoinRequest request;
    private NetworkPeerId peerId;
    private bool peerAssigned;
    private bool connected;
    private bool admissionAccepted;
    private bool disposed;
    private string? failure;
    private ulong nextIntentSequence = 1;
    private ulong lastSentIntentSequence;
    private ulong lastSnapshotSequence;
    private ulong lastTick;
    private ulong lastMonsterSnapshotSequence;
    private ulong lastMonsterTick;
    private ulong? entityId;

    internal ConnectedWalkingClientSession(INetworkTransport transport, string ticket)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        request = new WorldJoinRequest(ticket);
    }

    internal GameplaySessionId? SessionId
    {
        get; private set;
    }
    internal TechnicalPlayerSnapshot? Snapshot
    {
        get; private set;
    }
    internal BoundedMonsterSnapshot? MonsterSnapshot
    {
        get; private set;
    }
    internal bool IsReady => admissionAccepted && Snapshot.HasValue;
    internal bool IsDisconnected => failure is not null;

    internal void ConnectAndAwaitInitialSnapshot(
        ConnectedClientLaunchOptions options,
        TimeSpan? timeoutOverride = null)
    {
        TimeSpan timeout = timeoutOverride ?? ConnectionTimeout;
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeoutOverride));
        transport.Start(0);
        peerId = transport.Connect(new NetworkEndpoint(options.Address.ToString(), options.Port));
        peerAssigned = true;
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (!IsReady && failure is null && stopwatch.Elapsed < timeout)
        {
            Poll();
            Thread.Sleep(5);
        }
        if (!IsReady)
            throw new InvalidOperationException(failure ?? "Connected walking admission timed out.");
    }

    internal void Poll()
    {
        transport.Poll(this);
        if (failure is not null)
            throw new InvalidOperationException(failure);
    }

    internal void SendMovementIntent(GroundPoint destination)
    {
        if (!IsReady)
            throw new InvalidOperationException("Connected walking session is not ready.");
        if (nextIntentSequence == 0)
            throw new InvalidOperationException("Movement intent sequence space is exhausted.");
        ulong sequence = nextIntentSequence;
        nextIntentSequence = sequence == ulong.MaxValue ? 0 : sequence + 1;
        var command = new GroundMovementCommand(
            new MovementIntentSequence(sequence),
            new GroundPosition(destination.XMetres, destination.ZMetres));
        transport.Send(peerId, ConnectedWalkingCodec.EncodeCommand(command), NetworkDelivery.ReliableSequenced, StarfallNetworkChannels.MovementCommands);
        lastSentIntentSequence = sequence;
    }

    public void Connected(NetworkPeerId connectedPeer, NetworkEndpoint endpoint)
    {
        if (!peerAssigned || connectedPeer != peerId)
        {
            failure = "Transport connected an unexpected peer.";
            return;
        }
        connected = true;
        transport.Send(peerId, WorldJoinAdmissionCodec.EncodeRequest(request), NetworkDelivery.ReliableOrdered, StarfallNetworkChannels.Admission);
    }

    public void Disconnected(NetworkPeerId disconnectedPeer, NetworkDisconnectReason reason)
    {
        if (peerAssigned && disconnectedPeer == peerId)
        {
            connected = false;
            failure = $"World disconnected the client ({reason}).";
        }
    }

    public void PacketReceived(NetworkPeerId sender, ReadOnlyMemory<byte> packet, NetworkDelivery delivery, byte channel)
    {
        if (!peerAssigned || sender != peerId)
        {
            failure = "Received a packet from an unexpected peer.";
            return;
        }
        if (channel == StarfallNetworkChannels.MonsterSnapshots)
        {
            if (delivery == NetworkDelivery.Sequenced &&
                BoundedMonsterSnapshotCodec.TryDecode(packet.Span, out BoundedMonsterSnapshot? monsterSnapshot))
            {
                AcceptMonsterSnapshot(monsterSnapshot);
                return;
            }

            failure = "World sent malformed or misrouted monster snapshot data.";
            return;
        }
        if (!admissionAccepted)
        {
            if (channel == StarfallNetworkChannels.MovementSnapshots && delivery == NetworkDelivery.Sequenced)
            {
                if (ConnectedWalkingCodec.TryDecodeSnapshot(packet.Span, out PlayerMovementSnapshot? earlySnapshot))
                {
                    AcceptSnapshot(earlySnapshot);
                    return;
                }
                failure = "World sent malformed initial movement snapshot data.";
                return;
            }
            if (channel != StarfallNetworkChannels.Admission || delivery != NetworkDelivery.ReliableOrdered)
            {
                failure = "Received non-admission data before world admission.";
                return;
            }
            if (WorldJoinAdmissionCodec.TryDecodeAccepted(packet.Span, out WorldJoinAccepted? accepted))
            {
                SessionId = accepted.SessionId;
                admissionAccepted = true;
                return;
            }
            if (WorldJoinAdmissionCodec.TryDecodeRejected(packet.Span, out WorldJoinRejected? rejected))
            {
                failure = $"World rejected admission ({rejected.Reason}).";
                return;
            }
            failure = "World sent malformed admission data.";
            return;
        }

        PlayerMovementSnapshot? snapshot;
        if (channel == StarfallNetworkChannels.MovementSnapshots && delivery == NetworkDelivery.Sequenced &&
            ConnectedWalkingCodec.TryDecodeSnapshot(packet.Span, out snapshot))
        {
            AcceptSnapshot(snapshot);
            return;
        }
        if (channel == StarfallNetworkChannels.MovementCorrections && delivery == NetworkDelivery.ReliableOrdered &&
            ConnectedWalkingCodec.TryDecodeCorrection(packet.Span, out PlayerMovementCorrection? correction))
        {
            AcceptSnapshot(correction.AuthoritativeSnapshot);
            return;
        }
        failure = "World sent malformed or misrouted connected-walking data.";
    }

    public void NetworkError(NetworkEndpoint? endpoint, SocketError socketError) =>
        Console.Error.WriteLine($"STARFALL_CLIENT_NETWORK_ERROR endpoint={endpoint?.ToString() ?? "unknown"} error={socketError}");

    public void LatencyUpdated(NetworkPeerId updatedPeer, int latencyMilliseconds)
    {
    }

    public void Dispose()
    {
        if (disposed)
            return;
        if (connected)
        {
            try
            {
                transport.Disconnect(peerId);
            }
            catch (InvalidOperationException)
            {
                // A transport-level disconnect may have raced the caller-owned teardown.
            }
        }
        transport.Dispose();
        disposed = true;
    }

    private void AcceptSnapshot(PlayerMovementSnapshot snapshot)
    {
        if (snapshot.Sequence.Value <= lastSnapshotSequence)
            return;
        if (snapshot.SimulationTick < lastTick)
        {
            failure = "World snapshot tick moved backwards.";
            return;
        }
        if (entityId is { } expected && snapshot.EntityId.Value != expected)
        {
            failure = "World changed the admitted player entity identity.";
            return;
        }
        if (snapshot.LastProcessedIntentSequence is { } acknowledged && acknowledged.Value > lastSentIntentSequence)
        {
            failure = "World acknowledged a movement intent the client did not send.";
            return;
        }
        entityId ??= snapshot.EntityId.Value;
        lastSnapshotSequence = snapshot.Sequence.Value;
        lastTick = snapshot.SimulationTick;
        Snapshot = new TechnicalPlayerSnapshot(
            $"entity_{snapshot.EntityId.Value}",
            snapshot.SimulationTick,
            new GroundPoint(snapshot.Position.XMetres, snapshot.Position.ZMetres),
            snapshot.VelocityMetresPerSecond,
            snapshot.Facing);
    }

    private void AcceptMonsterSnapshot(BoundedMonsterSnapshot snapshot)
    {
        if (snapshot.Sequence.Value <= lastMonsterSnapshotSequence)
            return;
        if (snapshot.SimulationTick < lastMonsterTick)
        {
            failure = "World monster snapshot tick moved backwards.";
            return;
        }

        lastMonsterSnapshotSequence = snapshot.Sequence.Value;
        lastMonsterTick = snapshot.SimulationTick;
        MonsterSnapshot = snapshot;
    }
}
