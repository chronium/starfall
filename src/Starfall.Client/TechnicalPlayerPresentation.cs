using System.Globalization;
using System.Numerics;
using ChronoFall.CharacterPresentation;
using SDL;
using Starfall.Content.Zones;

namespace Starfall.Client;

internal enum TechnicalPlayerLocomotion
{
    Idle,
    Walking,
}

internal readonly record struct TechnicalPlayerSnapshot
{
    private const float FacingLengthTolerance = 1e-4f;

    internal TechnicalPlayerSnapshot(
        string identity,
        ulong tick,
        GroundPoint position,
        Vector2 velocityMetresPerSecond,
        Vector2 facing)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        if (!IsFinite(velocityMetresPerSecond))
            throw new ArgumentException("Snapshot velocity must be finite.", nameof(velocityMetresPerSecond));
        if (!IsFinite(facing))
            throw new ArgumentException("Snapshot facing must be finite.", nameof(facing));
        float facingLength = facing.Length();
        if (MathF.Abs(facingLength - 1.0f) > FacingLengthTolerance)
            throw new ArgumentException("Snapshot facing must be normalized.", nameof(facing));

        Identity = identity;
        Tick = tick;
        Position = position;
        VelocityMetresPerSecond = velocityMetresPerSecond;
        Facing = facing;
    }

    internal string Identity
    {
        get;
    }

    internal ulong Tick
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

internal readonly record struct TechnicalPlayerPresentationState(
    TechnicalPlayerSnapshot Snapshot,
    Matrix4x4 World,
    TechnicalPlayerLocomotion Locomotion);

internal static class TechnicalPlayerPresentationAdapter
{
    private const float MovingSpeedThresholdMetresPerSecond = 0.01f;

    internal static TechnicalPlayerPresentationState Adapt(TechnicalPlayerSnapshot snapshot)
    {
        float yaw = MathF.Atan2(snapshot.Facing.X, snapshot.Facing.Y);
        Matrix4x4 world =
            Matrix4x4.CreateRotationY(yaw) *
            Matrix4x4.CreateTranslation(snapshot.Position.Metres);
        TechnicalPlayerLocomotion locomotion =
            snapshot.VelocityMetresPerSecond.LengthSquared() >
                MovingSpeedThresholdMetresPerSecond * MovingSpeedThresholdMetresPerSecond
                ? TechnicalPlayerLocomotion.Walking
                : TechnicalPlayerLocomotion.Idle;
        return new TechnicalPlayerPresentationState(snapshot, world, locomotion);
    }
}

internal sealed class Draft0LocalWalkingFixture
{
    internal const string Identity = "local_technical_player";
    internal const int TickRate = 60;
    internal const int DefaultSpeedTenths = 40;
    internal const int MinimumSpeedTenths = 1;
    internal const int MaximumSpeedTenths = 120;

    private GroundPoint? destination;
    private GroundPoint position;
    private Vector2 facing = Vector2.UnitY;
    private ulong tick;

    internal Draft0LocalWalkingFixture(GroundPoint initialPosition)
    {
        position = initialPosition;
        Snapshot = CreateSnapshot(Vector2.Zero);
    }

    internal int SpeedTenths
    {
        get;
        private set;
    } = DefaultSpeedTenths;

    internal float SpeedMetresPerSecond => SpeedTenths / 10.0f;

    internal TechnicalPlayerSnapshot Snapshot
    {
        get;
        private set;
    }

    internal GroundPoint? Destination => destination;

    internal void Submit(GroundMovementIntent intent) => destination = intent.Destination;

    internal bool AdjustSpeedTenths(int delta)
    {
        int adjusted = Math.Clamp(SpeedTenths + delta, MinimumSpeedTenths, MaximumSpeedTenths);
        if (adjusted == SpeedTenths)
            return false;
        SpeedTenths = adjusted;
        return true;
    }

    internal void AdvanceTick()
    {
        tick = checked(tick + 1);
        if (!destination.HasValue)
        {
            Snapshot = CreateSnapshot(Vector2.Zero);
            return;
        }

        Vector2 current = ToPlane(position);
        Vector2 target = ToPlane(destination.Value);
        Vector2 difference = target - current;
        float distance = difference.Length();
        if (distance <= 0.0f)
        {
            destination = null;
            Snapshot = CreateSnapshot(Vector2.Zero);
            return;
        }

        Vector2 direction = difference / distance;
        facing = direction;
        float maximumStep = SpeedMetresPerSecond / TickRate;
        if (distance <= maximumStep)
        {
            position = destination.Value;
            destination = null;
            Snapshot = CreateSnapshot(Vector2.Zero);
            return;
        }

        current += direction * maximumStep;
        position = new GroundPoint(current.X, current.Y);
        Snapshot = CreateSnapshot(direction * SpeedMetresPerSecond);
    }

    private TechnicalPlayerSnapshot CreateSnapshot(Vector2 velocity) =>
        new(Identity, tick, position, velocity, facing);

    private static Vector2 ToPlane(GroundPoint point) =>
        new(point.XMetres, point.ZMetres);
}

internal sealed class FixedTickAccumulator
{
    internal const double MaximumElapsedSeconds = 0.25;
    private const double TickSeconds = 1.0 / Draft0LocalWalkingFixture.TickRate;
    private double accumulatedSeconds;

    internal int Advance(double elapsedSeconds, Action advanceTick)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0.0)
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), "Elapsed time must be finite and non-negative.");
        ArgumentNullException.ThrowIfNull(advanceTick);

        accumulatedSeconds += Math.Min(elapsedSeconds, MaximumElapsedSeconds);
        var advanced = 0;
        while (accumulatedSeconds >= TickSeconds)
        {
            advanceTick();
            accumulatedSeconds -= TickSeconds;
            advanced++;
        }
        return advanced;
    }
}

internal static class Draft0LocalWalkingControls
{
    internal static bool TryAdjustSpeed(
        Draft0LocalWalkingFixture fixture,
        SDL_Keycode key,
        bool repeated)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        if (repeated)
            return false;

        int delta = key switch
        {
            SDL_Keycode.SDLK_KP_PLUS => 1,
            SDL_Keycode.SDLK_KP_MINUS => -1,
            _ => 0,
        };
        return delta != 0 && fixture.AdjustSpeedTenths(delta);
    }
}

internal static class Draft0LocalPreviewTitle
{
    internal static string Create(
        string viewName,
        int speedTenths,
        float cameraDistanceMetres)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewName);
        if (speedTenths < Draft0LocalWalkingFixture.MinimumSpeedTenths ||
            speedTenths > Draft0LocalWalkingFixture.MaximumSpeedTenths)
        {
            throw new ArgumentOutOfRangeException(nameof(speedTenths));
        }
        if (!float.IsFinite(cameraDistanceMetres) || cameraDistanceMetres <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(cameraDistanceMetres));

        float speedMetresPerSecond = speedTenths / 10.0f;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Starfall - Draft 0 Local Graybox [{viewName}] " +
            $"[speed {speedMetresPerSecond:F1} m/s] " +
            $"[camera {cameraDistanceMetres:F1} m]");
    }
}

internal sealed class TechnicalPlayerLocomotionPlayback
{
    internal const float BlendDurationSeconds = 0.25f;
    internal const float WalkReferenceSpeedMetresPerSecond = 1.0f;

    private readonly AnimationClip idle;
    private readonly AnimationClip walk;
    private AnimationClip active;
    private double activeSampleTime;
    private SkeletonPose? blendSource;
    private double blendElapsed;
    private TechnicalPlayerLocomotion locomotion;

    internal TechnicalPlayerLocomotionPlayback(AnimationClip idle, AnimationClip walk)
    {
        this.idle = idle ?? throw new ArgumentNullException(nameof(idle));
        this.walk = walk ?? throw new ArgumentNullException(nameof(walk));
        if (!ReferenceEquals(idle.Skeleton, walk.Skeleton))
            throw new ArgumentException("Idle and walk clips must use the same skeleton.", nameof(walk));
        active = idle;
    }

    internal TechnicalPlayerLocomotion Locomotion => locomotion;

    internal bool IsBlending => blendSource is not null;

    internal float BlendAmount => blendSource is null
        ? 1.0f
        : Math.Clamp((float)(blendElapsed / BlendDurationSeconds), 0.0f, 1.0f);

    internal float PlaybackRate
    {
        get;
        private set;
    } = 1.0f;

    internal void SetLocomotion(TechnicalPlayerLocomotion value)
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentOutOfRangeException(nameof(value));
        if (value == locomotion)
            return;

        blendSource = CreatePose();
        blendElapsed = 0.0;
        activeSampleTime = 0.0;
        active = value == TechnicalPlayerLocomotion.Walking ? walk : idle;
        locomotion = value;
    }

    internal void Advance(double elapsedSeconds, float planarSpeedMetresPerSecond)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0.0)
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), "Elapsed time must be finite and non-negative.");
        if (!float.IsFinite(planarSpeedMetresPerSecond) || planarSpeedMetresPerSecond < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(planarSpeedMetresPerSecond),
                "Planar speed must be finite and non-negative.");
        }

        PlaybackRate = locomotion == TechnicalPlayerLocomotion.Walking
            ? MathF.Sqrt(planarSpeedMetresPerSecond / WalkReferenceSpeedMetresPerSecond)
            : 1.0f;
        activeSampleTime += elapsedSeconds * PlaybackRate;
        if (blendSource is null)
            return;
        blendElapsed += elapsedSeconds;
        if (blendElapsed >= BlendDurationSeconds)
        {
            blendElapsed = BlendDurationSeconds;
            blendSource = null;
        }
    }

    internal SkeletonPose CreatePose()
    {
        SkeletonPose destinationPose = AnimationSampler.Sample(
            active,
            (float)activeSampleTime,
            AnimationPlaybackMode.Loop);
        return blendSource is null
            ? destinationPose
            : SkeletonPoseBlender.Blend(blendSource, destinationPose, BlendAmount);
    }
}
