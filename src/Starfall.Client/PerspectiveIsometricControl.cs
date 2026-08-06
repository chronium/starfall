using System.Numerics;
using Starfall.Content.Zones;

namespace Starfall.Client;

internal sealed class PerspectiveIsometricCameraSettings
{
    internal PerspectiveIsometricCameraSettings(
        float verticalFieldOfViewDegrees,
        float downwardPitchDegrees,
        float yawDegrees,
        float focusDistanceMetres,
        float nearPlaneMetres,
        float farPlaneMetres)
    {
        if (!float.IsFinite(verticalFieldOfViewDegrees) ||
            verticalFieldOfViewDegrees <= 0.0f ||
            verticalFieldOfViewDegrees >= 180.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(verticalFieldOfViewDegrees),
                "Vertical field of view must be finite and between 0 and 180 degrees.");
        }
        if (!float.IsFinite(downwardPitchDegrees) ||
            downwardPitchDegrees <= 0.0f ||
            downwardPitchDegrees >= 90.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(downwardPitchDegrees),
                "Downward pitch must be finite and between 0 and 90 degrees.");
        }
        if (!float.IsFinite(yawDegrees))
            throw new ArgumentOutOfRangeException(nameof(yawDegrees), "Yaw must be finite.");
        if (!float.IsFinite(focusDistanceMetres) || focusDistanceMetres <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(focusDistanceMetres),
                "Focus distance must be a positive finite metre value.");
        }
        if (!float.IsFinite(nearPlaneMetres) || nearPlaneMetres <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nearPlaneMetres),
                "Near plane must be a positive finite metre value.");
        }
        if (!float.IsFinite(farPlaneMetres) || farPlaneMetres <= nearPlaneMetres)
        {
            throw new ArgumentOutOfRangeException(
                nameof(farPlaneMetres),
                "Far plane must be finite and greater than the near plane.");
        }

        VerticalFieldOfViewDegrees = verticalFieldOfViewDegrees;
        DownwardPitchDegrees = downwardPitchDegrees;
        YawDegrees = yawDegrees;
        FocusDistanceMetres = focusDistanceMetres;
        NearPlaneMetres = nearPlaneMetres;
        FarPlaneMetres = farPlaneMetres;
    }

    internal static PerspectiveIsometricCameraSettings Draft0
    {
        get;
    } = new(
        verticalFieldOfViewDegrees: 28.0f,
        downwardPitchDegrees: 42.0f,
        yawDegrees: 45.0f,
        focusDistanceMetres: 22.5f,
        nearPlaneMetres: 0.1f,
        farPlaneMetres: 300.0f);

    internal float VerticalFieldOfViewDegrees
    {
        get;
    }

    internal float DownwardPitchDegrees
    {
        get;
    }

    internal float YawDegrees
    {
        get;
    }

    internal float FocusDistanceMetres
    {
        get;
    }

    internal float NearPlaneMetres
    {
        get;
    }

    internal float FarPlaneMetres
    {
        get;
    }
}

internal sealed class PerspectiveIsometricCamera
{
    private const float HomogeneousTolerance = 1e-6f;
    private const float ParallelTolerance = 1e-6f;

    internal PerspectiveIsometricCamera(
        GroundPoint focus,
        PerspectiveIsometricCameraSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Focus = focus;
        Settings = settings;

        float pitchRadians = DegreesToRadians(settings.DownwardPitchDegrees);
        float yawRadians = DegreesToRadians(settings.YawDegrees);
        float horizontalDistance = settings.FocusDistanceMetres * MathF.Cos(pitchRadians);
        var horizontalDirection = new Vector3(MathF.Cos(yawRadians), 0.0f, MathF.Sin(yawRadians));
        Position = focus.Metres +
            horizontalDirection * horizontalDistance +
            Vector3.UnitY * (settings.FocusDistanceMetres * MathF.Sin(pitchRadians));
        View = Matrix4x4.CreateLookAt(Position, focus.Metres, Vector3.UnitY);
    }

    internal GroundPoint Focus
    {
        get;
    }

    internal PerspectiveIsometricCameraSettings Settings
    {
        get;
    }

    internal Vector3 Position
    {
        get;
    }

    internal Matrix4x4 View
    {
        get;
    }

    internal Matrix4x4 CreateViewProjection(uint width, uint height)
    {
        if (width == 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Viewport width must be positive.");
        if (height == 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Viewport height must be positive.");

        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            DegreesToRadians(Settings.VerticalFieldOfViewDegrees),
            width / (float)height,
            Settings.NearPlaneMetres,
            Settings.FarPlaneMetres);
        return View * projection;
    }

    internal bool TryPickGround(
        Vector2 normalizedViewportPosition,
        uint drawableWidth,
        uint drawableHeight,
        GroundBounds validGround,
        out GroundPoint point)
    {
        point = default;
        if (!TryCreateWorldRay(
                normalizedViewportPosition,
                drawableWidth,
                drawableHeight,
                out PerspectiveWorldRay ray))
            return false;

        if (MathF.Abs(ray.Direction.Y) <= ParallelTolerance)
            return false;

        float distance = -ray.Origin.Y / ray.Direction.Y;
        if (!float.IsFinite(distance) || distance < 0.0f)
            return false;

        Vector3 intersection = ray.Origin + ray.Direction * distance;
        if (!IsFinite(intersection))
            return false;

        var candidate = new GroundPoint(intersection.X, intersection.Z);
        if (!validGround.Contains(candidate))
            return false;

        point = candidate;
        return true;
    }

    internal bool TryCreateWorldRay(
        Vector2 normalizedViewportPosition,
        uint drawableWidth,
        uint drawableHeight,
        out PerspectiveWorldRay ray)
    {
        ray = default;
        if (!float.IsFinite(normalizedViewportPosition.X) ||
            !float.IsFinite(normalizedViewportPosition.Y) ||
            normalizedViewportPosition.X < 0.0f ||
            normalizedViewportPosition.X > 1.0f ||
            normalizedViewportPosition.Y < 0.0f ||
            normalizedViewportPosition.Y > 1.0f ||
            drawableWidth == 0 ||
            drawableHeight == 0)
        {
            return false;
        }

        Matrix4x4 viewProjection = CreateViewProjection(drawableWidth, drawableHeight);
        if (!Matrix4x4.Invert(viewProjection, out Matrix4x4 inverseViewProjection))
            return false;

        float ndcX = normalizedViewportPosition.X * 2.0f - 1.0f;
        float ndcY = 1.0f - normalizedViewportPosition.Y * 2.0f;
        if (!TryUnproject(new Vector4(ndcX, ndcY, 0.0f, 1.0f), inverseViewProjection, out Vector3 near))
            return false;

        Vector3 direction = near - Position;
        float lengthSquared = direction.LengthSquared();
        if (!IsFinite(direction) || !float.IsFinite(lengthSquared) || lengthSquared <= ParallelTolerance * ParallelTolerance)
            return false;

        ray = new PerspectiveWorldRay(Position, Vector3.Normalize(direction));
        return true;
    }

    private static bool TryUnproject(
        Vector4 clipPosition,
        Matrix4x4 inverseViewProjection,
        out Vector3 position)
    {
        Vector4 homogeneous = Vector4.Transform(clipPosition, inverseViewProjection);
        if (!float.IsFinite(homogeneous.X) ||
            !float.IsFinite(homogeneous.Y) ||
            !float.IsFinite(homogeneous.Z) ||
            !float.IsFinite(homogeneous.W) ||
            MathF.Abs(homogeneous.W) <= HomogeneousTolerance)
        {
            position = default;
            return false;
        }

        position = new Vector3(homogeneous.X, homogeneous.Y, homogeneous.Z) / homogeneous.W;
        return IsFinite(position);
    }

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    private static float DegreesToRadians(float degrees) => degrees * (MathF.PI / 180.0f);
}

internal readonly record struct PerspectiveWorldRay(Vector3 Origin, Vector3 Direction);

internal readonly record struct GroundMovementIntent(GroundPoint Destination);

internal static class GroundMovementInput
{
    internal static bool TryCreateIntent(
        PerspectiveIsometricCamera camera,
        GroundBounds validGround,
        float logicalX,
        float logicalY,
        int logicalWidth,
        int logicalHeight,
        uint drawableWidth,
        uint drawableHeight,
        out GroundMovementIntent intent)
    {
        ArgumentNullException.ThrowIfNull(camera);
        intent = default;
        if (!float.IsFinite(logicalX) ||
            !float.IsFinite(logicalY) ||
            logicalWidth <= 0 ||
            logicalHeight <= 0 ||
            logicalX < 0.0f ||
            logicalX > logicalWidth ||
            logicalY < 0.0f ||
            logicalY > logicalHeight)
        {
            return false;
        }

        var normalized = new Vector2(logicalX / logicalWidth, logicalY / logicalHeight);
        if (!camera.TryPickGround(normalized, drawableWidth, drawableHeight, validGround, out GroundPoint point))
            return false;

        intent = new GroundMovementIntent(point);
        return true;
    }
}
