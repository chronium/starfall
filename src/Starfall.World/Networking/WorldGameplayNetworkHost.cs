using System.Net;
using System.Net.Sockets;
using ChronoFall.Network.Transport;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Networking;
using Starfall.World.Admission;
using Starfall.World.Combat;
using Starfall.World.Development;
using Starfall.World.Lifecycle;
using Starfall.World.Monsters;
using Starfall.World.Movement;

namespace Starfall.World.Networking;

internal sealed class WorldGameplayNetworkHost : INetworkEventHandler, IDisposable
{
    internal static readonly TimeSpan AdmissionTimeout = TimeSpan.FromSeconds(10);
    private readonly INetworkTransport transport;
    private readonly WorldChannelRuntime runtime;
    private readonly WorldJoinAdmissionExchange admission;
    private readonly WorldWalkingExchange walking;
    private readonly WorldMonsterExchange monsters;
    private readonly WorldBasicArrowExchange basicArrow;
    private readonly WorldDevelopmentCommandDispatcher developmentCommands;
    private readonly TimeProvider timeProvider;
    private readonly Dictionary<NetworkPeerId, PeerState> peers = [];
    private readonly Dictionary<GameplaySessionId, NetworkPeerId> sessionPeers = [];
    private bool disposed;

    internal WorldGameplayNetworkHost(
        INetworkTransport transport,
        WorldChannelRuntime runtime,
        WorldJoinTicketVerificationKeyRing verificationKeys,
        TimeProvider? timeProvider = null)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        admission = new WorldJoinAdmissionExchange(runtime, verificationKeys ?? throw new ArgumentNullException(nameof(verificationKeys)));
        walking = new WorldWalkingExchange(runtime);
        monsters = new WorldMonsterExchange(runtime);
        basicArrow = new WorldBasicArrowExchange(runtime);
        developmentCommands = new WorldDevelopmentCommandDispatcher(
            runtime,
            [new PingDevelopmentCommandHandler()]);
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    internal void Start(int listenPort) => transport.Start(listenPort);

    internal void Pump()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        transport.Poll(this);
        ExpireAdmissions();
        PublishSnapshots();
    }

    internal void PublishBasicArrowTickOutcomes() =>
        PublishBasicArrowOutcomes(runtime.LastBasicArrowResolutions);

    public void Connected(NetworkPeerId peerId, NetworkEndpoint endpoint)
    {
        if (!IPAddress.TryParse(endpoint.Host, out IPAddress? address) || !IPAddress.IsLoopback(address))
        {
            Console.Error.WriteLine($"STARFALL_WORLD_PEER_REJECTED peer={peerId} reason=non-loopback endpoint={endpoint}");
            TryDisconnect(peerId);
            return;
        }
        peers[peerId] = new PeerState(timeProvider.GetUtcNow() + AdmissionTimeout);
    }

    public void Disconnected(NetworkPeerId peerId, NetworkDisconnectReason reason)
    {
        GameplaySessionId? sessionId = peers.TryGetValue(peerId, out PeerState? peer) ? peer.SessionId : null;
        CleanupPeer(peerId);
        if (sessionId is not null)
            Console.WriteLine($"STARFALL_WORLD_SESSION_ENDED session={sessionId} peer={peerId} reason={reason}");
    }

    public void PacketReceived(NetworkPeerId peerId, ReadOnlyMemory<byte> packet, NetworkDelivery delivery, byte channel)
    {
        if (!peers.TryGetValue(peerId, out PeerState? peer))
        {
            DisconnectProtocolViolation(peerId);
            return;
        }

        if (peer.Rejected)
            DisconnectProtocolViolation(peerId);
        else if (peer.SessionId is null)
            HandleAdmission(peerId, peer, packet.Span, delivery, channel);
        else
            HandleGameplay(peerId, peer.SessionId.Value, packet.Span, delivery, channel);
    }

    public void NetworkError(NetworkEndpoint? endpoint, SocketError socketError) =>
        Console.Error.WriteLine($"STARFALL_WORLD_NETWORK_ERROR endpoint={endpoint?.ToString() ?? "unknown"} error={socketError}");

    public void LatencyUpdated(NetworkPeerId peerId, int latencyMilliseconds)
    {
    }

    public void Dispose()
    {
        if (disposed)
            return;
        foreach (PeerState peer in peers.Values)
        {
            if (peer.SessionId is { } sessionId && runtime.State is WorldChannelLifecycleState.Running or WorldChannelLifecycleState.Draining)
                runtime.TerminateGameplaySession(sessionId);
        }
        peers.Clear();
        sessionPeers.Clear();
        basicArrow.Clear();
        developmentCommands.Clear();
        transport.Dispose();
        disposed = true;
    }

    private void HandleAdmission(NetworkPeerId peerId, PeerState peer, ReadOnlySpan<byte> packet, NetworkDelivery delivery, byte channel)
    {
        if (channel != StarfallNetworkChannels.Admission || delivery != NetworkDelivery.ReliableOrdered ||
            !WorldJoinAdmissionCodec.TryDecodeRequest(packet, out WorldJoinRequest? request))
        {
            Reject(peerId, new WorldJoinRejected(WorldJoinRejectionReason.InvalidTicket));
            return;
        }

        WorldJoinAdmissionOutcome outcome = admission.Handle(request, timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
        if (!outcome.IsAccepted)
        {
            Reject(peerId, outcome.Rejected!);
            return;
        }

        GameplaySessionId sessionId = outcome.Accepted!.SessionId;
        peer.SessionId = sessionId;
        sessionPeers.Add(sessionId, peerId);
        if (!TrySend(peerId, WorldJoinAdmissionCodec.EncodeAccepted(outcome.Accepted), NetworkDelivery.ReliableOrdered, StarfallNetworkChannels.Admission))
            return;
        Console.WriteLine($"STARFALL_WORLD_SESSION_ACCEPTED session={sessionId} peer={peerId} players={runtime.PlayerCount}");
        PublishSnapshots();
    }

    private void HandleGameplay(NetworkPeerId peerId, GameplaySessionId sessionId, ReadOnlySpan<byte> packet, NetworkDelivery delivery, byte channel)
    {
        if (channel == StarfallNetworkChannels.MovementCommands && delivery == NetworkDelivery.ReliableSequenced)
        {
            HandleMovement(peerId, sessionId, packet);
            return;
        }
        if (channel == StarfallNetworkChannels.BasicArrowCommands && delivery == NetworkDelivery.ReliableSequenced)
        {
            HandleBasicArrow(peerId, sessionId, packet);
            return;
        }
        if (channel == StarfallNetworkChannels.DevelopmentCommands && delivery == NetworkDelivery.ReliableOrdered)
        {
            HandleDevelopmentCommand(peerId, sessionId, packet);
            return;
        }

        DisconnectProtocolViolation(peerId);
    }

    private void HandleMovement(NetworkPeerId peerId, GameplaySessionId sessionId, ReadOnlySpan<byte> packet)
    {
        WorldWalkingCommandOutcome outcome = walking.HandleCommand(sessionId, packet);
        if (outcome.Disposition is WorldWalkingCommandDisposition.MalformedPayload or WorldWalkingCommandDisposition.UnknownSession)
        {
            DisconnectProtocolViolation(peerId);
            return;
        }
        if (outcome.CorrectionPayload is { } correction)
            _ = TrySend(peerId, correction, NetworkDelivery.ReliableOrdered, StarfallNetworkChannels.MovementCorrections);
        if (outcome.BasicArrowCancellation is { } cancellation)
            PublishBasicArrowOutcomes([cancellation]);
    }

    private void HandleBasicArrow(NetworkPeerId peerId, GameplaySessionId sessionId, ReadOnlySpan<byte> packet)
    {
        WorldBasicArrowCommandOutcome outcome = basicArrow.HandleCommand(sessionId, packet);
        if (outcome.Disposition is WorldBasicArrowCommandDisposition.MalformedPayload or WorldBasicArrowCommandDisposition.UnknownSession)
        {
            DisconnectProtocolViolation(peerId);
            return;
        }
        if (outcome.Payload is { } payload)
            _ = TrySend(peerId, payload, NetworkDelivery.ReliableOrdered, StarfallNetworkChannels.BasicArrowOutcomes);
    }

    private void HandleDevelopmentCommand(NetworkPeerId peerId, GameplaySessionId sessionId, ReadOnlySpan<byte> packet)
    {
        WorldDevelopmentCommandOutcome outcome = developmentCommands.Handle(sessionId, packet);
        if (outcome.Disposition is WorldDevelopmentCommandDisposition.MalformedPayload or
            WorldDevelopmentCommandDisposition.UnknownSession)
        {
            DisconnectProtocolViolation(peerId);
            return;
        }

        if (outcome.Payload is not { } payload)
            throw new InvalidOperationException("A handled development command must carry a result payload.");

        _ = TrySend(
            peerId,
            payload,
            NetworkDelivery.ReliableOrdered,
            StarfallNetworkChannels.DevelopmentCommandResults);
    }

    private void PublishBasicArrowOutcomes(IReadOnlyList<Starfall.Simulation.Combat.BasicArrowResolution> resolutions)
    {
        foreach (WorldBasicArrowOutcomePublication publication in basicArrow.CaptureResolutions(resolutions))
        {
            if (sessionPeers.TryGetValue(publication.SessionId, out NetworkPeerId peerId))
                _ = TrySend(peerId, publication.Payload, NetworkDelivery.ReliableOrdered, StarfallNetworkChannels.BasicArrowOutcomes);
        }
    }

    private void PublishSnapshots()
    {
        foreach (WorldWalkingSnapshotPublication publication in walking.CaptureSnapshots())
        {
            if (sessionPeers.TryGetValue(publication.SessionId, out NetworkPeerId peerId))
            {
                _ = TrySend(peerId, publication.Payload, NetworkDelivery.Sequenced, StarfallNetworkChannels.MovementSnapshots);
            }
        }

        foreach (WorldMonsterSnapshotPublication publication in monsters.CaptureSnapshots())
        {
            if (sessionPeers.TryGetValue(publication.SessionId, out NetworkPeerId peerId))
            {
                _ = TrySend(peerId, publication.Payload, NetworkDelivery.Sequenced, StarfallNetworkChannels.MonsterSnapshots);
            }
        }
    }

    private void ExpireAdmissions()
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        foreach ((NetworkPeerId peerId, PeerState peer) in peers.ToArray())
        {
            if (peer.SessionId is null && now >= peer.Deadline)
                TryDisconnect(peerId);
        }
    }

    private void Reject(NetworkPeerId peerId, WorldJoinRejected rejection)
    {
        if (peers.TryGetValue(peerId, out PeerState? peer))
            peer.Rejected = true;
        _ = TrySend(peerId, WorldJoinAdmissionCodec.EncodeRejected(rejection), NetworkDelivery.ReliableOrdered, StarfallNetworkChannels.Admission);
    }

    private void DisconnectProtocolViolation(NetworkPeerId peerId)
    {
        Console.Error.WriteLine($"STARFALL_WORLD_PROTOCOL_VIOLATION peer={peerId}");
        CleanupPeer(peerId);
        TryDisconnect(peerId);
    }

    private bool TrySend(
        NetworkPeerId peerId,
        ReadOnlySpan<byte> payload,
        NetworkDelivery delivery,
        byte channel)
    {
        try
        {
            transport.Send(peerId, payload, delivery, channel);
            return true;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"STARFALL_WORLD_SEND_FAILURE peer={peerId} channel={channel} error={exception.Message}");
            CleanupPeer(peerId);
            TryDisconnect(peerId);
            return false;
        }
    }

    private void CleanupPeer(NetworkPeerId peerId)
    {
        if (!peers.Remove(peerId, out PeerState? peer) || peer.SessionId is not { } sessionId)
            return;
        sessionPeers.Remove(sessionId);
        basicArrow.RemoveSession(sessionId);
        developmentCommands.RemoveSession(sessionId);
        runtime.TerminateGameplaySession(sessionId);
    }

    private void TryDisconnect(NetworkPeerId peerId)
    {
        try
        {
            transport.Disconnect(peerId);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"STARFALL_WORLD_DISCONNECT_FAILURE peer={peerId} error={exception.Message}");
        }
    }

    private sealed class PeerState(DateTimeOffset deadline)
    {
        internal DateTimeOffset Deadline { get; } = deadline;
        internal GameplaySessionId? SessionId
        {
            get; set;
        }

        internal bool Rejected
        {
            get; set;
        }
    }
}
