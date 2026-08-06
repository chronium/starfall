using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using Starfall.Protocol.Movement;

namespace Starfall.Protocol.Combat;

public enum BasicArrowPayloadKind : byte
{
    Command = 1,
    Accepted = 2,
    Rejected = 3,
    Canceled = 4,
    Resolved = 5,
}

public static class ConnectedBasicArrowCodec
{
    public const int CommandPayloadLength = 29;
    public const int AcceptedPayloadLength = 53;
    public const int RejectedPayloadLength = 46;
    public const int CanceledPayloadLength = 62;
    public const int ResolvedPayloadLength = 62;

    private const int HeaderPayloadLength = 13;
    private const int ActionIdentityLength = 11;
    private const byte TargetNotDefeated = 0;
    private const byte TargetDefeated = 1;

    private static ReadOnlySpan<byte> ActionIdentityBytes => "basic_arrow"u8;

    public static bool TryReadPayloadKind(ReadOnlySpan<byte> payload, out BasicArrowPayloadKind kind)
    {
        kind = default;
        if (payload.Length < HeaderPayloadLength ||
            !Enum.IsDefined((BasicArrowPayloadKind)payload[0]) ||
            payload[1] != ActionIdentityLength ||
            !payload.Slice(2, ActionIdentityLength).SequenceEqual(ActionIdentityBytes))
        {
            return false;
        }

        kind = (BasicArrowPayloadKind)payload[0];
        if (payload.Length != GetPayloadLength(kind))
        {
            kind = default;
            return false;
        }

        return true;
    }

    public static byte[] EncodeCommand(BasicArrowCommand command)
    {
        ValidateCommand(command);

        byte[] payload = CreatePayload(BasicArrowPayloadKind.Command, CommandPayloadLength);
        WriteUInt64(payload, 13, command.Sequence.Value);
        WriteUInt64(payload, 21, command.TargetEntityId.Value);
        return payload;
    }

    public static bool TryDecodeCommand(
        ReadOnlySpan<byte> payload,
        [NotNullWhen(true)] out BasicArrowCommand? command)
    {
        command = null;
        if (!HasKind(payload, BasicArrowPayloadKind.Command))
            return false;

        ulong sequence = ReadUInt64(payload, 13);
        ulong target = ReadUInt64(payload, 21);
        if (sequence == 0 || target == 0)
            return false;

        command = new BasicArrowCommand(
            new CombatCommandSequence(sequence),
            new WorldEntityId(target));
        return true;
    }

    public static byte[] EncodeAccepted(BasicArrowAccepted accepted)
    {
        ValidateAccepted(accepted);

        byte[] payload = CreatePayload(BasicArrowPayloadKind.Accepted, AcceptedPayloadLength);
        WriteAction(payload, accepted.Sequence, accepted.ActorEntityId, accepted.TargetEntityId);
        WriteUInt64(payload, 37, accepted.StartTick);
        WriteUInt64(payload, 45, accepted.ResolveTick);
        return payload;
    }

    public static bool TryDecodeAccepted(
        ReadOnlySpan<byte> payload,
        [NotNullWhen(true)] out BasicArrowAccepted? accepted)
    {
        accepted = null;
        if (!HasKind(payload, BasicArrowPayloadKind.Accepted) ||
            !TryReadAction(payload, out CombatCommandSequence sequence, out WorldEntityId actor, out WorldEntityId target))
        {
            return false;
        }

        ulong startTick = ReadUInt64(payload, 37);
        ulong resolveTick = ReadUInt64(payload, 45);
        if (resolveTick <= startTick)
            return false;

        accepted = new BasicArrowAccepted(sequence, actor, target, startTick, resolveTick);
        return true;
    }

    public static byte[] EncodeRejected(BasicArrowRejected rejected)
    {
        ValidateRejected(rejected);

        byte[] payload = CreatePayload(BasicArrowPayloadKind.Rejected, RejectedPayloadLength);
        WriteAction(payload, rejected.Sequence, rejected.ActorEntityId, rejected.TargetEntityId);
        WriteUInt64(payload, 37, rejected.DecisionTick);
        payload[45] = (byte)rejected.Reason;
        return payload;
    }

    public static bool TryDecodeRejected(
        ReadOnlySpan<byte> payload,
        [NotNullWhen(true)] out BasicArrowRejected? rejected)
    {
        rejected = null;
        if (!HasKind(payload, BasicArrowPayloadKind.Rejected) ||
            !TryReadAction(payload, out CombatCommandSequence sequence, out WorldEntityId actor, out WorldEntityId target) ||
            !Enum.IsDefined((BasicArrowRejectionReason)payload[45]))
        {
            return false;
        }

        rejected = new BasicArrowRejected(
            sequence,
            actor,
            target,
            ReadUInt64(payload, 37),
            (BasicArrowRejectionReason)payload[45]);
        return true;
    }

    public static byte[] EncodeCanceled(BasicArrowCanceled canceled)
    {
        ValidateCanceled(canceled);

        byte[] payload = CreatePayload(BasicArrowPayloadKind.Canceled, CanceledPayloadLength);
        WriteAction(payload, canceled.Sequence, canceled.ActorEntityId, canceled.TargetEntityId);
        WriteUInt64(payload, 37, canceled.StartTick);
        WriteUInt64(payload, 45, canceled.ResolveTick);
        WriteUInt64(payload, 53, canceled.CancellationTick);
        payload[61] = (byte)canceled.Reason;
        return payload;
    }

    public static bool TryDecodeCanceled(
        ReadOnlySpan<byte> payload,
        [NotNullWhen(true)] out BasicArrowCanceled? canceled)
    {
        canceled = null;
        if (!HasKind(payload, BasicArrowPayloadKind.Canceled) ||
            !TryReadAction(payload, out CombatCommandSequence sequence, out WorldEntityId actor, out WorldEntityId target) ||
            !Enum.IsDefined((BasicArrowCancellationReason)payload[61]))
        {
            return false;
        }

        ulong startTick = ReadUInt64(payload, 37);
        ulong resolveTick = ReadUInt64(payload, 45);
        ulong cancellationTick = ReadUInt64(payload, 53);
        if (resolveTick <= startTick || cancellationTick < startTick || cancellationTick > resolveTick)
            return false;

        canceled = new BasicArrowCanceled(
            sequence,
            actor,
            target,
            startTick,
            resolveTick,
            cancellationTick,
            (BasicArrowCancellationReason)payload[61]);
        return true;
    }

    public static byte[] EncodeResolved(BasicArrowResolved resolved)
    {
        ValidateResolved(resolved);

        byte[] payload = CreatePayload(BasicArrowPayloadKind.Resolved, ResolvedPayloadLength);
        WriteAction(payload, resolved.Sequence, resolved.ActorEntityId, resolved.TargetEntityId);
        WriteUInt64(payload, 37, resolved.StartTick);
        WriteUInt64(payload, 45, resolved.ResolveTick);
        WriteInt32(payload, 53, resolved.RequestedDamageUnits);
        WriteInt32(payload, 57, resolved.EffectiveDamageUnits);
        payload[61] = resolved.TargetDefeated ? TargetDefeated : TargetNotDefeated;
        return payload;
    }

    public static bool TryDecodeResolved(
        ReadOnlySpan<byte> payload,
        [NotNullWhen(true)] out BasicArrowResolved? resolved)
    {
        resolved = null;
        if (!HasKind(payload, BasicArrowPayloadKind.Resolved) ||
            !TryReadAction(payload, out CombatCommandSequence sequence, out WorldEntityId actor, out WorldEntityId target))
        {
            return false;
        }

        ulong startTick = ReadUInt64(payload, 37);
        ulong resolveTick = ReadUInt64(payload, 45);
        int requestedDamage = ReadInt32(payload, 53);
        int effectiveDamage = ReadInt32(payload, 57);
        byte defeated = payload[61];
        if (resolveTick <= startTick ||
            requestedDamage != ConnectedBasicArrow.RequestedDamageUnits ||
            effectiveDamage <= 0 ||
            effectiveDamage > requestedDamage ||
            defeated > TargetDefeated)
        {
            return false;
        }

        resolved = new BasicArrowResolved(
            sequence,
            actor,
            target,
            startTick,
            resolveTick,
            requestedDamage,
            effectiveDamage,
            defeated == TargetDefeated);
        return true;
    }

    private static byte[] CreatePayload(BasicArrowPayloadKind kind, int length)
    {
        byte[] payload = new byte[length];
        payload[0] = (byte)kind;
        payload[1] = ActionIdentityLength;
        ActionIdentityBytes.CopyTo(payload.AsSpan(2));
        return payload;
    }

    private static bool HasKind(ReadOnlySpan<byte> payload, BasicArrowPayloadKind expected) =>
        TryReadPayloadKind(payload, out BasicArrowPayloadKind actual) && actual == expected;

    private static int GetPayloadLength(BasicArrowPayloadKind kind) => kind switch
    {
        BasicArrowPayloadKind.Command => CommandPayloadLength,
        BasicArrowPayloadKind.Accepted => AcceptedPayloadLength,
        BasicArrowPayloadKind.Rejected => RejectedPayloadLength,
        BasicArrowPayloadKind.Canceled => CanceledPayloadLength,
        BasicArrowPayloadKind.Resolved => ResolvedPayloadLength,
        _ => 0,
    };

    private static void WriteAction(
        Span<byte> payload,
        CombatCommandSequence sequence,
        WorldEntityId actor,
        WorldEntityId target)
    {
        WriteUInt64(payload, 13, sequence.Value);
        WriteUInt64(payload, 21, actor.Value);
        WriteUInt64(payload, 29, target.Value);
    }

    private static bool TryReadAction(
        ReadOnlySpan<byte> payload,
        out CombatCommandSequence sequence,
        out WorldEntityId actor,
        out WorldEntityId target)
    {
        ulong sequenceValue = ReadUInt64(payload, 13);
        ulong actorValue = ReadUInt64(payload, 21);
        ulong targetValue = ReadUInt64(payload, 29);
        sequence = default;
        actor = default;
        target = default;
        if (sequenceValue == 0 || actorValue == 0 || targetValue == 0 || actorValue == targetValue)
            return false;

        sequence = new CombatCommandSequence(sequenceValue);
        actor = new WorldEntityId(actorValue);
        target = new WorldEntityId(targetValue);
        return true;
    }

    private static void ValidateCommand(BasicArrowCommand? command)
    {
        if (command is null ||
            !command.Sequence.IsValid ||
            command.ActionId != ConnectedBasicArrow.ActionId ||
            !command.TargetEntityId.IsValid)
        {
            throw new ArgumentException("Basic Arrow command must be a complete canonical fact.", nameof(command));
        }
    }

    private static void ValidateAccepted(BasicArrowAccepted? accepted)
    {
        if (!IsValidAction(accepted) || accepted!.ResolveTick <= accepted.StartTick)
            throw new ArgumentException("Accepted Basic Arrow must be a complete canonical fact.", nameof(accepted));
    }

    private static void ValidateRejected(BasicArrowRejected? rejected)
    {
        if (!IsValidAction(rejected) || !Enum.IsDefined(rejected!.Reason))
            throw new ArgumentException("Rejected Basic Arrow must be a complete canonical fact.", nameof(rejected));
    }

    private static void ValidateCanceled(BasicArrowCanceled? canceled)
    {
        if (!IsValidAction(canceled) ||
            canceled!.ResolveTick <= canceled.StartTick ||
            canceled.CancellationTick < canceled.StartTick ||
            canceled.CancellationTick > canceled.ResolveTick ||
            !Enum.IsDefined(canceled.Reason))
        {
            throw new ArgumentException("Canceled Basic Arrow must be a complete canonical fact.", nameof(canceled));
        }
    }

    private static void ValidateResolved(BasicArrowResolved? resolved)
    {
        if (!IsValidAction(resolved) ||
            resolved!.ResolveTick <= resolved.StartTick ||
            resolved.RequestedDamageUnits != ConnectedBasicArrow.RequestedDamageUnits ||
            resolved.EffectiveDamageUnits <= 0 ||
            resolved.EffectiveDamageUnits > resolved.RequestedDamageUnits)
        {
            throw new ArgumentException("Resolved Basic Arrow must be a complete canonical fact.", nameof(resolved));
        }
    }

    private static bool IsValidAction(BasicArrowAccepted? value) =>
        value is not null && IsValidAction(value.Sequence, value.ActionId, value.ActorEntityId, value.TargetEntityId);

    private static bool IsValidAction(BasicArrowRejected? value) =>
        value is not null && IsValidAction(value.Sequence, value.ActionId, value.ActorEntityId, value.TargetEntityId);

    private static bool IsValidAction(BasicArrowCanceled? value) =>
        value is not null && IsValidAction(value.Sequence, value.ActionId, value.ActorEntityId, value.TargetEntityId);

    private static bool IsValidAction(BasicArrowResolved? value) =>
        value is not null && IsValidAction(value.Sequence, value.ActionId, value.ActorEntityId, value.TargetEntityId);

    private static bool IsValidAction(
        CombatCommandSequence sequence,
        CombatActionId action,
        WorldEntityId actor,
        WorldEntityId target) =>
        sequence.IsValid &&
        action == ConnectedBasicArrow.ActionId &&
        actor.IsValid &&
        target.IsValid &&
        actor != target;

    private static void WriteUInt64(Span<byte> destination, int offset, ulong value) =>
        BinaryPrimitives.WriteUInt64BigEndian(destination[offset..], value);

    private static ulong ReadUInt64(ReadOnlySpan<byte> source, int offset) =>
        BinaryPrimitives.ReadUInt64BigEndian(source[offset..]);

    private static void WriteInt32(Span<byte> destination, int offset, int value) =>
        BinaryPrimitives.WriteInt32BigEndian(destination[offset..], value);

    private static int ReadInt32(ReadOnlySpan<byte> source, int offset) =>
        BinaryPrimitives.ReadInt32BigEndian(source[offset..]);
}
