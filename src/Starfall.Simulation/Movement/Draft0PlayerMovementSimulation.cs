using System.Collections.ObjectModel;
using System.Numerics;
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

    private static readonly PlayerCollisionCapsule TechnicalPlayerCollision =
        new(PlayerRadiusMetres, PlayerHeightMetres);

    private readonly Draft0GroundCollisionWorld collisionWorld;
    private readonly bool advancesCollisionWorld;
    private readonly bool ownsCollisionWorld;
    private readonly Dictionary<WorldEntityId, PlayerMover> players = [];
    private bool disposed;

    public Draft0PlayerMovementSimulation(Draft0GrayboxLayout layout)
        : this(
            layout,
            new Draft0GroundCollisionWorld(layout ?? throw new ArgumentNullException(nameof(layout))),
            advancesCollisionWorld: true,
            ownsCollisionWorld: true)
    {
    }

    internal Draft0PlayerMovementSimulation(
        Draft0GrayboxLayout layout,
        Draft0GroundCollisionWorld collisionWorld,
        bool advancesCollisionWorld,
        bool ownsCollisionWorld)
    {
        ArgumentNullException.ThrowIfNull(layout);
        this.collisionWorld = collisionWorld ?? throw new ArgumentNullException(nameof(collisionWorld));
        this.advancesCollisionWorld = advancesCollisionWorld;
        this.ownsCollisionWorld = ownsCollisionWorld;
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

    public AuthoritativePlayerMovementState StopAndFace(
        WorldEntityId entityId,
        Vector2 facing)
    {
        ThrowIfDisposed();
        if (!players.TryGetValue(entityId, out PlayerMover? player))
            throw new InvalidOperationException($"Cannot stop unknown player {entityId}.");
        if (!IsFiniteNormalized(facing))
            throw new ArgumentException("Player facing must be finite and normalized.", nameof(facing));

        player.Destination = null;
        player.State = WithMotion(
            player.State,
            player.State.Position,
            Vector2.Zero,
            facing,
            GroundMovementTickOutcome.Idle);
        return player.State;
    }

    public IReadOnlyList<AuthoritativePlayerMovementState> Step()
    {
        ThrowIfDisposed();
        if (advancesCollisionWorld)
            collisionWorld.Step();

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
        if (ownsCollisionWorld)
            collisionWorld.Dispose();
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
        float fraction = collisionWorld.CastCapsuleMover(
            previous.Position,
            PlayerRadiusMetres,
            PlayerHeightMetres,
            planarTranslation);

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

    private void RequireClearPosition(GroundPoint point, string parameterName)
    {
        if (!collisionWorld.ContainsWalkableCenter(point, PlayerRadiusMetres))
            throw new ArgumentOutOfRangeException(parameterName, point, "Player position lies outside the walkable bounds.");
        if (collisionWorld.OverlapsProxy(point, PlayerRadiusMetres))
            throw new ArgumentException("Player position overlaps collidable proxy geometry.", parameterName);
    }

    private bool ContainsPlayerCenter(GroundPoint point) =>
        collisionWorld.ContainsWalkableCenter(point, PlayerRadiusMetres);

    private bool OverlapsProxy(GroundPoint point) =>
        collisionWorld.OverlapsProxy(point, PlayerRadiusMetres);

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
