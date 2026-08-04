using System.Numerics;
using Starfall.Protocol.Movement;

namespace Starfall.Protocol.Tests.Movement;

public sealed class ConnectedWalkingContractTests
{
    [Fact]
    public void Opaque_identifiers_and_sequences_are_positive_and_invariant()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorldEntityId(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MovementIntentSequence(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MovementSnapshotSequence(0));

        Assert.Equal("42", new WorldEntityId(42).ToString());
        Assert.Equal("7", new MovementIntentSequence(7).ToString());
        Assert.Equal("9", new MovementSnapshotSequence(9).ToString());
    }

    [Fact]
    public void Ground_positions_preserve_finite_xz_metres_without_zone_policy()
    {
        var position = new GroundPosition(-12.5f, 2048.25f);

        Assert.Equal(new Vector2(-12.5f, 2048.25f), position.Metres);
        Assert.Equal(-12.5f, position.XMetres);
        Assert.Equal(2048.25f, position.ZMetres);
        Assert.Throws<ArgumentException>(() => new GroundPosition(float.NaN, 0.0f));
        Assert.Throws<ArgumentException>(() => new GroundPosition(0.0f, float.PositiveInfinity));
    }

    [Fact]
    public void Collision_capsules_require_finite_positive_capsule_dimensions()
    {
        var capsule = new PlayerCollisionCapsule(0.35f, 1.8f);

        Assert.Equal(0.35f, capsule.RadiusMetres);
        Assert.Equal(1.8f, capsule.HeightMetres);
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerCollisionCapsule(0.0f, 1.8f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerCollisionCapsule(float.NaN, 1.8f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerCollisionCapsule(0.35f, 0.7f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerCollisionCapsule(0.35f, float.PositiveInfinity));
    }

    [Fact]
    public void Movement_commands_are_session_bound_sequence_and_destination_facts()
    {
        var command = new GroundMovementCommand(
            new MovementIntentSequence(3),
            new GroundPosition(100.0f, 25.0f));

        Assert.Equal(new MovementIntentSequence(3), command.Sequence);
        Assert.Equal(new GroundPosition(100.0f, 25.0f), command.Destination);
        Assert.Throws<ArgumentException>(() => new GroundMovementCommand(
            default,
            new GroundPosition(100.0f, 25.0f)));
    }

    [Fact]
    public void Initial_snapshot_accepts_tick_zero_without_processed_intent()
    {
        PlayerMovementSnapshot snapshot = CreateSnapshot(
            sequence: 1,
            simulationTick: 0,
            lastProcessedIntentSequence: null);

        Assert.Equal(new MovementSnapshotSequence(1), snapshot.Sequence);
        Assert.Equal(0UL, snapshot.SimulationTick);
        Assert.Equal(new WorldEntityId(12), snapshot.EntityId);
        Assert.Equal(new GroundPosition(100.0f, 25.0f), snapshot.Position);
        Assert.Equal(Vector2.Zero, snapshot.VelocityMetresPerSecond);
        Assert.Equal(Vector2.UnitY, snapshot.Facing);
        Assert.Equal(new PlayerCollisionCapsule(0.35f, 1.8f), snapshot.Collision);
        Assert.Null(snapshot.LastProcessedIntentSequence);
    }

    [Fact]
    public void Snapshot_preserves_fixed_tick_and_latest_processed_intent()
    {
        PlayerMovementSnapshot snapshot = CreateSnapshot(
            sequence: 17,
            simulationTick: 60,
            lastProcessedIntentSequence: new MovementIntentSequence(5),
            velocity: new Vector2(4.0f, 0.0f),
            facing: Vector2.UnitX);

        Assert.Equal(new MovementSnapshotSequence(17), snapshot.Sequence);
        Assert.Equal(60UL, snapshot.SimulationTick);
        Assert.Equal(new MovementIntentSequence(5), snapshot.LastProcessedIntentSequence);
        Assert.Equal(new Vector2(4.0f, 0.0f), snapshot.VelocityMetresPerSecond);
        Assert.Equal(Vector2.UnitX, snapshot.Facing);
    }

    [Fact]
    public void Snapshots_reject_default_or_malformed_state()
    {
        Assert.Throws<ArgumentException>(() => CreateSnapshot(sequence: 0));
        Assert.Throws<ArgumentException>(() => CreateSnapshot(entityId: 0));
        Assert.Throws<ArgumentException>(() => CreateSnapshot(velocity: new Vector2(float.NaN, 0.0f)));
        Assert.Throws<ArgumentException>(() => CreateSnapshot(facing: Vector2.Zero));
        Assert.Throws<ArgumentException>(() => CreateSnapshot(facing: new Vector2(2.0f, 0.0f)));
        Assert.Throws<ArgumentException>(() => new PlayerMovementSnapshot(
            new MovementSnapshotSequence(1),
            1,
            new WorldEntityId(12),
            new GroundPosition(100.0f, 25.0f),
            Vector2.Zero,
            Vector2.UnitY,
            default,
            null));
        Assert.Throws<ArgumentException>(() => CreateSnapshot(lastProcessedIntentSequence: default(MovementIntentSequence)));
    }

    [Fact]
    public void Correction_requires_a_snapshot_acknowledging_the_corrected_intent()
    {
        MovementIntentSequence corrected = new(8);
        PlayerMovementSnapshot snapshot = CreateSnapshot(
            sequence: 21,
            simulationTick: 120,
            lastProcessedIntentSequence: corrected);
        var correction = new PlayerMovementCorrection(corrected, snapshot);

        Assert.Equal(corrected, correction.CorrectedIntentSequence);
        Assert.Same(snapshot, correction.AuthoritativeSnapshot);
        Assert.Throws<ArgumentException>(() => new PlayerMovementCorrection(default, snapshot));
        Assert.Throws<ArgumentNullException>(() => new PlayerMovementCorrection(corrected, null!));
        Assert.Throws<ArgumentException>(() => new PlayerMovementCorrection(
            new MovementIntentSequence(9),
            snapshot));
        Assert.Throws<ArgumentException>(() => new PlayerMovementCorrection(
            corrected,
            CreateSnapshot(sequence: 22, lastProcessedIntentSequence: null)));
    }

    private static PlayerMovementSnapshot CreateSnapshot(
        ulong sequence = 1,
        ulong simulationTick = 1,
        ulong entityId = 12,
        Vector2? velocity = null,
        Vector2? facing = null,
        PlayerCollisionCapsule? collision = null,
        MovementIntentSequence? lastProcessedIntentSequence = null) =>
        new(
            sequence == 0 ? default : new MovementSnapshotSequence(sequence),
            simulationTick,
            entityId == 0 ? default : new WorldEntityId(entityId),
            new GroundPosition(100.0f, 25.0f),
            velocity ?? Vector2.Zero,
            facing ?? Vector2.UnitY,
            collision ?? new PlayerCollisionCapsule(0.35f, 1.8f),
            lastProcessedIntentSequence);
}
