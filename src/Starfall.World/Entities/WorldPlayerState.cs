using System.Numerics;
using Starfall.Content.Zones;
using Starfall.Simulation.Entities;
using Starfall.Simulation.Movement;

namespace Starfall.World.Entities;

internal sealed class WorldPlayerState
{
    private const float FacingLengthTolerance = 1e-4f;

    internal WorldPlayerState(
        WorldEntityId entityId,
        GroundPoint position,
        Vector2 velocityMetresPerSecond,
        Vector2 facing,
        PlayerCollisionCapsule collision,
        GroundMovementTickOutcome movementOutcome)
    {
        if (entityId.Value == 0)
            throw new ArgumentException("Player entity identity must be valid.", nameof(entityId));
        if (!IsFinite(velocityMetresPerSecond))
            throw new ArgumentException("Player velocity must be finite.", nameof(velocityMetresPerSecond));
        if (!IsFinite(facing))
            throw new ArgumentException("Player facing must be finite.", nameof(facing));
        if (MathF.Abs(facing.Length() - 1.0f) > FacingLengthTolerance)
            throw new ArgumentException("Player facing must be normalized.", nameof(facing));
        if (collision.RadiusMetres <= 0.0f || collision.HeightMetres <= collision.RadiusMetres * 2.0f)
            throw new ArgumentException("Player collision capsule must be valid.", nameof(collision));
        if (!Enum.IsDefined(movementOutcome))
            throw new ArgumentOutOfRangeException(nameof(movementOutcome));

        EntityId = entityId;
        Position = position;
        VelocityMetresPerSecond = velocityMetresPerSecond;
        Facing = facing;
        Collision = collision;
        MovementOutcome = movementOutcome;
    }

    internal WorldEntityId EntityId
    {
        get;
    }

    internal GroundPoint Position
    {
        get;
    }

    internal Vector2 VelocityMetresPerSecond
    {
        get;
    }

    internal Vector2 Facing
    {
        get;
    }

    internal PlayerCollisionCapsule Collision
    {
        get;
    }

    internal GroundMovementTickOutcome MovementOutcome
    {
        get;
    }

    private static bool IsFinite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);
}
