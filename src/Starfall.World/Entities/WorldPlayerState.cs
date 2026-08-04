using System.Numerics;
using Starfall.Content.Zones;
using Starfall.Simulation.Entities;

namespace Starfall.World.Entities;

internal sealed class WorldPlayerState
{
    private const float FacingLengthTolerance = 1e-4f;

    internal WorldPlayerState(
        WorldEntityId entityId,
        GroundPoint position,
        Vector2 velocityMetresPerSecond,
        Vector2 facing)
    {
        if (entityId.Value == 0)
            throw new ArgumentException("Player entity identity must be valid.", nameof(entityId));
        if (!IsFinite(velocityMetresPerSecond))
            throw new ArgumentException("Player velocity must be finite.", nameof(velocityMetresPerSecond));
        if (!IsFinite(facing))
            throw new ArgumentException("Player facing must be finite.", nameof(facing));
        if (MathF.Abs(facing.Length() - 1.0f) > FacingLengthTolerance)
            throw new ArgumentException("Player facing must be normalized.", nameof(facing));

        EntityId = entityId;
        Position = position;
        VelocityMetresPerSecond = velocityMetresPerSecond;
        Facing = facing;
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

    private static bool IsFinite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);
}
