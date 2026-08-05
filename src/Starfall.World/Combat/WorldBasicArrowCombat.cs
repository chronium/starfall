using Starfall.Simulation.Combat;
using Starfall.Simulation.Entities;

namespace Starfall.World.Combat;

internal sealed class WorldBasicArrowCombat
{
    private readonly Dictionary<WorldEntityId, PendingBasicArrow> pendingByActor = [];
    private readonly Dictionary<WorldEntityId, ulong> nextAllowedStartByActor = [];

    internal int PendingCount => pendingByActor.Count;

    internal BasicArrowStartEvaluation TryStart(
        BasicArrowIntent intent,
        BasicArrowActorState actor,
        BasicArrowTargetState target,
        ulong currentTick)
    {
        ulong nextAllowedStartTick = nextAllowedStartByActor.GetValueOrDefault(intent.ActorId);
        BasicArrowStartEvaluation evaluation = Draft0BasicArrowRules.TryStart(
            Draft0BasicArrowTuning.FirstPlayable,
            intent,
            actor,
            target,
            currentTick,
            nextAllowedStartTick,
            pendingByActor.ContainsKey(intent.ActorId));
        if (evaluation.PendingAction is not { } pending)
            return evaluation;

        pendingByActor.Add(intent.ActorId, pending);
        nextAllowedStartByActor[intent.ActorId] = pending.NextAllowedStartTick;
        return evaluation;
    }

    internal BasicArrowResolution? CancelForMovement(WorldEntityId actorId, ulong currentTick)
    {
        if (!pendingByActor.Remove(actorId, out PendingBasicArrow pending))
            return null;

        return BasicArrowResolution.Cancel(
            pending,
            currentTick,
            BasicArrowResolutionDisposition.CanceledByMovement);
    }

    internal IReadOnlyList<PendingBasicArrow> TakeDue(ulong currentTick)
    {
        PendingBasicArrow[] overdue = pendingByActor.Values
            .Where(action => action.ResolveTick < currentTick)
            .ToArray();
        if (overdue.Length != 0)
            throw new InvalidOperationException("A pending Basic Arrow passed its exact resolve tick.");

        PendingBasicArrow[] due = pendingByActor.Values
            .Where(action => action.ResolveTick == currentTick)
            .OrderBy(static action => action.ActorId.Value)
            .ToArray();
        foreach (PendingBasicArrow action in due)
            pendingByActor.Remove(action.ActorId);
        return Array.AsReadOnly(due);
    }

    internal bool TryGetPending(WorldEntityId actorId, out PendingBasicArrow pending) =>
        pendingByActor.TryGetValue(actorId, out pending);

    internal ulong GetNextAllowedStartTick(WorldEntityId actorId) =>
        nextAllowedStartByActor.GetValueOrDefault(actorId);

    internal void RemoveActor(WorldEntityId actorId)
    {
        pendingByActor.Remove(actorId);
        nextAllowedStartByActor.Remove(actorId);
    }

    internal void Clear()
    {
        pendingByActor.Clear();
        nextAllowedStartByActor.Clear();
    }
}
