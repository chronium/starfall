using System.Diagnostics;
using System.Net.Sockets;
using ChronoFall.Network.Transport;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Combat;
using Starfall.Protocol.Compatibility;
using Starfall.Protocol.Development;
using Starfall.Protocol.Monsters;
using Starfall.Protocol.Movement;
using Starfall.Protocol.Networking;

namespace Starfall.Client.Networking;

internal sealed class ConnectedWalkingClientSession : INetworkEventHandler, IDisposable
{
    internal static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(10);
    internal const int MaximumOutstandingBasicArrowCommands = 64;
    internal const int MaximumDevelopmentCommandLifecycles = 64;
    private readonly INetworkTransport transport;
    private readonly WorldJoinRequest request;
    private NetworkPeerId peerId;
    private bool peerAssigned;
    private bool connected;
    private bool admissionAccepted;
    private bool disposed;
    private string? failure;
    private ulong nextIntentSequence = 1;
    private ulong nextCombatSequence;
    private ulong nextDevelopmentSequence;
    private ulong lastSentIntentSequence;
    private ulong lastSnapshotSequence;
    private ulong lastTick;
    private ulong lastMonsterSnapshotSequence;
    private ulong lastMonsterTick;
    private ulong? entityId;
    private readonly Dictionary<ulong, SentBasicArrowCommand> basicArrowCommands = [];
    private readonly Queue<ConnectedBasicArrowOutcome> basicArrowOutcomes = [];
    private readonly Dictionary<ulong, DevelopmentCommandId> developmentCommands = [];
    private readonly Queue<ConnectedDevelopmentCommandResult> developmentResults = [];

    internal ConnectedWalkingClientSession(
        INetworkTransport transport,
        string ticket,
        ulong initialCombatSequence = 1,
        ulong initialDevelopmentSequence = 1)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        if (initialCombatSequence == 0)
            throw new ArgumentOutOfRangeException(nameof(initialCombatSequence));
        if (initialDevelopmentSequence == 0)
            throw new ArgumentOutOfRangeException(nameof(initialDevelopmentSequence));
        request = new WorldJoinRequest(StarfallGameplayProtocol.CurrentVersion, ticket);
        nextCombatSequence = initialCombatSequence;
        nextDevelopmentSequence = initialDevelopmentSequence;
    }

    internal GameplaySessionId? SessionId
    {
        get; private set;
    }
    internal ProtocolVersion? ProtocolVersion
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

    internal CombatCommandSequence SendBasicArrowIntent(WorldEntityId targetEntityId)
    {
        if (!IsReady)
            throw new InvalidOperationException("Connected gameplay session is not ready.");
        if (targetEntityId.Value == 0)
            throw new ArgumentException("Basic Arrow target identity must be valid.", nameof(targetEntityId));
        if (nextCombatSequence == 0)
            throw new InvalidOperationException("Basic Arrow command sequence space is exhausted.");
        if (basicArrowCommands.Count >= MaximumOutstandingBasicArrowCommands)
            throw new InvalidOperationException("Too many Basic Arrow commands are awaiting authoritative correlation.");

        ulong sequenceValue = nextCombatSequence;
        nextCombatSequence = sequenceValue == ulong.MaxValue ? 0 : sequenceValue + 1;
        var sequence = new CombatCommandSequence(sequenceValue);
        var command = new BasicArrowCommand(sequence, targetEntityId);
        transport.Send(
            peerId,
            ConnectedBasicArrowCodec.EncodeCommand(command),
            NetworkDelivery.ReliableSequenced,
            StarfallNetworkChannels.BasicArrowCommands);
        basicArrowCommands.Add(sequenceValue, new SentBasicArrowCommand(targetEntityId));
        return sequence;
    }

    internal DevelopmentCommandSequence SendDevelopmentCommand(
        DevelopmentCommandId commandId,
        IEnumerable<string> arguments)
    {
        if (!IsReady)
            throw new InvalidOperationException("Connected gameplay session is not ready.");
        if (nextDevelopmentSequence == 0)
            throw new InvalidOperationException("Development command sequence space is exhausted.");
        if (developmentCommands.Count + developmentResults.Count >= MaximumDevelopmentCommandLifecycles)
        {
            throw new InvalidOperationException(
                "Too many development commands are awaiting authoritative correlation or Client consumption.");
        }

        ulong sequenceValue = nextDevelopmentSequence;
        nextDevelopmentSequence = sequenceValue == ulong.MaxValue ? 0 : sequenceValue + 1;
        var sequence = new DevelopmentCommandSequence(sequenceValue);
        var command = new DevelopmentCommandRequest(sequence, commandId, arguments);
        transport.Send(
            peerId,
            DevelopmentCommandCodec.EncodeRequest(command),
            NetworkDelivery.ReliableOrdered,
            StarfallNetworkChannels.DevelopmentCommands);
        developmentCommands.Add(sequenceValue, commandId);
        return sequence;
    }

    internal bool TryDequeueDevelopmentCommandResult(out ConnectedDevelopmentCommandResult? result) =>
        developmentResults.TryDequeue(out result);

    internal bool TryDequeueBasicArrowOutcome(out ConnectedBasicArrowOutcome outcome) =>
        basicArrowOutcomes.TryDequeue(out outcome!);

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
        if (channel == StarfallNetworkChannels.BasicArrowOutcomes)
        {
            if (!admissionAccepted || delivery != NetworkDelivery.ReliableOrdered ||
                !TryAcceptBasicArrowOutcome(packet.Span))
            {
                failure = "World sent malformed, misrouted or inconsistent Basic Arrow outcome data.";
            }
            return;
        }
        if (channel == StarfallNetworkChannels.DevelopmentCommandResults)
        {
            if (!admissionAccepted || delivery != NetworkDelivery.ReliableOrdered ||
                !TryAcceptDevelopmentCommandResult(packet.Span))
            {
                failure = "World sent malformed, misrouted or inconsistent development command result data.";
            }
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
                if (accepted.SelectedProtocolVersion != request.ProtocolVersion)
                {
                    failure = $"World selected incompatible gameplay protocol version {accepted.SelectedProtocolVersion}.";
                    return;
                }
                SessionId = accepted.SessionId;
                ProtocolVersion = accepted.SelectedProtocolVersion;
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

    private bool TryAcceptBasicArrowOutcome(ReadOnlySpan<byte> payload)
    {
        if (!ConnectedBasicArrowCodec.TryReadPayloadKind(payload, out BasicArrowPayloadKind kind))
            return false;

        return kind switch
        {
            BasicArrowPayloadKind.Accepted =>
                ConnectedBasicArrowCodec.TryDecodeAccepted(payload, out BasicArrowAccepted? accepted) &&
                TryAccept(accepted),
            BasicArrowPayloadKind.Rejected =>
                ConnectedBasicArrowCodec.TryDecodeRejected(payload, out BasicArrowRejected? rejected) &&
                TryAccept(rejected),
            BasicArrowPayloadKind.Canceled =>
                ConnectedBasicArrowCodec.TryDecodeCanceled(payload, out BasicArrowCanceled? canceled) &&
                TryAccept(canceled),
            BasicArrowPayloadKind.Resolved =>
                ConnectedBasicArrowCodec.TryDecodeResolved(payload, out BasicArrowResolved? resolved) &&
                TryAccept(resolved),
            _ => false,
        };
    }

    private bool TryAcceptDevelopmentCommandResult(ReadOnlySpan<byte> payload)
    {
        if (!DevelopmentCommandCodec.TryReadResultPayloadKind(
                payload,
                out DevelopmentCommandResultPayloadKind kind))
        {
            return false;
        }

        ConnectedDevelopmentCommandResult? result = kind switch
        {
            DevelopmentCommandResultPayloadKind.Succeeded when
                DevelopmentCommandCodec.TryDecodeSucceeded(payload, out DevelopmentCommandSucceeded? succeeded) =>
                ConnectedDevelopmentCommandResult.Succeeded(succeeded),
            DevelopmentCommandResultPayloadKind.Rejected when
                DevelopmentCommandCodec.TryDecodeRejected(payload, out DevelopmentCommandRejected? rejected) =>
                ConnectedDevelopmentCommandResult.Rejected(rejected),
            _ => null,
        };
        if (result is null ||
            !developmentCommands.TryGetValue(result.Sequence.Value, out DevelopmentCommandId expectedCommandId) ||
            expectedCommandId != result.CommandId)
        {
            return false;
        }

        developmentCommands.Remove(result.Sequence.Value);
        developmentResults.Enqueue(result);
        return true;
    }

    private bool TryAccept(BasicArrowAccepted accepted)
    {
        if (!TryGetMatchingCommand(accepted.Sequence, accepted.ActorEntityId, accepted.TargetEntityId, out SentBasicArrowCommand? command) ||
            command is null ||
            command.Accepted)
        {
            return false;
        }

        DiscardSupersededUnaccepted(accepted.Sequence.Value);
        command.Accepted = true;
        EnqueueBasicArrowOutcome(ConnectedBasicArrowOutcome.Accepted(accepted));
        return true;
    }

    private bool TryAccept(BasicArrowRejected rejected)
    {
        if (!TryGetMatchingCommand(rejected.Sequence, rejected.ActorEntityId, rejected.TargetEntityId, out SentBasicArrowCommand? command) ||
            command is null ||
            command.Accepted)
        {
            return false;
        }

        DiscardSupersededUnaccepted(rejected.Sequence.Value);
        basicArrowCommands.Remove(rejected.Sequence.Value);
        EnqueueBasicArrowOutcome(ConnectedBasicArrowOutcome.Rejected(rejected));
        return true;
    }

    private bool TryAccept(BasicArrowCanceled canceled)
    {
        if (!TryGetMatchingCommand(canceled.Sequence, canceled.ActorEntityId, canceled.TargetEntityId, out SentBasicArrowCommand? command) ||
            command is null ||
            !command.Accepted)
        {
            return false;
        }

        basicArrowCommands.Remove(canceled.Sequence.Value);
        EnqueueBasicArrowOutcome(ConnectedBasicArrowOutcome.Canceled(canceled));
        return true;
    }

    private bool TryAccept(BasicArrowResolved resolved)
    {
        if (!TryGetMatchingCommand(resolved.Sequence, resolved.ActorEntityId, resolved.TargetEntityId, out SentBasicArrowCommand? command) ||
            command is null ||
            !command.Accepted)
        {
            return false;
        }

        basicArrowCommands.Remove(resolved.Sequence.Value);
        EnqueueBasicArrowOutcome(ConnectedBasicArrowOutcome.Resolved(resolved));
        return true;
    }

    private bool TryGetMatchingCommand(
        CombatCommandSequence sequence,
        WorldEntityId actorEntityId,
        WorldEntityId targetEntityId,
        out SentBasicArrowCommand? command)
    {
        command = null;
        return entityId is { } expectedActor &&
            actorEntityId.Value == expectedActor &&
            basicArrowCommands.TryGetValue(sequence.Value, out command) &&
            command.TargetEntityId == targetEntityId;
    }

    private void DiscardSupersededUnaccepted(ulong processedSequence)
    {
        foreach (ulong sequence in basicArrowCommands
                     .Where(entry => entry.Key < processedSequence && !entry.Value.Accepted)
                     .Select(static entry => entry.Key)
                     .ToArray())
        {
            basicArrowCommands.Remove(sequence);
        }
    }

    private static void WriteBasicArrowOutcomeDiagnostic(ConnectedBasicArrowOutcome outcome)
    {
        string details = outcome.Kind switch
        {
            ConnectedBasicArrowOutcomeKind.Accepted =>
                $"startTick={outcome.StartTick} resolveTick={outcome.ResolveTick}",
            ConnectedBasicArrowOutcomeKind.Rejected =>
                $"decisionTick={outcome.OutcomeTick} reason={outcome.RejectionReason}",
            ConnectedBasicArrowOutcomeKind.Canceled =>
                $"cancelTick={outcome.OutcomeTick} reason={outcome.CancellationReason}",
            ConnectedBasicArrowOutcomeKind.Resolved =>
                $"resolveTick={outcome.OutcomeTick} requested={outcome.RequestedDamageUnits} " +
                $"effective={outcome.EffectiveDamageUnits} defeated={outcome.TargetDefeated}",
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };
        Console.WriteLine(
            $"STARFALL_CLIENT_BASIC_ARROW_OUTCOME kind={outcome.Kind} sequence={outcome.Sequence} " +
            $"actor={outcome.ActorEntityId} target={outcome.TargetEntityId} {details}");
    }

    private void EnqueueBasicArrowOutcome(ConnectedBasicArrowOutcome outcome)
    {
        basicArrowOutcomes.Enqueue(outcome);
        WriteBasicArrowOutcomeDiagnostic(outcome);
    }

    private sealed class SentBasicArrowCommand(WorldEntityId targetEntityId)
    {
        internal WorldEntityId TargetEntityId
        {
            get;
        } = targetEntityId;

        internal bool Accepted
        {
            get; set;
        }
    }
}

internal enum ConnectedDevelopmentCommandResultKind
{
    Succeeded,
    Rejected,
}

internal sealed record ConnectedDevelopmentCommandResult(
    ConnectedDevelopmentCommandResultKind Kind,
    DevelopmentCommandSequence Sequence,
    DevelopmentCommandId CommandId,
    DevelopmentCommandRejectionReason? RejectionReason,
    string Diagnostic)
{
    internal static ConnectedDevelopmentCommandResult Succeeded(DevelopmentCommandSucceeded value) => new(
        ConnectedDevelopmentCommandResultKind.Succeeded,
        value.Sequence,
        value.CommandId,
        null,
        value.Diagnostic);

    internal static ConnectedDevelopmentCommandResult Rejected(DevelopmentCommandRejected value) => new(
        ConnectedDevelopmentCommandResultKind.Rejected,
        value.Sequence,
        value.CommandId,
        value.Reason,
        value.Diagnostic);
}

internal enum ConnectedBasicArrowOutcomeKind
{
    Accepted,
    Rejected,
    Canceled,
    Resolved,
}

internal sealed record ConnectedBasicArrowOutcome(
    ConnectedBasicArrowOutcomeKind Kind,
    CombatCommandSequence Sequence,
    WorldEntityId ActorEntityId,
    WorldEntityId TargetEntityId,
    ulong StartTick,
    ulong ResolveTick,
    ulong OutcomeTick,
    BasicArrowRejectionReason? RejectionReason,
    BasicArrowCancellationReason? CancellationReason,
    int? RequestedDamageUnits,
    int? EffectiveDamageUnits,
    bool? TargetDefeated)
{
    internal static ConnectedBasicArrowOutcome Accepted(BasicArrowAccepted value) => new(
        ConnectedBasicArrowOutcomeKind.Accepted,
        value.Sequence,
        value.ActorEntityId,
        value.TargetEntityId,
        value.StartTick,
        value.ResolveTick,
        value.StartTick,
        null,
        null,
        null,
        null,
        null);

    internal static ConnectedBasicArrowOutcome Rejected(BasicArrowRejected value) => new(
        ConnectedBasicArrowOutcomeKind.Rejected,
        value.Sequence,
        value.ActorEntityId,
        value.TargetEntityId,
        value.DecisionTick,
        value.DecisionTick,
        value.DecisionTick,
        value.Reason,
        null,
        null,
        null,
        null);

    internal static ConnectedBasicArrowOutcome Canceled(BasicArrowCanceled value) => new(
        ConnectedBasicArrowOutcomeKind.Canceled,
        value.Sequence,
        value.ActorEntityId,
        value.TargetEntityId,
        value.StartTick,
        value.ResolveTick,
        value.CancellationTick,
        null,
        value.Reason,
        null,
        null,
        null);

    internal static ConnectedBasicArrowOutcome Resolved(BasicArrowResolved value) => new(
        ConnectedBasicArrowOutcomeKind.Resolved,
        value.Sequence,
        value.ActorEntityId,
        value.TargetEntityId,
        value.StartTick,
        value.ResolveTick,
        value.ResolveTick,
        null,
        null,
        value.RequestedDamageUnits,
        value.EffectiveDamageUnits,
        value.TargetDefeated);
}
