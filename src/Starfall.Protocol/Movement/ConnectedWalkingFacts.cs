using System.Globalization;
using System.Numerics;

namespace Starfall.Protocol.Movement;

public readonly record struct WorldEntityId
{
    public WorldEntityId(ulong value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "World entity identities must be positive.");

        Value = value;
    }

    public ulong Value
    {
        get;
    }

    internal bool IsValid => Value != 0;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public readonly record struct MovementIntentSequence
{
    public MovementIntentSequence(ulong value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Movement intent sequences must be positive.");

        Value = value;
    }

    public ulong Value
    {
        get;
    }

    internal bool IsValid => Value != 0;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public readonly record struct MovementSnapshotSequence
{
    public MovementSnapshotSequence(ulong value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Movement snapshot sequences must be positive.");

        Value = value;
    }

    public ulong Value
    {
        get;
    }

    internal bool IsValid => Value != 0;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public readonly record struct GroundPosition
{
    public GroundPosition(float xMetres, float zMetres)
        : this(new Vector2(xMetres, zMetres))
    {
    }

    public GroundPosition(Vector2 metres)
    {
        if (!IsFinite(metres))
            throw new ArgumentException("Ground positions must contain finite X/Z metre values.", nameof(metres));

        Metres = metres;
    }

    public Vector2 Metres
    {
        get;
    }

    public float XMetres => Metres.X;

    public float ZMetres => Metres.Y;

    internal bool IsValid => IsFinite(Metres);

    private static bool IsFinite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);
}

public readonly record struct PlayerCollisionCapsule
{
    public PlayerCollisionCapsule(float radiusMetres, float heightMetres)
    {
        if (!float.IsFinite(radiusMetres) || radiusMetres <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(radiusMetres));
        if (!float.IsFinite(heightMetres) || heightMetres <= radiusMetres * 2.0f)
            throw new ArgumentOutOfRangeException(nameof(heightMetres));

        RadiusMetres = radiusMetres;
        HeightMetres = heightMetres;
    }

    public float RadiusMetres
    {
        get;
    }

    public float HeightMetres
    {
        get;
    }

    internal bool IsValid =>
        float.IsFinite(RadiusMetres) &&
        RadiusMetres > 0.0f &&
        float.IsFinite(HeightMetres) &&
        HeightMetres > RadiusMetres * 2.0f;
}

public sealed class GroundMovementCommand
{
    public GroundMovementCommand(
        MovementIntentSequence sequence,
        GroundPosition destination)
    {
        if (!sequence.IsValid)
            throw new ArgumentException("Movement intent sequence must be valid.", nameof(sequence));
        if (!destination.IsValid)
            throw new ArgumentException("Movement destination must be finite.", nameof(destination));

        Sequence = sequence;
        Destination = destination;
    }

    public MovementIntentSequence Sequence
    {
        get;
    }

    public GroundPosition Destination
    {
        get;
    }
}

public sealed class PlayerMovementSnapshot
{
    internal const float FacingLengthTolerance = 1e-4f;

    public PlayerMovementSnapshot(
        MovementSnapshotSequence sequence,
        ulong simulationTick,
        WorldEntityId entityId,
        GroundPosition position,
        Vector2 velocityMetresPerSecond,
        Vector2 facing,
        PlayerCollisionCapsule collision,
        MovementIntentSequence? lastProcessedIntentSequence)
    {
        if (!sequence.IsValid)
            throw new ArgumentException("Movement snapshot sequence must be valid.", nameof(sequence));
        if (!entityId.IsValid)
            throw new ArgumentException("World entity identity must be valid.", nameof(entityId));
        if (!position.IsValid)
            throw new ArgumentException("Player position must be finite.", nameof(position));
        if (!IsFiniteVector(velocityMetresPerSecond))
            throw new ArgumentException("Player velocity must be finite.", nameof(velocityMetresPerSecond));
        if (!IsValidFacing(facing))
            throw new ArgumentException("Player facing must be finite and normalized.", nameof(facing));
        if (!collision.IsValid)
            throw new ArgumentException("Player collision capsule must be valid.", nameof(collision));
        if (lastProcessedIntentSequence is { } processedSequence && !processedSequence.IsValid)
        {
            throw new ArgumentException(
                "Last processed movement intent sequence must be valid when present.",
                nameof(lastProcessedIntentSequence));
        }

        Sequence = sequence;
        SimulationTick = simulationTick;
        EntityId = entityId;
        Position = position;
        VelocityMetresPerSecond = velocityMetresPerSecond;
        Facing = facing;
        Collision = collision;
        LastProcessedIntentSequence = lastProcessedIntentSequence;
    }

    public MovementSnapshotSequence Sequence
    {
        get;
    }

    public ulong SimulationTick
    {
        get;
    }

    public WorldEntityId EntityId
    {
        get;
    }

    public GroundPosition Position
    {
        get;
    }

    public Vector2 VelocityMetresPerSecond
    {
        get;
    }

    public Vector2 Facing
    {
        get;
    }

    public PlayerCollisionCapsule Collision
    {
        get;
    }

    public MovementIntentSequence? LastProcessedIntentSequence
    {
        get;
    }

    internal static bool IsFiniteVector(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);

    internal static bool IsValidFacing(Vector2 value) =>
        IsFiniteVector(value) && MathF.Abs(value.Length() - 1.0f) <= FacingLengthTolerance;
}

public sealed class PlayerMovementCorrection
{
    public PlayerMovementCorrection(
        MovementIntentSequence correctedIntentSequence,
        PlayerMovementSnapshot authoritativeSnapshot)
    {
        if (!correctedIntentSequence.IsValid)
            throw new ArgumentException("Corrected movement intent sequence must be valid.", nameof(correctedIntentSequence));
        ArgumentNullException.ThrowIfNull(authoritativeSnapshot);
        if (authoritativeSnapshot.LastProcessedIntentSequence != correctedIntentSequence)
        {
            throw new ArgumentException(
                "The authoritative snapshot must acknowledge the corrected movement intent.",
                nameof(authoritativeSnapshot));
        }

        CorrectedIntentSequence = correctedIntentSequence;
        AuthoritativeSnapshot = authoritativeSnapshot;
    }

    public MovementIntentSequence CorrectedIntentSequence
    {
        get;
    }

    public PlayerMovementSnapshot AuthoritativeSnapshot
    {
        get;
    }
}
