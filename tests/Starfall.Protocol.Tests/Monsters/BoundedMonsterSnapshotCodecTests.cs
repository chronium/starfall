using System.Buffers.Binary;
using System.Numerics;
using Starfall.Protocol.Monsters;
using Starfall.Protocol.Movement;

namespace Starfall.Protocol.Tests.Monsters;

public sealed class BoundedMonsterSnapshotCodecTests
{
    private const string GoldenHex =
        "0102030405060708" +
        "1112131415161718" +
        "01" +
        "01" +
        "2122232425262728" +
        "01" +
        "61" +
        "3f800000" +
        "c0000000" +
        "3f000000" +
        "bf000000" +
        "3f800000" +
        "00000000" +
        "3f400000" +
        "01" +
        "01" +
        "3132333435363738" +
        "0000012c" +
        "000002bc" +
        "4142434445464748" +
        "01" +
        "62" +
        "40400000" +
        "40800000" +
        "00000000" +
        "3f800000" +
        "0102030405060707";

    private static readonly byte[] Golden = Convert.FromHexString(GoldenHex);

    [Fact]
    public void Payload_bounds_are_frozen_without_a_packet_local_version()
    {
        Assert.Equal(18, BoundedMonsterSnapshotCodec.HeaderPayloadLength);
        Assert.Equal(1_208, BoundedMonsterSnapshotCodec.MaxPayloadLength);
        Assert.Equal(108, Golden.Length);
    }

    [Fact]
    public void Mixed_snapshot_matches_golden_bytes_and_round_trips()
    {
        BoundedMonsterSnapshot source = CreateGoldenSnapshot();

        byte[] first = BoundedMonsterSnapshotCodec.Encode(source);
        byte[] second = BoundedMonsterSnapshotCodec.Encode(source);

        Assert.Equal(Golden, first);
        Assert.Equal(first, second);
        Assert.True(BoundedMonsterSnapshotCodec.TryDecode(first, out BoundedMonsterSnapshot? decoded));
        AssertSnapshotEqual(source, decoded);
    }

    [Fact]
    public void Empty_all_live_and_all_defeated_snapshots_round_trip()
    {
        BoundedMonsterSnapshot[] snapshots =
        [
            new(new MonsterSnapshotSequence(1), 0, [], []),
            new(
                new MonsterSnapshotSequence(2),
                20,
                [CreateLive(1, MonsterBehaviorKind.Idle), CreateLive(2, MonsterBehaviorKind.Attacking, 100)],
                []),
            new(
                new MonsterSnapshotSequence(3),
                30,
                [],
                [CreateDefeated(1, 29), CreateDefeated(2, 30)]),
        ];

        foreach (BoundedMonsterSnapshot source in snapshots)
        {
            byte[] encoded = BoundedMonsterSnapshotCodec.Encode(source);
            Assert.True(BoundedMonsterSnapshotCodec.TryDecode(encoded, out BoundedMonsterSnapshot? decoded));
            AssertSnapshotEqual(source, decoded);
        }
    }

    [Fact]
    public void Maximum_payload_is_exact_and_decodes()
    {
        string maximumIdentity = $"a{new string('0', 63)}";
        LiveMonsterSnapshot[] live = Enumerable.Range(1, BoundedMonsterSnapshot.MaxEntries)
            .Select(index => CreateLive((ulong)index, MonsterBehaviorKind.Idle, archetypeId: maximumIdentity))
            .ToArray();
        var source = new BoundedMonsterSnapshot(new MonsterSnapshotSequence(1), 0, live, []);

        byte[] encoded = BoundedMonsterSnapshotCodec.Encode(source);

        Assert.Equal(BoundedMonsterSnapshotCodec.MaxPayloadLength, encoded.Length);
        Assert.True(BoundedMonsterSnapshotCodec.TryDecode(encoded, out _));
    }

    [Fact]
    public void Every_truncated_length_and_representative_trailing_lengths_are_rejected()
    {
        for (int length = 0; length < Golden.Length; length++)
            Assert.False(BoundedMonsterSnapshotCodec.TryDecode(Golden.AsSpan(0, length), out _));

        foreach (int trailingLength in new[] { 1, 2, 17, 128 })
        {
            byte[] extended = new byte[Golden.Length + trailingLength];
            Golden.CopyTo(extended, 0);
            Assert.False(BoundedMonsterSnapshotCodec.TryDecode(extended, out _));
        }
    }

    [Fact]
    public void Invalid_header_identity_order_and_archetype_encoding_are_rejected()
    {
        AssertRejected(payload => Array.Clear(payload, 0, sizeof(ulong)));
        AssertRejected(payload => payload[16] = 11);
        AssertRejected(payload => payload[26] = 0);
        AssertRejected(payload => payload[27] = (byte)'A');

        byte[] unordered = BoundedMonsterSnapshotCodec.Encode(new BoundedMonsterSnapshot(
            new MonsterSnapshotSequence(1),
            0,
            [CreateLive(1, MonsterBehaviorKind.Idle), CreateLive(2, MonsterBehaviorKind.Idle)],
            []));
        BinaryPrimitives.WriteUInt64BigEndian(unordered.AsSpan(18), 3);
        Assert.False(BoundedMonsterSnapshotCodec.TryDecode(unordered, out _));

        AssertRejected(payload => Golden.AsSpan(18, 8).CopyTo(payload.AsSpan(74, 8)));
    }

    [Fact]
    public void Noncanonical_spatial_behavior_target_and_health_values_are_rejected()
    {
        AssertRejected(payload => BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(28), int.MinValue));
        AssertRejected(payload => BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(36), 0x7fc00000));
        AssertRejected(payload =>
        {
            Array.Clear(payload, 44, 8);
        });
        AssertRejected(payload => payload[56] = 4);
        AssertRejected(payload => payload[57] = 2);
        AssertRejected(payload => payload[57] = 0);
        AssertRejected(payload => Array.Clear(payload, 58, 8));
        AssertRejected(payload => Array.Clear(payload, 66, 4));
        AssertRejected(payload => BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(66), 701));
        AssertRejected(payload =>
        {
            BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(8), 1);
            BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(100), 2);
        });
    }

    [Fact]
    public void Encoder_rejects_noncanonical_facts_before_allocating_output()
    {
        Assert.ThrowsAny<ArgumentException>(() => BoundedMonsterSnapshotCodec.Encode(null!));

        var negativeZero = new LiveMonsterSnapshot(
            new WorldEntityId(1),
            new MonsterArchetypeId("starter_flyer_light"),
            new GroundPosition(BitConverter.Int32BitsToSingle(int.MinValue), 1.0f),
            Vector2.Zero,
            Vector2.UnitX,
            0.5f,
            MonsterBehaviorKind.Idle,
            null,
            300,
            700);
        var source = new BoundedMonsterSnapshot(new MonsterSnapshotSequence(1), 0, [negativeZero], []);

        Assert.ThrowsAny<ArgumentException>(() => BoundedMonsterSnapshotCodec.Encode(source));
    }

    [Fact]
    public void Arbitrary_bounded_payloads_never_make_decoder_throw()
    {
        var random = new Random(0x5f3759df);
        foreach (int length in Enumerable.Range(0, 256).Concat([1_207, 1_208, 1_209, 2_048]))
        {
            byte[] payload = new byte[length];
            random.NextBytes(payload);
            Assert.Null(Record.Exception(() => BoundedMonsterSnapshotCodec.TryDecode(payload, out _)));
        }
    }

    private static BoundedMonsterSnapshot CreateGoldenSnapshot() =>
        new(
            new MonsterSnapshotSequence(0x0102030405060708),
            0x1112131415161718,
            [
                new LiveMonsterSnapshot(
                    new WorldEntityId(0x2122232425262728),
                    new MonsterArchetypeId("a"),
                    new GroundPosition(1.0f, -2.0f),
                    new Vector2(0.5f, -0.5f),
                    Vector2.UnitX,
                    0.75f,
                    MonsterBehaviorKind.Pursuing,
                    new WorldEntityId(0x3132333435363738),
                    300,
                    700),
            ],
            [
                new DefeatedMonsterSnapshot(
                    new WorldEntityId(0x4142434445464748),
                    new MonsterArchetypeId("b"),
                    new GroundPosition(3.0f, 4.0f),
                    Vector2.UnitY,
                    0x0102030405060707),
            ]);

    private static LiveMonsterSnapshot CreateLive(
        ulong entityId,
        MonsterBehaviorKind behavior,
        ulong? targetId = null,
        string archetypeId = "starter_flyer_light") =>
        new(
            new WorldEntityId(entityId),
            new MonsterArchetypeId(archetypeId),
            new GroundPosition((float)entityId, (float)entityId + 1.0f),
            Vector2.Zero,
            Vector2.UnitX,
            0.5f,
            behavior,
            targetId is { } value ? new WorldEntityId(value) : null,
            300,
            700);

    private static DefeatedMonsterSnapshot CreateDefeated(ulong entityId, ulong defeatedAtTick) =>
        new(
            new WorldEntityId(entityId),
            new MonsterArchetypeId("starter_flyer_light"),
            new GroundPosition((float)entityId, (float)entityId + 1.0f),
            Vector2.UnitX,
            defeatedAtTick);

    private static void AssertRejected(Action<byte[]> mutation)
    {
        byte[] payload = Golden.ToArray();
        mutation(payload);
        Assert.False(BoundedMonsterSnapshotCodec.TryDecode(payload, out _));
    }

    private static void AssertSnapshotEqual(BoundedMonsterSnapshot expected, BoundedMonsterSnapshot actual)
    {
        Assert.Equal(expected.Sequence, actual.Sequence);
        Assert.Equal(expected.SimulationTick, actual.SimulationTick);
        Assert.Equal(expected.LiveMonsters.Length, actual.LiveMonsters.Length);
        Assert.Equal(expected.DefeatedMonsters.Length, actual.DefeatedMonsters.Length);

        for (int index = 0; index < expected.LiveMonsters.Length; index++)
        {
            LiveMonsterSnapshot left = expected.LiveMonsters[index];
            LiveMonsterSnapshot right = actual.LiveMonsters[index];
            Assert.Equal(left.EntityId, right.EntityId);
            Assert.Equal(left.ArchetypeId, right.ArchetypeId);
            Assert.Equal(left.Position, right.Position);
            Assert.Equal(left.VelocityMetresPerSecond, right.VelocityMetresPerSecond);
            Assert.Equal(left.Facing, right.Facing);
            Assert.Equal(left.CollisionRadiusMetres, right.CollisionRadiusMetres);
            Assert.Equal(left.Behavior, right.Behavior);
            Assert.Equal(left.TargetEntityId, right.TargetEntityId);
            Assert.Equal(left.CurrentHealthUnits, right.CurrentHealthUnits);
            Assert.Equal(left.MaximumHealthUnits, right.MaximumHealthUnits);
        }

        for (int index = 0; index < expected.DefeatedMonsters.Length; index++)
        {
            DefeatedMonsterSnapshot left = expected.DefeatedMonsters[index];
            DefeatedMonsterSnapshot right = actual.DefeatedMonsters[index];
            Assert.Equal(left.EntityId, right.EntityId);
            Assert.Equal(left.ArchetypeId, right.ArchetypeId);
            Assert.Equal(left.LastPosition, right.LastPosition);
            Assert.Equal(left.LastFacing, right.LastFacing);
            Assert.Equal(left.DefeatedAtTick, right.DefeatedAtTick);
        }
    }
}
