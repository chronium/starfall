using System.Numerics;
using Starfall.Content.Characters;
using Starfall.Content.Zones;
using Starfall.Simulation.Entities;

namespace Starfall.Simulation.Combat;

public enum BasicArrowStartDisposition
{
    Accepted,
    UnknownActor,
    UnknownTarget,
    WrongAction,
    ActorIsTarget,
    ActionAlreadyPending,
    CadenceNotReady,
    TargetCoincident,
    TargetOutOfRange,
}

public enum BasicArrowResolutionDisposition
{
    Resolved,
    CanceledByMovement,
    ActorUnavailable,
    TargetUnavailable,
    ActorMoving,
    TargetCoincident,
    TargetOutOfRange,
    TargetOutsideFacing,
}

public readonly record struct BasicArrowIntent
{
    public BasicArrowIntent(string actionId, WorldEntityId actorId, WorldEntityId targetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        if (actorId.Value == 0)
            throw new ArgumentException("Actor identity must be valid.", nameof(actorId));
        if (targetId.Value == 0)
            throw new ArgumentException("Target identity must be valid.", nameof(targetId));
        ActionId = actionId;
        ActorId = actorId;
        TargetId = targetId;
    }

    public string ActionId { get; }

    public WorldEntityId ActorId { get; }

    public WorldEntityId TargetId { get; }
}

public readonly record struct BasicArrowActorState
{
    private const float FacingLengthTolerance = 1e-4f;

    public BasicArrowActorState(
        WorldEntityId entityId,
        GroundPoint position,
        Vector2 velocityMetresPerSecond,
        Vector2 facing)
    {
        if (entityId.Value == 0)
            throw new ArgumentException("Actor identity must be valid.", nameof(entityId));
        if (!IsFinite(velocityMetresPerSecond))
            throw new ArgumentException("Actor velocity must be finite.", nameof(velocityMetresPerSecond));
        if (!IsFinite(facing) || MathF.Abs(facing.Length() - 1.0f) > FacingLengthTolerance)
            throw new ArgumentException("Actor facing must be finite and normalized.", nameof(facing));

        EntityId = entityId;
        Position = position;
        VelocityMetresPerSecond = velocityMetresPerSecond;
        Facing = facing;
    }

    public WorldEntityId EntityId { get; }

    public GroundPoint Position { get; }

    public Vector2 VelocityMetresPerSecond { get; }

    public Vector2 Facing { get; }

    private static bool IsFinite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);
}

public readonly record struct BasicArrowTargetState
{
    public BasicArrowTargetState(
        WorldEntityId entityId,
        GroundPoint position,
        int healthUnits)
    {
        if (entityId.Value == 0)
            throw new ArgumentException("Target identity must be valid.", nameof(entityId));
        if (healthUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(healthUnits));

        EntityId = entityId;
        Position = position;
        HealthUnits = healthUnits;
    }

    public WorldEntityId EntityId { get; }

    public GroundPoint Position { get; }

    public int HealthUnits { get; }
}

public sealed class Draft0BasicArrowTuning
{
    public const float Draft0MaximumRangeMetres = 12.0f;
    public const ulong Draft0ResolveDelayTicks = 12;
    public const ulong Draft0CadenceTicks = 48;
    public const float Draft0MinimumFacingDot = 0.70710677f;

    public Draft0BasicArrowTuning(
        string actionId,
        int damageUnits,
        float maximumRangeMetres,
        float minimumFacingDot,
        ulong resolveDelayTicks,
        ulong cadenceTicks)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        if (damageUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(damageUnits));
        if (!float.IsFinite(maximumRangeMetres) || maximumRangeMetres <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(maximumRangeMetres));
        if (!float.IsFinite(minimumFacingDot) || minimumFacingDot < -1.0f || minimumFacingDot > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(minimumFacingDot));
        if (resolveDelayTicks == 0)
            throw new ArgumentOutOfRangeException(nameof(resolveDelayTicks));
        if (cadenceTicks < resolveDelayTicks)
            throw new ArgumentOutOfRangeException(nameof(cadenceTicks));

        ActionId = actionId;
        DamageUnits = damageUnits;
        MaximumRangeMetres = maximumRangeMetres;
        MinimumFacingDot = minimumFacingDot;
        ResolveDelayTicks = resolveDelayTicks;
        CadenceTicks = cadenceTicks;
    }

    public string ActionId { get; }

    public int DamageUnits { get; }

    public float MaximumRangeMetres { get; }

    public float MinimumFacingDot { get; }

    public ulong ResolveDelayTicks { get; }

    public ulong CadenceTicks { get; }

    public static Draft0BasicArrowTuning FirstPlayable { get; } = CreateFirstPlayable();

    private static Draft0BasicArrowTuning CreateFirstPlayable()
    {
        Draft0ActionDefinition action = Draft0ArcherCatalog.FirstPlayable.Actions
            .Single(static candidate => string.Equals(candidate.Id, "basic_arrow", StringComparison.Ordinal));
        if (action.TargetKind != Draft0ActionTargetKind.SelectedEntity || action.UsesMana)
            throw new InvalidOperationException("The Draft 0 Basic Arrow content contract is incompatible with its authoritative rule.");

        return new(
            action.Id,
            action.AuthoritativeDamageUnits,
            Draft0MaximumRangeMetres,
            Draft0MinimumFacingDot,
            Draft0ResolveDelayTicks,
            Draft0CadenceTicks);
    }
}

public readonly record struct PendingBasicArrow
{
    private const float FacingLengthTolerance = 1e-4f;

    public PendingBasicArrow(
        string actionId,
        WorldEntityId actorId,
        WorldEntityId targetId,
        ulong startTick,
        ulong resolveTick,
        ulong nextAllowedStartTick,
        Vector2 acceptedFacing)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        if (actorId.Value == 0)
            throw new ArgumentException("Actor identity must be valid.", nameof(actorId));
        if (targetId.Value == 0)
            throw new ArgumentException("Target identity must be valid.", nameof(targetId));
        if (actorId == targetId)
            throw new ArgumentException("Basic Arrow actor and target must differ.", nameof(targetId));
        if (resolveTick <= startTick)
            throw new ArgumentOutOfRangeException(nameof(resolveTick));
        if (nextAllowedStartTick < resolveTick)
            throw new ArgumentOutOfRangeException(nameof(nextAllowedStartTick));
        if (!float.IsFinite(acceptedFacing.X) ||
            !float.IsFinite(acceptedFacing.Y) ||
            MathF.Abs(acceptedFacing.Length() - 1.0f) > FacingLengthTolerance)
        {
            throw new ArgumentException("Accepted facing must be finite and normalized.", nameof(acceptedFacing));
        }

        ActionId = actionId;
        ActorId = actorId;
        TargetId = targetId;
        StartTick = startTick;
        ResolveTick = resolveTick;
        NextAllowedStartTick = nextAllowedStartTick;
        AcceptedFacing = acceptedFacing;
    }

    public string ActionId { get; }

    public WorldEntityId ActorId { get; }

    public WorldEntityId TargetId { get; }

    public ulong StartTick { get; }

    public ulong ResolveTick { get; }

    public ulong NextAllowedStartTick { get; }

    public Vector2 AcceptedFacing { get; }
}

public readonly record struct BasicArrowStartEvaluation(
    BasicArrowStartDisposition Disposition,
    PendingBasicArrow? PendingAction)
{
    public static BasicArrowStartEvaluation Reject(BasicArrowStartDisposition disposition)
    {
        if (disposition == BasicArrowStartDisposition.Accepted)
            throw new ArgumentOutOfRangeException(nameof(disposition));
        return new(disposition, null);
    }
}

public readonly record struct AuthoritativeDamageResult(
    int RequestedDamageUnits,
    int AppliedDamageUnits,
    int PreviousHealthUnits,
    int RemainingHealthUnits,
    bool Defeated);

public readonly record struct BasicArrowResolution(
    PendingBasicArrow Action,
    ulong OutcomeTick,
    BasicArrowResolutionDisposition Disposition,
    AuthoritativeDamageResult? Damage)
{
    public static BasicArrowResolution Cancel(
        PendingBasicArrow action,
        ulong outcomeTick,
        BasicArrowResolutionDisposition disposition)
    {
        if (disposition == BasicArrowResolutionDisposition.Resolved)
            throw new ArgumentOutOfRangeException(nameof(disposition));
        return new(action, outcomeTick, disposition, null);
    }
}

public static class AuthoritativeIntegerDamage
{
    public static AuthoritativeDamageResult Apply(int currentHealthUnits, int requestedDamageUnits)
    {
        if (currentHealthUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(currentHealthUnits));
        if (requestedDamageUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedDamageUnits));

        int appliedDamageUnits = Math.Min(currentHealthUnits, requestedDamageUnits);
        int remainingHealthUnits = currentHealthUnits - appliedDamageUnits;
        return new(
            requestedDamageUnits,
            appliedDamageUnits,
            currentHealthUnits,
            remainingHealthUnits,
            remainingHealthUnits == 0);
    }
}

public static class Draft0BasicArrowRules
{
    private const float FacingComparisonTolerance = 1e-6f;

    public static BasicArrowStartEvaluation TryStart(
        Draft0BasicArrowTuning tuning,
        BasicArrowIntent intent,
        BasicArrowActorState actor,
        BasicArrowTargetState target,
        ulong currentTick,
        ulong nextAllowedStartTick,
        bool hasPendingAction)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        if (!string.Equals(intent.ActionId, tuning.ActionId, StringComparison.Ordinal))
            return BasicArrowStartEvaluation.Reject(BasicArrowStartDisposition.WrongAction);
        if (intent.ActorId != actor.EntityId)
            throw new ArgumentException("Intent and actor identities must match.", nameof(actor));
        if (intent.TargetId != target.EntityId)
            throw new ArgumentException("Intent and target identities must match.", nameof(target));
        if (intent.ActorId == intent.TargetId)
            return BasicArrowStartEvaluation.Reject(BasicArrowStartDisposition.ActorIsTarget);
        if (hasPendingAction)
            return BasicArrowStartEvaluation.Reject(BasicArrowStartDisposition.ActionAlreadyPending);
        if (currentTick < nextAllowedStartTick)
            return BasicArrowStartEvaluation.Reject(BasicArrowStartDisposition.CadenceNotReady);

        Vector2 difference = ToPlane(target.Position) - ToPlane(actor.Position);
        float distanceSquared = difference.LengthSquared();
        if (distanceSquared == 0.0f)
            return BasicArrowStartEvaluation.Reject(BasicArrowStartDisposition.TargetCoincident);
        if (distanceSquared > tuning.MaximumRangeMetres * tuning.MaximumRangeMetres)
            return BasicArrowStartEvaluation.Reject(BasicArrowStartDisposition.TargetOutOfRange);

        ulong resolveTick = checked(currentTick + tuning.ResolveDelayTicks);
        ulong nextStartTick = checked(currentTick + tuning.CadenceTicks);
        var pending = new PendingBasicArrow(
            intent.ActionId,
            intent.ActorId,
            intent.TargetId,
            currentTick,
            resolveTick,
            nextStartTick,
            Vector2.Normalize(difference));
        return new(BasicArrowStartDisposition.Accepted, pending);
    }

    public static BasicArrowResolution Resolve(
        Draft0BasicArrowTuning tuning,
        PendingBasicArrow action,
        BasicArrowActorState actor,
        BasicArrowTargetState target,
        ulong currentTick)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        if (!string.Equals(action.ActionId, tuning.ActionId, StringComparison.Ordinal))
            throw new ArgumentException("Pending action and tuning identities must match.", nameof(action));
        if (currentTick != action.ResolveTick)
            throw new ArgumentOutOfRangeException(nameof(currentTick), "Basic Arrow must resolve at its exact fixed tick.");
        if (actor.EntityId != action.ActorId)
            throw new ArgumentException("Pending action and actor identities must match.", nameof(actor));
        if (target.EntityId != action.TargetId)
            throw new ArgumentException("Pending action and target identities must match.", nameof(target));
        if (actor.VelocityMetresPerSecond != Vector2.Zero)
            return BasicArrowResolution.Cancel(action, currentTick, BasicArrowResolutionDisposition.ActorMoving);

        Vector2 difference = ToPlane(target.Position) - ToPlane(actor.Position);
        float distanceSquared = difference.LengthSquared();
        if (distanceSquared == 0.0f)
            return BasicArrowResolution.Cancel(action, currentTick, BasicArrowResolutionDisposition.TargetCoincident);
        if (distanceSquared > tuning.MaximumRangeMetres * tuning.MaximumRangeMetres)
            return BasicArrowResolution.Cancel(action, currentTick, BasicArrowResolutionDisposition.TargetOutOfRange);

        Vector2 targetDirection = Vector2.Normalize(difference);
        if (Vector2.Dot(actor.Facing, targetDirection) + FacingComparisonTolerance < tuning.MinimumFacingDot)
            return BasicArrowResolution.Cancel(action, currentTick, BasicArrowResolutionDisposition.TargetOutsideFacing);

        AuthoritativeDamageResult damage = AuthoritativeIntegerDamage.Apply(
            target.HealthUnits,
            tuning.DamageUnits);
        return new(action, currentTick, BasicArrowResolutionDisposition.Resolved, damage);
    }

    private static Vector2 ToPlane(GroundPoint point) =>
        new(point.XMetres, point.ZMetres);
}
