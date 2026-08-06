using System.Globalization;
using Starfall.Protocol.Movement;

namespace Starfall.Protocol.Combat;

public readonly record struct CombatCommandSequence
{
    public CombatCommandSequence(ulong value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Combat command sequences must be positive.");

        Value = value;
    }

    public ulong Value
    {
        get;
    }

    internal bool IsValid => Value != 0;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public readonly record struct CombatActionId
{
    public const int MaxByteLength = 64;

    public CombatActionId(string value)
    {
        if (!IsValidValue(value))
        {
            throw new ArgumentException(
                $"Combat action identities must contain 1-{MaxByteLength} lowercase ASCII letters, digits or underscores and begin with a letter.",
                nameof(value));
        }

        Value = value;
    }

    public string Value
    {
        get;
    }

    internal bool IsValid => IsValidValue(Value);

    public override string ToString() => Value ?? string.Empty;

    private static bool IsValidValue(string? value) =>
        value is { Length: > 0 and <= MaxByteLength } &&
        value[0] is >= 'a' and <= 'z' &&
        value.All(static character =>
            character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');
}

public static class ConnectedBasicArrow
{
    public const int RequestedDamageUnits = 300;

    public static CombatActionId ActionId
    {
        get;
    } = new("basic_arrow");
}

public sealed class BasicArrowCommand
{
    public BasicArrowCommand(
        CombatCommandSequence sequence,
        WorldEntityId targetEntityId)
    {
        BasicArrowFactValidation.ValidateSequence(sequence);
        BasicArrowFactValidation.ValidateEntity(targetEntityId, nameof(targetEntityId));

        Sequence = sequence;
        TargetEntityId = targetEntityId;
    }

    public CombatCommandSequence Sequence
    {
        get;
    }

    public CombatActionId ActionId => ConnectedBasicArrow.ActionId;

    public WorldEntityId TargetEntityId
    {
        get;
    }
}

public enum BasicArrowRejectionReason : byte
{
    ActorUnavailable = 0,
    TargetUnavailable = 1,
    ActorDefeated = 2,
    ActorInProtectedTown = 3,
    ActionAlreadyPending = 4,
    CadenceNotReady = 5,
    TargetCoincident = 6,
    TargetOutOfRange = 7,
}

public enum BasicArrowCancellationReason : byte
{
    CanceledByMovement = 0,
    ActorDefeated = 1,
    ActorUnavailable = 2,
    TargetUnavailable = 3,
    ActorMoving = 4,
    TargetCoincident = 5,
    TargetOutOfRange = 6,
    TargetOutsideFacing = 7,
}

public sealed class BasicArrowAccepted
{
    public BasicArrowAccepted(
        CombatCommandSequence sequence,
        WorldEntityId actorEntityId,
        WorldEntityId targetEntityId,
        ulong startTick,
        ulong resolveTick)
    {
        BasicArrowFactValidation.ValidateAction(sequence, actorEntityId, targetEntityId);
        BasicArrowFactValidation.ValidateAcceptedTicks(startTick, resolveTick);

        Sequence = sequence;
        ActorEntityId = actorEntityId;
        TargetEntityId = targetEntityId;
        StartTick = startTick;
        ResolveTick = resolveTick;
    }

    public CombatCommandSequence Sequence
    {
        get;
    }

    public CombatActionId ActionId => ConnectedBasicArrow.ActionId;

    public WorldEntityId ActorEntityId
    {
        get;
    }

    public WorldEntityId TargetEntityId
    {
        get;
    }

    public ulong StartTick
    {
        get;
    }

    public ulong ResolveTick
    {
        get;
    }
}

public sealed class BasicArrowRejected
{
    public BasicArrowRejected(
        CombatCommandSequence sequence,
        WorldEntityId actorEntityId,
        WorldEntityId targetEntityId,
        ulong decisionTick,
        BasicArrowRejectionReason reason)
    {
        BasicArrowFactValidation.ValidateAction(sequence, actorEntityId, targetEntityId);
        if (!Enum.IsDefined(reason))
            throw new ArgumentOutOfRangeException(nameof(reason));

        Sequence = sequence;
        ActorEntityId = actorEntityId;
        TargetEntityId = targetEntityId;
        DecisionTick = decisionTick;
        Reason = reason;
    }

    public CombatCommandSequence Sequence
    {
        get;
    }

    public CombatActionId ActionId => ConnectedBasicArrow.ActionId;

    public WorldEntityId ActorEntityId
    {
        get;
    }

    public WorldEntityId TargetEntityId
    {
        get;
    }

    public ulong DecisionTick
    {
        get;
    }

    public BasicArrowRejectionReason Reason
    {
        get;
    }
}

public sealed class BasicArrowCanceled
{
    public BasicArrowCanceled(
        CombatCommandSequence sequence,
        WorldEntityId actorEntityId,
        WorldEntityId targetEntityId,
        ulong startTick,
        ulong resolveTick,
        ulong cancellationTick,
        BasicArrowCancellationReason reason)
    {
        BasicArrowFactValidation.ValidateAction(sequence, actorEntityId, targetEntityId);
        BasicArrowFactValidation.ValidateAcceptedTicks(startTick, resolveTick);
        if (cancellationTick < startTick || cancellationTick > resolveTick)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cancellationTick),
                "A cancellation must occur from the accepted start through the scheduled resolve tick.");
        }
        if (!Enum.IsDefined(reason))
            throw new ArgumentOutOfRangeException(nameof(reason));

        Sequence = sequence;
        ActorEntityId = actorEntityId;
        TargetEntityId = targetEntityId;
        StartTick = startTick;
        ResolveTick = resolveTick;
        CancellationTick = cancellationTick;
        Reason = reason;
    }

    public CombatCommandSequence Sequence
    {
        get;
    }

    public CombatActionId ActionId => ConnectedBasicArrow.ActionId;

    public WorldEntityId ActorEntityId
    {
        get;
    }

    public WorldEntityId TargetEntityId
    {
        get;
    }

    public ulong StartTick
    {
        get;
    }

    public ulong ResolveTick
    {
        get;
    }

    public ulong CancellationTick
    {
        get;
    }

    public BasicArrowCancellationReason Reason
    {
        get;
    }
}

public sealed class BasicArrowResolved
{
    public BasicArrowResolved(
        CombatCommandSequence sequence,
        WorldEntityId actorEntityId,
        WorldEntityId targetEntityId,
        ulong startTick,
        ulong resolveTick,
        int requestedDamageUnits,
        int effectiveDamageUnits,
        bool targetDefeated)
    {
        BasicArrowFactValidation.ValidateAction(sequence, actorEntityId, targetEntityId);
        BasicArrowFactValidation.ValidateAcceptedTicks(startTick, resolveTick);
        if (requestedDamageUnits != ConnectedBasicArrow.RequestedDamageUnits)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedDamageUnits),
                $"Connected Basic Arrow must request exactly {ConnectedBasicArrow.RequestedDamageUnits} internal damage units.");
        }
        if (effectiveDamageUnits <= 0 || effectiveDamageUnits > requestedDamageUnits)
            throw new ArgumentOutOfRangeException(nameof(effectiveDamageUnits));

        Sequence = sequence;
        ActorEntityId = actorEntityId;
        TargetEntityId = targetEntityId;
        StartTick = startTick;
        ResolveTick = resolveTick;
        RequestedDamageUnits = requestedDamageUnits;
        EffectiveDamageUnits = effectiveDamageUnits;
        TargetDefeated = targetDefeated;
    }

    public CombatCommandSequence Sequence
    {
        get;
    }

    public CombatActionId ActionId => ConnectedBasicArrow.ActionId;

    public WorldEntityId ActorEntityId
    {
        get;
    }

    public WorldEntityId TargetEntityId
    {
        get;
    }

    public ulong StartTick
    {
        get;
    }

    public ulong ResolveTick
    {
        get;
    }

    public int RequestedDamageUnits
    {
        get;
    }

    public int EffectiveDamageUnits
    {
        get;
    }

    public bool TargetDefeated
    {
        get;
    }
}

internal static class BasicArrowFactValidation
{
    internal static void ValidateSequence(CombatCommandSequence sequence)
    {
        if (!sequence.IsValid)
            throw new ArgumentException("Combat command sequence must be valid.", nameof(sequence));
    }

    internal static void ValidateEntity(WorldEntityId entityId, string parameterName)
    {
        if (!entityId.IsValid)
            throw new ArgumentException("World entity identity must be valid.", parameterName);
    }

    internal static void ValidateAction(
        CombatCommandSequence sequence,
        WorldEntityId actorEntityId,
        WorldEntityId targetEntityId)
    {
        ValidateSequence(sequence);
        ValidateEntity(actorEntityId, nameof(actorEntityId));
        ValidateEntity(targetEntityId, nameof(targetEntityId));
        if (actorEntityId == targetEntityId)
            throw new ArgumentException("Basic Arrow actor and target identities must differ.", nameof(targetEntityId));
    }

    internal static void ValidateAcceptedTicks(ulong startTick, ulong resolveTick)
    {
        if (resolveTick <= startTick)
            throw new ArgumentOutOfRangeException(nameof(resolveTick), "Resolve tick must be later than start tick.");
    }
}
