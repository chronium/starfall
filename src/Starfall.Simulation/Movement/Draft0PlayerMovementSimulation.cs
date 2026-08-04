using System.Collections.ObjectModel;
using System.Numerics;
using ChronoFall.Box3D.Bodies;
using ChronoFall.Box3D.Geometry;
using ChronoFall.Box3D.Worlds;
using Starfall.Content.Zones;
using Starfall.Simulation.Entities;

namespace Starfall.Simulation.Movement;

public enum GroundMovementIntentDisposition
{
    Accepted,
    UnknownPlayer,
    OutsideWalkableBounds,
    ObstructedDestination,
}

public enum GroundMovementTickOutcome
{
    Idle,
    Moving,
    Arrived,
    Blocked,
}

public readonly record struct GroundMovementIntent(
    WorldEntityId EntityId,
    GroundPoint Destination);

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
}

public readonly record struct AuthoritativePlayerMovementState
{
    private const float FacingLengthTolerance = 1e-4f;

    public AuthoritativePlayerMovementState(
        WorldEntityId entityId,
        GroundPoint position,
        Vector2 velocityMetresPerSecond,
        Vector2 facing,
        PlayerCollisionCapsule collision,
        GroundMovementTickOutcome outcome)
    {
        if (entityId.Value == 0)
            throw new ArgumentException("Player entity identity must be valid.", nameof(entityId));
        if (!IsFinite(velocityMetresPerSecond))
            throw new ArgumentException("Player velocity must be finite.", nameof(velocityMetresPerSecond));
        if (!IsFinite(facing) || MathF.Abs(facing.Length() - 1.0f) > FacingLengthTolerance)
            throw new ArgumentException("Player facing must be finite and normalized.", nameof(facing));
        if (collision.RadiusMetres <= 0.0f || collision.HeightMetres <= collision.RadiusMetres * 2.0f)
            throw new ArgumentException("Player collision capsule must be valid.", nameof(collision));
        if (!Enum.IsDefined(outcome))
            throw new ArgumentOutOfRangeException(nameof(outcome));

        EntityId = entityId;
        Position = position;
        VelocityMetresPerSecond = velocityMetresPerSecond;
        Facing = facing;
        Collision = collision;
        Outcome = outcome;
    }

    public WorldEntityId EntityId
    {
        get;
    }

    public GroundPoint Position
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

    public GroundMovementTickOutcome Outcome
    {
        get;
    }

    private static bool IsFinite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);
}

public sealed class Draft0PlayerMovementSimulation : IDisposable
{
    public const int TickRateHz = 60;
    public const float SpeedMetresPerSecond = 4.0f;
    public const float PlayerRadiusMetres = 0.35f;
    public const float PlayerHeightMetres = 1.8f;

    private const ulong EnvironmentCategory = 1UL << 0;
    private const ulong PlayerMoverCategory = 1UL << 1;
    private static readonly PlayerCollisionCapsule TechnicalPlayerCollision =
        new(PlayerRadiusMetres, PlayerHeightMetres);
    private static readonly Box3DFilter EnvironmentFilter =
        new(EnvironmentCategory, PlayerMoverCategory);
    private static readonly Box3DQueryFilter PlayerQueryFilter =
        new(PlayerMoverCategory, EnvironmentCategory);
    private static readonly Vector3 CapsuleCenter1 =
        new(0.0f, PlayerRadiusMetres, 0.0f);
    private static readonly Vector3 CapsuleCenter2 =
        new(0.0f, PlayerHeightMetres - PlayerRadiusMetres, 0.0f);

    private readonly Draft0GrayboxLayout layout;
    private readonly Box3DWorld physicsWorld;
    private readonly Dictionary<WorldEntityId, PlayerMover> players = [];
    private bool disposed;

    public Draft0PlayerMovementSimulation(Draft0GrayboxLayout layout)
    {
        this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
        physicsWorld = Box3DWorld.Create(Vector3.Zero);
        try
        {
            CreateStaticCollision();
            physicsWorld.Step(1.0f / TickRateHz, 1);
        }
        catch
        {
            physicsWorld.Dispose();
            throw;
        }
    }

    public int PlayerCount
    {
        get
        {
            ThrowIfDisposed();
            return players.Count;
        }
    }

    public AuthoritativePlayerMovementState RegisterPlayer(
        WorldEntityId entityId,
        GroundPoint position,
        Vector2 facing)
    {
        ThrowIfDisposed();
        if (entityId.Value == 0)
            throw new ArgumentException("Player entity identity must be valid.", nameof(entityId));
        if (!IsFiniteNormalized(facing))
            throw new ArgumentException("Player facing must be finite and normalized.", nameof(facing));
        RequireClearPosition(position, nameof(position));
        if (players.ContainsKey(entityId))
            throw new InvalidOperationException($"Player {entityId} is already registered for movement.");

        var state = new AuthoritativePlayerMovementState(
            entityId,
            position,
            Vector2.Zero,
            facing,
            TechnicalPlayerCollision,
            GroundMovementTickOutcome.Idle);
        players.Add(entityId, new PlayerMover(state));
        return state;
    }

    public bool RemovePlayer(WorldEntityId entityId)
    {
        ThrowIfDisposed();
        return players.Remove(entityId);
    }

    public bool TryGetPlayer(
        WorldEntityId entityId,
        out AuthoritativePlayerMovementState state)
    {
        ThrowIfDisposed();
        if (players.TryGetValue(entityId, out PlayerMover? player))
        {
            state = player.State;
            return true;
        }

        state = default;
        return false;
    }

    public GroundMovementIntentDisposition Submit(GroundMovementIntent intent)
    {
        ThrowIfDisposed();
        if (!players.TryGetValue(intent.EntityId, out PlayerMover? player))
            return GroundMovementIntentDisposition.UnknownPlayer;
        if (!ContainsPlayerCenter(intent.Destination))
            return GroundMovementIntentDisposition.OutsideWalkableBounds;
        if (OverlapsProxy(intent.Destination))
            return GroundMovementIntentDisposition.ObstructedDestination;

        player.Destination = intent.Destination;
        return GroundMovementIntentDisposition.Accepted;
    }

    public IReadOnlyList<AuthoritativePlayerMovementState> Step()
    {
        ThrowIfDisposed();
        physicsWorld.Step(1.0f / TickRateHz, 1);

        AuthoritativePlayerMovementState[] states = players
            .OrderBy(static pair => pair.Key.Value)
            .Select(pair => Advance(pair.Value))
            .ToArray();
        return new ReadOnlyCollection<AuthoritativePlayerMovementState>(states);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        players.Clear();
        physicsWorld.Dispose();
        disposed = true;
    }

    private AuthoritativePlayerMovementState Advance(PlayerMover player)
    {
        AuthoritativePlayerMovementState previous = player.State;
        if (!player.Destination.HasValue)
        {
            player.State = WithMotion(
                previous,
                previous.Position,
                Vector2.Zero,
                previous.Facing,
                GroundMovementTickOutcome.Idle);
            return player.State;
        }

        Vector2 current = ToPlane(previous.Position);
        Vector2 target = ToPlane(player.Destination.Value);
        Vector2 difference = target - current;
        float distance = difference.Length();
        if (distance == 0.0f)
        {
            player.Destination = null;
            player.State = WithMotion(
                previous,
                previous.Position,
                Vector2.Zero,
                previous.Facing,
                GroundMovementTickOutcome.Arrived);
            return player.State;
        }

        Vector2 direction = difference / distance;
        float maximumStep = SpeedMetresPerSecond / TickRateHz;
        float stepLength = MathF.Min(distance, maximumStep);
        Vector2 planarTranslation = direction * stepLength;
        Vector3 translation = new(planarTranslation.X, 0.0f, planarTranslation.Y);
        float fraction = Math.Clamp(
            physicsWorld.CastMover(
                previous.Position.Metres,
                CapsuleCenter1,
                CapsuleCenter2,
                PlayerRadiusMetres,
                translation,
                PlayerQueryFilter),
            0.0f,
            1.0f);

        if (fraction < 1.0f)
        {
            Vector2 stopped = current + (planarTranslation * fraction);
            player.Destination = null;
            player.State = WithMotion(
                previous,
                new GroundPoint(stopped.X, stopped.Y),
                Vector2.Zero,
                direction,
                GroundMovementTickOutcome.Blocked);
            return player.State;
        }

        if (distance <= maximumStep + Draft0GrayboxLayout.ValidationToleranceMetres)
        {
            GroundPoint destination = player.Destination.Value;
            player.Destination = null;
            player.State = WithMotion(
                previous,
                destination,
                Vector2.Zero,
                direction,
                GroundMovementTickOutcome.Arrived);
            return player.State;
        }

        Vector2 moved = current + planarTranslation;
        player.State = WithMotion(
            previous,
            new GroundPoint(moved.X, moved.Y),
            direction * SpeedMetresPerSecond,
            direction,
            GroundMovementTickOutcome.Moving);
        return player.State;
    }

    private void CreateStaticCollision()
    {
        GroundBounds zone = layout.Specification.Bounds;
        GroundBounds walkable = layout.WalkableBounds;

        CreateBox(
            new GroundBounds(
                zone.Minimum,
                new GroundPoint(zone.Maximum.XMetres, walkable.Minimum.ZMetres)),
            PlayerHeightMetres);
        CreateBox(
            new GroundBounds(
                new GroundPoint(zone.Minimum.XMetres, walkable.Maximum.ZMetres),
                zone.Maximum),
            PlayerHeightMetres);
        CreateBox(
            new GroundBounds(
                new GroundPoint(zone.Minimum.XMetres, walkable.Minimum.ZMetres),
                new GroundPoint(walkable.Minimum.XMetres, walkable.Maximum.ZMetres)),
            PlayerHeightMetres);
        CreateBox(
            new GroundBounds(
                new GroundPoint(walkable.Maximum.XMetres, walkable.Minimum.ZMetres),
                new GroundPoint(zone.Maximum.XMetres, walkable.Maximum.ZMetres)),
            PlayerHeightMetres);

        foreach (Draft0ProxyBlock proxy in layout.Proxies)
            CreateBox(proxy.Footprint, proxy.HeightMetres);
    }

    private void CreateBox(GroundBounds footprint, float heightMetres)
    {
        GroundDimensions dimensions = footprint.Dimensions;
        Vector3 position = new(
            (footprint.Minimum.XMetres + footprint.Maximum.XMetres) * 0.5f,
            heightMetres * 0.5f,
            (footprint.Minimum.ZMetres + footprint.Maximum.ZMetres) * 0.5f);
        Vector3 halfExtents = new(
            dimensions.WidthMetres * 0.5f,
            heightMetres * 0.5f,
            dimensions.DepthMetres * 0.5f);
        Box3DBody body = physicsWorld.CreateBody(Box3DBodyKind.Static, position, Quaternion.Identity);
        body.CreateBoxShape(halfExtents, EnvironmentFilter);
    }

    private void RequireClearPosition(GroundPoint point, string parameterName)
    {
        if (!ContainsPlayerCenter(point))
            throw new ArgumentOutOfRangeException(parameterName, point, "Player position lies outside the walkable bounds.");
        if (OverlapsProxy(point))
            throw new ArgumentException("Player position overlaps collidable proxy geometry.", parameterName);
    }

    private bool ContainsPlayerCenter(GroundPoint point)
    {
        GroundBounds bounds = layout.WalkableBounds;
        return point.XMetres >= bounds.Minimum.XMetres + PlayerRadiusMetres &&
            point.XMetres <= bounds.Maximum.XMetres - PlayerRadiusMetres &&
            point.ZMetres >= bounds.Minimum.ZMetres + PlayerRadiusMetres &&
            point.ZMetres <= bounds.Maximum.ZMetres - PlayerRadiusMetres;
    }

    private bool OverlapsProxy(GroundPoint point)
    {
        foreach (Draft0ProxyBlock proxy in layout.Proxies)
        {
            float nearestX = Math.Clamp(
                point.XMetres,
                proxy.Footprint.Minimum.XMetres,
                proxy.Footprint.Maximum.XMetres);
            float nearestZ = Math.Clamp(
                point.ZMetres,
                proxy.Footprint.Minimum.ZMetres,
                proxy.Footprint.Maximum.ZMetres);
            float deltaX = point.XMetres - nearestX;
            float deltaZ = point.ZMetres - nearestZ;
            if ((deltaX * deltaX) + (deltaZ * deltaZ) <= PlayerRadiusMetres * PlayerRadiusMetres)
                return true;
        }

        return false;
    }

    private static AuthoritativePlayerMovementState WithMotion(
        AuthoritativePlayerMovementState previous,
        GroundPoint position,
        Vector2 velocity,
        Vector2 facing,
        GroundMovementTickOutcome outcome) =>
        new(previous.EntityId, position, velocity, facing, previous.Collision, outcome);

    private static Vector2 ToPlane(GroundPoint point) =>
        new(point.XMetres, point.ZMetres);

    private static bool IsFiniteNormalized(Vector2 value) =>
        float.IsFinite(value.X) &&
        float.IsFinite(value.Y) &&
        MathF.Abs(value.Length() - 1.0f) <= 1e-4f;

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);

    private sealed class PlayerMover(AuthoritativePlayerMovementState state)
    {
        internal AuthoritativePlayerMovementState State { get; set; } = state;

        internal GroundPoint? Destination
        {
            get; set;
        }
    }
}
