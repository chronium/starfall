using System.Buffers.Binary;
using System.Numerics;
using Starfall.Protocol.Movement;

namespace Starfall.Protocol.Tests.Movement;

public sealed class ConnectedWalkingCodecTests
{
    private static readonly byte[] CommandGolden = Convert.FromHexString(
        "0101020304050607083FC00000C0100000");

    private static readonly byte[] SnapshotGolden = Convert.FromHexString(
        "01010203040506070800000000000000001112131415161718" +
        "3FC00000C01000004080000000000000000000003F800000" +
        "3F00000040000000012122232425262728");

    private static readonly byte[] CorrectionGolden = Convert.FromHexString(
        "01212223242526272801020304050607080000000000000000" +
        "11121314151617183FC00000C01000004080000000000000" +
        "000000003F8000003F00000040000000012122232425262728");

    [Fact]
    public void Public_schema_and_payload_lengths_are_frozen()
    {
        Assert.Equal(1, ConnectedWalkingCodec.SchemaVersion);
        Assert.Equal(17, ConnectedWalkingCodec.CommandPayloadLength);
        Assert.Equal(66, ConnectedWalkingCodec.SnapshotPayloadLength);
        Assert.Equal(74, ConnectedWalkingCodec.CorrectionPayloadLength);
    }

    [Fact]
    public void Command_encoding_matches_golden_big_endian_bytes_and_round_trips()
    {
        GroundMovementCommand command = CreateCommand();

        byte[] first = ConnectedWalkingCodec.EncodeCommand(command);
        byte[] second = ConnectedWalkingCodec.EncodeCommand(command);

        Assert.Equal(CommandGolden, first);
        Assert.Equal(CommandGolden, second);
        Assert.NotSame(first, second);
        Assert.True(ConnectedWalkingCodec.TryDecodeCommand(first, out GroundMovementCommand? decoded));
        Assert.NotNull(decoded);
        Assert.Equal(command.Sequence, decoded.Sequence);
        Assert.Equal(command.Destination, decoded.Destination);
    }

    [Fact]
    public void Snapshot_encoding_matches_golden_bytes_and_preserves_tick_zero()
    {
        PlayerMovementSnapshot snapshot = CreateSnapshot();

        byte[] first = ConnectedWalkingCodec.EncodeSnapshot(snapshot);
        byte[] second = ConnectedWalkingCodec.EncodeSnapshot(snapshot);

        Assert.Equal(SnapshotGolden, first);
        Assert.Equal(SnapshotGolden, second);
        Assert.NotSame(first, second);
        Assert.True(ConnectedWalkingCodec.TryDecodeSnapshot(first, out PlayerMovementSnapshot? decoded));
        AssertSnapshotEqual(snapshot, Assert.IsType<PlayerMovementSnapshot>(decoded));
        Assert.Equal(0UL, decoded!.SimulationTick);
    }

    [Fact]
    public void Correction_encoding_matches_golden_bytes_without_a_nested_version()
    {
        PlayerMovementSnapshot snapshot = CreateSnapshot();
        var correction = new PlayerMovementCorrection(
            new MovementIntentSequence(0x2122232425262728),
            snapshot);

        byte[] first = ConnectedWalkingCodec.EncodeCorrection(correction);
        byte[] second = ConnectedWalkingCodec.EncodeCorrection(correction);

        Assert.Equal(CorrectionGolden, first);
        Assert.Equal(CorrectionGolden, second);
        Assert.NotSame(first, second);
        Assert.Equal(SnapshotGolden.AsSpan(1).ToArray(), first.AsSpan(9).ToArray());
        Assert.True(ConnectedWalkingCodec.TryDecodeCorrection(first, out PlayerMovementCorrection? decoded));
        Assert.NotNull(decoded);
        Assert.Equal(correction.CorrectedIntentSequence, decoded.CorrectedIntentSequence);
        AssertSnapshotEqual(snapshot, decoded.AuthoritativeSnapshot);
    }

    [Fact]
    public void Snapshot_without_acknowledgement_uses_the_only_canonical_absent_representation()
    {
        PlayerMovementSnapshot snapshot = CreateSnapshot(includeAcknowledgement: false);

        byte[] payload = ConnectedWalkingCodec.EncodeSnapshot(snapshot);

        Assert.Equal(0, payload[57]);
        Assert.Equal(0UL, BinaryPrimitives.ReadUInt64BigEndian(payload.AsSpan(58, 8)));
        Assert.True(ConnectedWalkingCodec.TryDecodeSnapshot(payload, out PlayerMovementSnapshot? decoded));
        Assert.Null(decoded!.LastProcessedIntentSequence);

        payload[65] = 1;
        Assert.False(ConnectedWalkingCodec.TryDecodeSnapshot(payload, out _));
    }

    [Fact]
    public void Every_shorter_or_extended_payload_length_is_rejected()
    {
        AssertExactLengthRejection(
            ConnectedWalkingCodec.EncodeCommand(CreateCommand()),
            payload => ConnectedWalkingCodec.TryDecodeCommand(payload, out _));
        AssertExactLengthRejection(
            ConnectedWalkingCodec.EncodeSnapshot(CreateSnapshot()),
            payload => ConnectedWalkingCodec.TryDecodeSnapshot(payload, out _));
        AssertExactLengthRejection(
            ConnectedWalkingCodec.EncodeCorrection(new PlayerMovementCorrection(
                new MovementIntentSequence(0x2122232425262728),
                CreateSnapshot())),
            payload => ConnectedWalkingCodec.TryDecodeCorrection(payload, out _));
    }

    [Fact]
    public void Unsupported_versions_and_invalid_acknowledgement_flags_are_rejected()
    {
        byte[] command = [.. CommandGolden];
        byte[] snapshot = [.. SnapshotGolden];
        byte[] correction = [.. CorrectionGolden];

        command[0] = 2;
        snapshot[0] = 2;
        correction[0] = 2;
        Assert.False(ConnectedWalkingCodec.TryDecodeCommand(command, out _));
        Assert.False(ConnectedWalkingCodec.TryDecodeSnapshot(snapshot, out _));
        Assert.False(ConnectedWalkingCodec.TryDecodeCorrection(correction, out _));

        snapshot = [.. SnapshotGolden];
        snapshot[57] = 2;
        Assert.False(ConnectedWalkingCodec.TryDecodeSnapshot(snapshot, out _));

        correction = [.. CorrectionGolden];
        correction[65] = 2;
        Assert.False(ConnectedWalkingCodec.TryDecodeCorrection(correction, out _));
    }

    [Fact]
    public void Required_identifiers_and_sequences_reject_zero_while_tick_zero_remains_valid()
    {
        byte[] command = [.. CommandGolden];
        command.AsSpan(1, 8).Clear();
        Assert.False(ConnectedWalkingCodec.TryDecodeCommand(command, out _));

        foreach (int offset in new[] { 1, 17 })
        {
            byte[] snapshot = [.. SnapshotGolden];
            snapshot.AsSpan(offset, 8).Clear();
            Assert.False(ConnectedWalkingCodec.TryDecodeSnapshot(snapshot, out _));
        }

        byte[] presentZero = [.. SnapshotGolden];
        presentZero.AsSpan(58, 8).Clear();
        Assert.False(ConnectedWalkingCodec.TryDecodeSnapshot(presentZero, out _));

        byte[] correction = [.. CorrectionGolden];
        correction.AsSpan(1, 8).Clear();
        Assert.False(ConnectedWalkingCodec.TryDecodeCorrection(correction, out _));

        Assert.True(ConnectedWalkingCodec.TryDecodeSnapshot(SnapshotGolden, out PlayerMovementSnapshot? decoded));
        Assert.Equal(0UL, decoded!.SimulationTick);
    }

    [Fact]
    public void Non_finite_and_negative_zero_float_encodings_are_rejected()
    {
        foreach (int bits in new[]
        {
            unchecked((int)0x7fc00000),
            unchecked((int)0x7f800000),
            unchecked((int)0xff800000),
            int.MinValue,
        })
        {
            byte[] command = [.. CommandGolden];
            BinaryPrimitives.WriteInt32BigEndian(command.AsSpan(9, 4), bits);
            Assert.False(ConnectedWalkingCodec.TryDecodeCommand(command, out _));
        }

        foreach (int offset in new[] { 25, 29, 33, 37, 41, 45, 49, 53 })
        {
            byte[] snapshot = [.. SnapshotGolden];
            BinaryPrimitives.WriteInt32BigEndian(snapshot.AsSpan(offset, 4), int.MinValue);
            Assert.False(ConnectedWalkingCodec.TryDecodeSnapshot(snapshot, out _));
        }
    }

    [Fact]
    public void Facing_and_capsule_validation_reuses_the_fact_contract()
    {
        byte[] zeroFacing = [.. SnapshotGolden];
        zeroFacing.AsSpan(41, 8).Clear();
        Assert.False(ConnectedWalkingCodec.TryDecodeSnapshot(zeroFacing, out _));

        byte[] longFacing = [.. SnapshotGolden];
        BinaryPrimitives.WriteInt32BigEndian(longFacing.AsSpan(41, 4), BitConverter.SingleToInt32Bits(2.0f));
        Assert.False(ConnectedWalkingCodec.TryDecodeSnapshot(longFacing, out _));

        byte[] zeroRadius = [.. SnapshotGolden];
        zeroRadius.AsSpan(49, 4).Clear();
        Assert.False(ConnectedWalkingCodec.TryDecodeSnapshot(zeroRadius, out _));

        byte[] shortCapsule = [.. SnapshotGolden];
        BinaryPrimitives.WriteInt32BigEndian(shortCapsule.AsSpan(53, 4), BitConverter.SingleToInt32Bits(1.0f));
        Assert.False(ConnectedWalkingCodec.TryDecodeSnapshot(shortCapsule, out _));
    }

    [Fact]
    public void Correction_requires_an_acknowledgement_equal_to_the_corrected_sequence()
    {
        byte[] absent = [.. CorrectionGolden];
        absent[65] = 0;
        absent.AsSpan(66, 8).Clear();
        Assert.False(ConnectedWalkingCodec.TryDecodeCorrection(absent, out _));

        byte[] unequal = [.. CorrectionGolden];
        BinaryPrimitives.WriteUInt64BigEndian(unequal.AsSpan(66, 8), 99);
        Assert.False(ConnectedWalkingCodec.TryDecodeCorrection(unequal, out _));
    }

    [Fact]
    public void Encoders_reject_null_or_noncanonical_source_facts_before_exposing_output()
    {
        Assert.ThrowsAny<ArgumentException>(() => ConnectedWalkingCodec.EncodeCommand(null!));
        Assert.ThrowsAny<ArgumentException>(() => ConnectedWalkingCodec.EncodeSnapshot(null!));
        Assert.ThrowsAny<ArgumentException>(() => ConnectedWalkingCodec.EncodeCorrection(null!));

        var command = new GroundMovementCommand(
            new MovementIntentSequence(1),
            new GroundPosition(-0.0f, 1.0f));
        PlayerMovementSnapshot snapshot = CreateSnapshot(facing: new Vector2(-0.0f, 1.0f));

        Assert.ThrowsAny<ArgumentException>(() => ConnectedWalkingCodec.EncodeCommand(command));
        Assert.ThrowsAny<ArgumentException>(() => ConnectedWalkingCodec.EncodeSnapshot(snapshot));
        Assert.ThrowsAny<ArgumentException>(() => ConnectedWalkingCodec.EncodeCorrection(
            new PlayerMovementCorrection(new MovementIntentSequence(0x2122232425262728), snapshot)));
    }

    [Fact]
    public void Try_decode_never_throws_for_arbitrary_untrusted_payloads()
    {
        var random = new Random(73821);
        for (int length = 0; length <= 128; length++)
        {
            byte[] payload = new byte[length];
            random.NextBytes(payload);

            Assert.Null(Record.Exception(() => ConnectedWalkingCodec.TryDecodeCommand(payload, out _)));
            Assert.Null(Record.Exception(() => ConnectedWalkingCodec.TryDecodeSnapshot(payload, out _)));
            Assert.Null(Record.Exception(() => ConnectedWalkingCodec.TryDecodeCorrection(payload, out _)));
        }
    }

    private static GroundMovementCommand CreateCommand() =>
        new(
            new MovementIntentSequence(0x0102030405060708),
            new GroundPosition(1.5f, -2.25f));

    private static PlayerMovementSnapshot CreateSnapshot(
        bool includeAcknowledgement = true,
        Vector2? facing = null)
    {
        MovementIntentSequence? acknowledgement = includeAcknowledgement
            ? new MovementIntentSequence(0x2122232425262728)
            : null;

        return new PlayerMovementSnapshot(
            new MovementSnapshotSequence(0x0102030405060708),
            simulationTick: 0,
            new WorldEntityId(0x1112131415161718),
            new GroundPosition(1.5f, -2.25f),
            new Vector2(4.0f, 0.0f),
            facing ?? Vector2.UnitY,
            new PlayerCollisionCapsule(0.5f, 2.0f),
            acknowledgement);
    }

    private static void AssertSnapshotEqual(
        PlayerMovementSnapshot expected,
        PlayerMovementSnapshot actual)
    {
        Assert.Equal(expected.Sequence, actual.Sequence);
        Assert.Equal(expected.SimulationTick, actual.SimulationTick);
        Assert.Equal(expected.EntityId, actual.EntityId);
        Assert.Equal(expected.Position, actual.Position);
        Assert.Equal(expected.VelocityMetresPerSecond, actual.VelocityMetresPerSecond);
        Assert.Equal(expected.Facing, actual.Facing);
        Assert.Equal(expected.Collision, actual.Collision);
        Assert.Equal(expected.LastProcessedIntentSequence, actual.LastProcessedIntentSequence);
    }

    private static void AssertExactLengthRejection(
        byte[] validPayload,
        Func<byte[], bool> tryDecode)
    {
        for (int length = 0; length < validPayload.Length; length++)
            Assert.False(tryDecode(validPayload[..length]));

        Assert.False(tryDecode([.. validPayload, 0]));
        Assert.False(tryDecode([.. validPayload, 0, 1, 2, 3, 4, 5, 6, 7]));
    }
}
