using System.Numerics;
using Starfall.Content.Zones;
using Starfall.Simulation.Entities;
using Starfall.Simulation.Movement;
using Starfall.Simulation.Players;

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
        GroundMovementTickOutcome movementOutcome,
        int healthUnits,
        Draft0PlayerLifeStatus lifeStatus,
        ulong? respawnAtTick)
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
        if (!Enum.IsDefined(lifeStatus))
            throw new ArgumentOutOfRangeException(nameof(lifeStatus));
        if (lifeStatus == Draft0PlayerLifeStatus.Active && (healthUnits <= 0 || respawnAtTick is not null))
            throw new ArgumentException("An active player requires positive health and no respawn tick.", nameof(healthUnits));
        if (lifeStatus == Draft0PlayerLifeStatus.Defeated && (healthUnits != 0 || respawnAtTick is null))
            throw new ArgumentException("A defeated player requires zero health and a respawn tick.", nameof(healthUnits));

        EntityId = entityId;
        Position = position;
        VelocityMetresPerSecond = velocityMetresPerSecond;
        Facing = facing;
        Collision = collision;
        MovementOutcome = movementOutcome;
        HealthUnits = healthUnits;
        LifeStatus = lifeStatus;
        RespawnAtTick = respawnAtTick;
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

    internal int HealthUnits
    {
        get;
    }

    internal Draft0PlayerLifeStatus LifeStatus
    {
        get;
    }

    internal ulong? RespawnAtTick
    {
        get;
    }

    internal bool IsActive => LifeStatus == Draft0PlayerLifeStatus.Active;

    private static bool IsFinite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);
}
