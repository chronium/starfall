using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Starfall.Protocol.Combat;
using Starfall.Protocol.Movement;

namespace Starfall.Protocol.Tests.Combat;

public sealed class ConnectedBasicArrowCodecTests
{
    private static readonly byte[] CommandGolden = Convert.FromHexString(
        "010B62617369635F6172726F77" +
        "0102030405060708" +
        "1112131415161718");

    private static readonly byte[] AcceptedGolden = Convert.FromHexString(
        "020B62617369635F6172726F77" +
        "0102030405060708" +
        "1112131415161718" +
        "2122232425262728" +
        "0000000000000000" +
        "0000000000000016");

    private static readonly byte[] RejectedGolden = Convert.FromHexString(
        "030B62617369635F6172726F77" +
        "0102030405060708" +
        "1112131415161718" +
        "2122232425262728" +
        "0000000000000000" +
        "07");

    private static readonly byte[] CanceledGolden = Convert.FromHexString(
        "040B62617369635F6172726F77" +
        "0102030405060708" +
        "1112131415161718" +
        "2122232425262728" +
        "000000000000000A" +
        "0000000000000016" +
        "000000000000000F" +
        "00");

    private static readonly byte[] ResolvedGolden = Convert.FromHexString(
        "050B62617369635F6172726F77" +
        "0102030405060708" +
        "1112131415161718" +
        "2122232425262728" +
        "000000000000000A" +
        "0000000000000016" +
        "0000012C" +
        "0000012C" +
        "01");

    [Fact]
    public void Public_kinds_and_payload_lengths_are_frozen_without_a_packet_local_version()
    {
        Assert.Equal(1, (byte)BasicArrowPayloadKind.Command);
        Assert.Equal(2, (byte)BasicArrowPayloadKind.Accepted);
        Assert.Equal(3, (byte)BasicArrowPayloadKind.Rejected);
        Assert.Equal(4, (byte)BasicArrowPayloadKind.Canceled);
        Assert.Equal(5, (byte)BasicArrowPayloadKind.Resolved);
        Assert.Equal(29, ConnectedBasicArrowCodec.CommandPayloadLength);
        Assert.Equal(53, ConnectedBasicArrowCodec.AcceptedPayloadLength);
        Assert.Equal(46, ConnectedBasicArrowCodec.RejectedPayloadLength);
        Assert.Equal(62, ConnectedBasicArrowCodec.CanceledPayloadLength);
        Assert.Equal(62, ConnectedBasicArrowCodec.ResolvedPayloadLength);
    }

    [Fact]
    public void Command_encoding_matches_golden_bytes_is_deterministic_and_round_trips()
    {
        BasicArrowCommand command = CreateCommand();

        byte[] first = ConnectedBasicArrowCodec.EncodeCommand(command);
        byte[] second = ConnectedBasicArrowCodec.EncodeCommand(command);

        Assert.Equal(CommandGolden, first);
        Assert.Equal(CommandGolden, second);
        Assert.NotSame(first, second);
        Assert.True(ConnectedBasicArrowCodec.TryReadPayloadKind(first, out BasicArrowPayloadKind kind));
        Assert.Equal(BasicArrowPayloadKind.Command, kind);
        Assert.True(ConnectedBasicArrowCodec.TryDecodeCommand(first, out BasicArrowCommand? decoded));
        Assert.Equal(command.Sequence, decoded!.Sequence);
        Assert.Equal(command.ActionId, decoded.ActionId);
        Assert.Equal(command.TargetEntityId, decoded.TargetEntityId);
    }

    [Fact]
    public void Accepted_encoding_matches_golden_bytes_and_preserves_tick_zero()
    {
        BasicArrowAccepted accepted = CreateAccepted();

        byte[] first = ConnectedBasicArrowCodec.EncodeAccepted(accepted);
        byte[] second = ConnectedBasicArrowCodec.EncodeAccepted(accepted);

        Assert.Equal(AcceptedGolden, first);
        Assert.Equal(AcceptedGolden, second);
        Assert.NotSame(first, second);
        Assert.True(ConnectedBasicArrowCodec.TryDecodeAccepted(first, out BasicArrowAccepted? decoded));
        AssertActionEqual(accepted, decoded!);
        Assert.Equal(0UL, decoded!.StartTick);
        Assert.Equal(accepted.ResolveTick, decoded.ResolveTick);
    }

    [Fact]
    public void Rejected_encoding_matches_golden_bytes_and_round_trips()
    {
        BasicArrowRejected rejected = CreateRejected(BasicArrowRejectionReason.TargetOutOfRange);

        byte[] first = ConnectedBasicArrowCodec.EncodeRejected(rejected);
        byte[] second = ConnectedBasicArrowCodec.EncodeRejected(rejected);

        Assert.Equal(RejectedGolden, first);
        Assert.Equal(RejectedGolden, second);
        Assert.NotSame(first, second);
        Assert.True(ConnectedBasicArrowCodec.TryDecodeRejected(first, out BasicArrowRejected? decoded));
        AssertActionEqual(rejected, decoded!);
        Assert.Equal(0UL, decoded!.DecisionTick);
        Assert.Equal(rejected.Reason, decoded.Reason);
    }

    [Fact]
    public void Canceled_encoding_matches_golden_bytes_and_round_trips()
    {
        BasicArrowCanceled canceled = CreateCanceled(BasicArrowCancellationReason.CanceledByMovement);

        byte[] first = ConnectedBasicArrowCodec.EncodeCanceled(canceled);
        byte[] second = ConnectedBasicArrowCodec.EncodeCanceled(canceled);

        Assert.Equal(CanceledGolden, first);
        Assert.Equal(CanceledGolden, second);
        Assert.NotSame(first, second);
        Assert.True(ConnectedBasicArrowCodec.TryDecodeCanceled(first, out BasicArrowCanceled? decoded));
        AssertActionEqual(canceled, decoded!);
        Assert.Equal(canceled.StartTick, decoded!.StartTick);
        Assert.Equal(canceled.ResolveTick, decoded.ResolveTick);
        Assert.Equal(canceled.CancellationTick, decoded.CancellationTick);
        Assert.Equal(canceled.Reason, decoded.Reason);
    }

    [Fact]
    public void Resolved_encoding_matches_golden_bytes_and_round_trips_defeat()
    {
        BasicArrowResolved resolved = CreateResolved();

        byte[] first = ConnectedBasicArrowCodec.EncodeResolved(resolved);
        byte[] second = ConnectedBasicArrowCodec.EncodeResolved(resolved);

        Assert.Equal(ResolvedGolden, first);
        Assert.Equal(ResolvedGolden, second);
        Assert.NotSame(first, second);
        Assert.True(ConnectedBasicArrowCodec.TryDecodeResolved(first, out BasicArrowResolved? decoded));
        AssertActionEqual(resolved, decoded!);
        Assert.Equal(resolved.StartTick, decoded!.StartTick);
        Assert.Equal(resolved.ResolveTick, decoded.ResolveTick);
        Assert.Equal(300, decoded.RequestedDamageUnits);
        Assert.Equal(300, decoded.EffectiveDamageUnits);
        Assert.True(decoded.TargetDefeated);
    }

    [Fact]
    public void Every_rejection_and_cancellation_reason_round_trips()
    {
        foreach (BasicArrowRejectionReason reason in Enum.GetValues<BasicArrowRejectionReason>())
        {
            byte[] payload = ConnectedBasicArrowCodec.EncodeRejected(CreateRejected(reason));
            Assert.True(ConnectedBasicArrowCodec.TryDecodeRejected(payload, out BasicArrowRejected? decoded));
            Assert.Equal(reason, decoded!.Reason);
        }

        foreach (BasicArrowCancellationReason reason in Enum.GetValues<BasicArrowCancellationReason>())
        {
            byte[] payload = ConnectedBasicArrowCodec.EncodeCanceled(CreateCanceled(reason));
            Assert.True(ConnectedBasicArrowCodec.TryDecodeCanceled(payload, out BasicArrowCanceled? decoded));
            Assert.Equal(reason, decoded!.Reason);
        }
    }

    [Fact]
    public void Every_shorter_and_representative_extended_payload_length_is_rejected()
    {
        AssertExactLengthRejection(CommandGolden, payload => ConnectedBasicArrowCodec.TryDecodeCommand(payload, out _));
        AssertExactLengthRejection(AcceptedGolden, payload => ConnectedBasicArrowCodec.TryDecodeAccepted(payload, out _));
        AssertExactLengthRejection(RejectedGolden, payload => ConnectedBasicArrowCodec.TryDecodeRejected(payload, out _));
        AssertExactLengthRejection(CanceledGolden, payload => ConnectedBasicArrowCodec.TryDecodeCanceled(payload, out _));
        AssertExactLengthRejection(ResolvedGolden, payload => ConnectedBasicArrowCodec.TryDecodeResolved(payload, out _));
    }

    [Fact]
    public void Unsupported_or_noncanonical_headers_are_rejected()
    {
        AssertHeaderRejected(payload => payload[0] = 0);
        AssertHeaderRejected(payload => payload[0] = 6);
        AssertHeaderRejected(payload => payload[1] = 10);
        AssertHeaderRejected(payload => payload[1] = 12);
        AssertHeaderRejected(payload => payload[2] = (byte)'B');
        AssertHeaderRejected(payload => payload[12] = (byte)'x');

        byte[] wrongKnownKind = [.. CommandGolden];
        wrongKnownKind[0] = (byte)BasicArrowPayloadKind.Accepted;
        Assert.False(ConnectedBasicArrowCodec.TryReadPayloadKind(wrongKnownKind, out _));
        Assert.False(ConnectedBasicArrowCodec.TryDecodeCommand(wrongKnownKind, out _));
    }

    [Fact]
    public void Required_sequences_and_entity_identities_reject_zero_or_self_targeting()
    {
        foreach (int offset in new[] { 13, 21 })
        {
            byte[] command = [.. CommandGolden];
            command.AsSpan(offset, 8).Clear();
            Assert.False(ConnectedBasicArrowCodec.TryDecodeCommand(command, out _));
        }

        foreach (int offset in new[] { 13, 21, 29 })
        {
            byte[] accepted = [.. AcceptedGolden];
            accepted.AsSpan(offset, 8).Clear();
            Assert.False(ConnectedBasicArrowCodec.TryDecodeAccepted(accepted, out _));
        }

        byte[] selfTarget = [.. AcceptedGolden];
        selfTarget.AsSpan(21, 8).CopyTo(selfTarget.AsSpan(29, 8));
        Assert.False(ConnectedBasicArrowCodec.TryDecodeAccepted(selfTarget, out _));
    }

    [Fact]
    public void Temporal_reason_damage_and_flag_constraints_are_rejected()
    {
        byte[] accepted = [.. AcceptedGolden];
        accepted.AsSpan(45, 8).Clear();
        Assert.False(ConnectedBasicArrowCodec.TryDecodeAccepted(accepted, out _));

        byte[] rejected = [.. RejectedGolden];
        rejected[45] = 255;
        Assert.False(ConnectedBasicArrowCodec.TryDecodeRejected(rejected, out _));

        byte[] cancellationBeforeStart = [.. CanceledGolden];
        BinaryPrimitives.WriteUInt64BigEndian(cancellationBeforeStart.AsSpan(53, 8), 9);
        Assert.False(ConnectedBasicArrowCodec.TryDecodeCanceled(cancellationBeforeStart, out _));

        byte[] cancellationAfterResolve = [.. CanceledGolden];
        BinaryPrimitives.WriteUInt64BigEndian(cancellationAfterResolve.AsSpan(53, 8), 23);
        Assert.False(ConnectedBasicArrowCodec.TryDecodeCanceled(cancellationAfterResolve, out _));

        byte[] invalidCancellationReason = [.. CanceledGolden];
        invalidCancellationReason[61] = 255;
        Assert.False(ConnectedBasicArrowCodec.TryDecodeCanceled(invalidCancellationReason, out _));

        AssertResolvedRejected(payload => BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(53, 4), 299));
        AssertResolvedRejected(payload => BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(57, 4), 0));
        AssertResolvedRejected(payload => BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(57, 4), 301));
        AssertResolvedRejected(payload => payload[61] = 2);
    }

    [Fact]
    public void Cancellation_boundaries_and_nondefeating_resolution_are_canonical()
    {
        foreach (ulong cancellationTick in new[] { 10UL, 22UL })
        {
            BasicArrowCanceled canceled = new(
                Sequence,
                Actor,
                Target,
                10,
                22,
                cancellationTick,
                BasicArrowCancellationReason.TargetUnavailable);
            byte[] payload = ConnectedBasicArrowCodec.EncodeCanceled(canceled);
            Assert.True(ConnectedBasicArrowCodec.TryDecodeCanceled(payload, out BasicArrowCanceled? decoded));
            Assert.Equal(cancellationTick, decoded!.CancellationTick);
        }

        BasicArrowResolved resolved = new(Sequence, Actor, Target, 10, 22, 300, 1, false);
        byte[] resolvedPayload = ConnectedBasicArrowCodec.EncodeResolved(resolved);
        Assert.True(ConnectedBasicArrowCodec.TryDecodeResolved(resolvedPayload, out BasicArrowResolved? decodedResolved));
        Assert.Equal(1, decodedResolved!.EffectiveDamageUnits);
        Assert.False(decodedResolved.TargetDefeated);
    }

    [Fact]
    public void Encoders_reject_null_or_uninitialized_source_facts_before_exposing_output()
    {
        Assert.ThrowsAny<ArgumentException>(() => ConnectedBasicArrowCodec.EncodeCommand(null!));
        Assert.ThrowsAny<ArgumentException>(() => ConnectedBasicArrowCodec.EncodeAccepted(null!));
        Assert.ThrowsAny<ArgumentException>(() => ConnectedBasicArrowCodec.EncodeRejected(null!));
        Assert.ThrowsAny<ArgumentException>(() => ConnectedBasicArrowCodec.EncodeCanceled(null!));
        Assert.ThrowsAny<ArgumentException>(() => ConnectedBasicArrowCodec.EncodeResolved(null!));

        Assert.ThrowsAny<ArgumentException>(() => ConnectedBasicArrowCodec.EncodeCommand(Uninitialized<BasicArrowCommand>()));
        Assert.ThrowsAny<ArgumentException>(() => ConnectedBasicArrowCodec.EncodeAccepted(Uninitialized<BasicArrowAccepted>()));
        Assert.ThrowsAny<ArgumentException>(() => ConnectedBasicArrowCodec.EncodeRejected(Uninitialized<BasicArrowRejected>()));
        Assert.ThrowsAny<ArgumentException>(() => ConnectedBasicArrowCodec.EncodeCanceled(Uninitialized<BasicArrowCanceled>()));
        Assert.ThrowsAny<ArgumentException>(() => ConnectedBasicArrowCodec.EncodeResolved(Uninitialized<BasicArrowResolved>()));
    }

    [Fact]
    public void Try_decode_never_throws_for_arbitrary_untrusted_payloads()
    {
        var random = new Random(73822);
        for (int length = 0; length <= 128; length++)
        {
            byte[] payload = new byte[length];
            random.NextBytes(payload);

            Assert.Null(Record.Exception(() => ConnectedBasicArrowCodec.TryReadPayloadKind(payload, out _)));
            Assert.Null(Record.Exception(() => ConnectedBasicArrowCodec.TryDecodeCommand(payload, out _)));
            Assert.Null(Record.Exception(() => ConnectedBasicArrowCodec.TryDecodeAccepted(payload, out _)));
            Assert.Null(Record.Exception(() => ConnectedBasicArrowCodec.TryDecodeRejected(payload, out _)));
            Assert.Null(Record.Exception(() => ConnectedBasicArrowCodec.TryDecodeCanceled(payload, out _)));
            Assert.Null(Record.Exception(() => ConnectedBasicArrowCodec.TryDecodeResolved(payload, out _)));
        }
    }

    private static CombatCommandSequence Sequence => new(0x0102030405060708);

    private static WorldEntityId Actor => new(0x1112131415161718);

    private static WorldEntityId Target => new(0x2122232425262728);

    private static BasicArrowCommand CreateCommand() => new(Sequence, Actor);

    private static BasicArrowAccepted CreateAccepted() => new(Sequence, Actor, Target, 0, 22);

    private static BasicArrowRejected CreateRejected(BasicArrowRejectionReason reason) =>
        new(Sequence, Actor, Target, 0, reason);

    private static BasicArrowCanceled CreateCanceled(BasicArrowCancellationReason reason) =>
        new(Sequence, Actor, Target, 10, 22, 15, reason);

    private static BasicArrowResolved CreateResolved() =>
        new(Sequence, Actor, Target, 10, 22, 300, 300, true);

    private static void AssertActionEqual(BasicArrowAccepted expected, BasicArrowAccepted actual)
    {
        Assert.Equal(expected.Sequence, actual.Sequence);
        Assert.Equal(expected.ActionId, actual.ActionId);
        Assert.Equal(expected.ActorEntityId, actual.ActorEntityId);
        Assert.Equal(expected.TargetEntityId, actual.TargetEntityId);
    }

    private static void AssertActionEqual(BasicArrowRejected expected, BasicArrowRejected actual)
    {
        Assert.Equal(expected.Sequence, actual.Sequence);
        Assert.Equal(expected.ActionId, actual.ActionId);
        Assert.Equal(expected.ActorEntityId, actual.ActorEntityId);
        Assert.Equal(expected.TargetEntityId, actual.TargetEntityId);
    }

    private static void AssertActionEqual(BasicArrowCanceled expected, BasicArrowCanceled actual)
    {
        Assert.Equal(expected.Sequence, actual.Sequence);
        Assert.Equal(expected.ActionId, actual.ActionId);
        Assert.Equal(expected.ActorEntityId, actual.ActorEntityId);
        Assert.Equal(expected.TargetEntityId, actual.TargetEntityId);
    }

    private static void AssertActionEqual(BasicArrowResolved expected, BasicArrowResolved actual)
    {
        Assert.Equal(expected.Sequence, actual.Sequence);
        Assert.Equal(expected.ActionId, actual.ActionId);
        Assert.Equal(expected.ActorEntityId, actual.ActorEntityId);
        Assert.Equal(expected.TargetEntityId, actual.TargetEntityId);
    }

    private static void AssertExactLengthRejection(byte[] canonical, Func<byte[], bool> decoder)
    {
        for (int length = 0; length < canonical.Length; length++)
            Assert.False(decoder(canonical.AsSpan(0, length).ToArray()));

        foreach (int extension in new[] { 1, 8, 64 })
        {
            byte[] extended = new byte[canonical.Length + extension];
            canonical.CopyTo(extended, 0);
            Assert.False(decoder(extended));
        }
    }

    private static void AssertHeaderRejected(Action<byte[]> mutation)
    {
        byte[] payload = [.. CommandGolden];
        mutation(payload);
        Assert.False(ConnectedBasicArrowCodec.TryReadPayloadKind(payload, out _));
        Assert.False(ConnectedBasicArrowCodec.TryDecodeCommand(payload, out _));
    }

    private static void AssertResolvedRejected(Action<byte[]> mutation)
    {
        byte[] payload = [.. ResolvedGolden];
        mutation(payload);
        Assert.False(ConnectedBasicArrowCodec.TryDecodeResolved(payload, out _));
    }

    private static T Uninitialized<T>() where T : class =>
        (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
}
