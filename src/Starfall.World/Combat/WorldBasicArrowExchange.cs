using Starfall.Protocol.Admission;
using Starfall.Protocol.Combat;
using Starfall.Simulation.Combat;
using Starfall.World.Lifecycle;
using ProtocolEntityId = Starfall.Protocol.Movement.WorldEntityId;
using SimulationEntityId = Starfall.Simulation.Entities.WorldEntityId;

namespace Starfall.World.Combat;

internal enum WorldBasicArrowCommandDisposition
{
    Accepted,
    Rejected,
    MalformedPayload,
    UnknownSession,
    StaleOrDuplicate,
}

internal sealed record WorldBasicArrowCommandOutcome(
    WorldBasicArrowCommandDisposition Disposition,
    byte[]? Payload = null);

internal sealed record WorldBasicArrowOutcomePublication(
    GameplaySessionId SessionId,
    byte[] Payload);

internal sealed class WorldBasicArrowExchange
{
    private readonly WorldChannelRuntime runtime;
    private readonly Dictionary<GameplaySessionId, SessionState> sessions = [];
    private readonly Dictionary<SimulationEntityId, PendingCorrelation> pendingByActor = [];

    internal WorldBasicArrowExchange(WorldChannelRuntime runtime)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    internal WorldBasicArrowCommandOutcome HandleCommand(
        GameplaySessionId sessionId,
        ReadOnlySpan<byte> payload)
    {
        if (!ConnectedBasicArrowCodec.TryDecodeCommand(payload, out BasicArrowCommand? command))
            return new(WorldBasicArrowCommandDisposition.MalformedPayload);
        if (!runtime.TryGetGameplaySession(sessionId, out var session) || session is null)
            return new(WorldBasicArrowCommandDisposition.UnknownSession);

        SessionState state = GetOrCreateSession(sessionId);
        if (state.LastProcessedSequence is { } lastProcessed &&
            command.Sequence.Value <= lastProcessed.Value)
        {
            return new(WorldBasicArrowCommandDisposition.StaleOrDuplicate);
        }

        state.LastProcessedSequence = command.Sequence;
        var targetId = new SimulationEntityId(command.TargetEntityId.Value);
        BasicArrowStartEvaluation evaluation = runtime.SubmitBasicArrow(
            new BasicArrowIntent(ConnectedBasicArrow.ActionId.Value, session.PlayerEntityId, targetId));
        var actorId = new ProtocolEntityId(session.PlayerEntityId.Value);

        if (evaluation.PendingAction is { } pending)
        {
            if (evaluation.Disposition != BasicArrowStartDisposition.Accepted)
                throw new InvalidOperationException("An accepted Basic Arrow pending action carried a rejection disposition.");
            if (pendingByActor.ContainsKey(session.PlayerEntityId))
                throw new InvalidOperationException($"Actor {session.PlayerEntityId} already has a connected Basic Arrow correlation.");

            pendingByActor.Add(
                session.PlayerEntityId,
                new PendingCorrelation(sessionId, command.Sequence, pending));
            return new(
                WorldBasicArrowCommandDisposition.Accepted,
                ConnectedBasicArrowCodec.EncodeAccepted(new BasicArrowAccepted(
                    command.Sequence,
                    actorId,
                    command.TargetEntityId,
                    pending.StartTick,
                    pending.ResolveTick)));
        }

        return new(
            WorldBasicArrowCommandDisposition.Rejected,
            ConnectedBasicArrowCodec.EncodeRejected(new BasicArrowRejected(
                command.Sequence,
                actorId,
                command.TargetEntityId,
                runtime.CurrentTick,
                MapRejection(evaluation.Disposition))));
    }

    internal IReadOnlyList<WorldBasicArrowOutcomePublication> CaptureResolutions(
        IReadOnlyList<BasicArrowResolution> resolutions)
    {
        ArgumentNullException.ThrowIfNull(resolutions);
        if (resolutions.Count == 0)
            return [];

        var publications = new List<WorldBasicArrowOutcomePublication>(resolutions.Count);
        foreach (BasicArrowResolution resolution in resolutions)
        {
            if (!pendingByActor.Remove(resolution.Action.ActorId, out PendingCorrelation? correlation))
                throw new InvalidOperationException($"Basic Arrow outcome for actor {resolution.Action.ActorId} has no connected correlation.");
            if (correlation.Action != resolution.Action)
                throw new InvalidOperationException($"Basic Arrow outcome for actor {resolution.Action.ActorId} does not match its connected correlation.");

            var actorId = new ProtocolEntityId(resolution.Action.ActorId.Value);
            var targetId = new ProtocolEntityId(resolution.Action.TargetId.Value);
            byte[] payload = resolution.Disposition == BasicArrowResolutionDisposition.Resolved
                ? EncodeResolved(correlation.Sequence, actorId, targetId, resolution)
                : ConnectedBasicArrowCodec.EncodeCanceled(new BasicArrowCanceled(
                    correlation.Sequence,
                    actorId,
                    targetId,
                    resolution.Action.StartTick,
                    resolution.Action.ResolveTick,
                    resolution.OutcomeTick,
                    MapCancellation(resolution.Disposition)));
            publications.Add(new(correlation.SessionId, payload));
        }

        return publications.AsReadOnly();
    }

    internal void RemoveSession(GameplaySessionId sessionId)
    {
        sessions.Remove(sessionId);
        foreach (SimulationEntityId actorId in pendingByActor
                     .Where(candidate => candidate.Value.SessionId == sessionId)
                     .Select(static candidate => candidate.Key)
                     .ToArray())
        {
            pendingByActor.Remove(actorId);
        }
    }

    internal void Clear()
    {
        sessions.Clear();
        pendingByActor.Clear();
    }

    private SessionState GetOrCreateSession(GameplaySessionId sessionId)
    {
        if (!sessions.TryGetValue(sessionId, out SessionState? state))
        {
            state = new SessionState();
            sessions.Add(sessionId, state);
        }

        return state;
    }

    private static byte[] EncodeResolved(
        CombatCommandSequence sequence,
        ProtocolEntityId actorId,
        ProtocolEntityId targetId,
        BasicArrowResolution resolution)
    {
        if (resolution.Damage is not { } damage)
            throw new InvalidOperationException("A resolved Basic Arrow outcome must carry authoritative damage.");
        return ConnectedBasicArrowCodec.EncodeResolved(new BasicArrowResolved(
            sequence,
            actorId,
            targetId,
            resolution.Action.StartTick,
            resolution.Action.ResolveTick,
            damage.RequestedDamageUnits,
            damage.AppliedDamageUnits,
            damage.Defeated));
    }

    private static BasicArrowRejectionReason MapRejection(BasicArrowStartDisposition disposition) => disposition switch
    {
        BasicArrowStartDisposition.UnknownActor => BasicArrowRejectionReason.ActorUnavailable,
        BasicArrowStartDisposition.UnknownTarget => BasicArrowRejectionReason.TargetUnavailable,
        BasicArrowStartDisposition.ActorDefeated => BasicArrowRejectionReason.ActorDefeated,
        BasicArrowStartDisposition.ActorInProtectedTown => BasicArrowRejectionReason.ActorInProtectedTown,
        BasicArrowStartDisposition.ActionAlreadyPending => BasicArrowRejectionReason.ActionAlreadyPending,
        BasicArrowStartDisposition.CadenceNotReady => BasicArrowRejectionReason.CadenceNotReady,
        BasicArrowStartDisposition.TargetCoincident => BasicArrowRejectionReason.TargetCoincident,
        BasicArrowStartDisposition.TargetOutOfRange => BasicArrowRejectionReason.TargetOutOfRange,
        BasicArrowStartDisposition.WrongAction or BasicArrowStartDisposition.ActorIsTarget =>
            throw new InvalidOperationException($"World Basic Arrow adapter produced impossible start disposition {disposition}."),
        BasicArrowStartDisposition.Accepted =>
            throw new InvalidOperationException("An accepted Basic Arrow start must carry a pending action."),
        _ => throw new ArgumentOutOfRangeException(nameof(disposition), disposition, null),
    };

    private static BasicArrowCancellationReason MapCancellation(BasicArrowResolutionDisposition disposition) => disposition switch
    {
        BasicArrowResolutionDisposition.CanceledByMovement => BasicArrowCancellationReason.CanceledByMovement,
        BasicArrowResolutionDisposition.ActorDefeated => BasicArrowCancellationReason.ActorDefeated,
        BasicArrowResolutionDisposition.ActorUnavailable => BasicArrowCancellationReason.ActorUnavailable,
        BasicArrowResolutionDisposition.TargetUnavailable => BasicArrowCancellationReason.TargetUnavailable,
        BasicArrowResolutionDisposition.ActorMoving => BasicArrowCancellationReason.ActorMoving,
        BasicArrowResolutionDisposition.TargetCoincident => BasicArrowCancellationReason.TargetCoincident,
        BasicArrowResolutionDisposition.TargetOutOfRange => BasicArrowCancellationReason.TargetOutOfRange,
        BasicArrowResolutionDisposition.TargetOutsideFacing => BasicArrowCancellationReason.TargetOutsideFacing,
        BasicArrowResolutionDisposition.Resolved =>
            throw new InvalidOperationException("A resolved Basic Arrow outcome is not a cancellation."),
        _ => throw new ArgumentOutOfRangeException(nameof(disposition), disposition, null),
    };

    private sealed class SessionState
    {
        internal CombatCommandSequence? LastProcessedSequence
        {
            get; set;
        }
    }

    private sealed record PendingCorrelation(
        GameplaySessionId SessionId,
        CombatCommandSequence Sequence,
        PendingBasicArrow Action);
}
