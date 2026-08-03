using System.Numerics;
using Starfall.Client;
using Starfall.Content.Zones;

namespace Starfall.Client.Tests;

public sealed class PerspectiveIsometricControlTests
{
    private const float PickingToleranceMetres = 0.01f;

    private static readonly GroundBounds ZoneBounds =
        new(new GroundPoint(0.0f, 0.0f), new GroundPoint(200.0f, 200.0f));

    [Fact]
    public void Draft0SettingsFreezeApprovedPerspectiveInputs()
    {
        PerspectiveIsometricCameraSettings settings = PerspectiveIsometricCameraSettings.Draft0;

        Assert.Equal(28.0f, settings.VerticalFieldOfViewDegrees);
        Assert.Equal(42.0f, settings.DownwardPitchDegrees);
        Assert.Equal(45.0f, settings.YawDegrees);
        Assert.Equal(45.0f, settings.FocusDistanceMetres);
        Assert.Equal(0.1f, settings.NearPlaneMetres);
        Assert.Equal(300.0f, settings.FarPlaneMetres);

        var camera = CreateCamera();
        Assert.True(Matrix4x4.Invert(camera.CreateViewProjection(960, 720), out _));
        Assert.True(float.IsFinite(camera.Position.X));
        Assert.True(float.IsFinite(camera.Position.Y));
        Assert.True(float.IsFinite(camera.Position.Z));
    }

    [Fact]
    public void CentreScreenMapsToTheGroundFocus()
    {
        var focus = new GroundPoint(100.0f, 100.0f);
        var camera = new PerspectiveIsometricCamera(focus, PerspectiveIsometricCameraSettings.Draft0);

        Assert.True(camera.TryPickGround(new Vector2(0.5f, 0.5f), 960, 720, ZoneBounds, out GroundPoint picked));
        AssertPoint(focus, picked);
    }

    [Fact]
    public void PickingIsDeterministicAcrossRepeatedCalls()
    {
        var camera = CreateCamera();
        var screen = new Vector2(0.32f, 0.68f);

        Assert.True(camera.TryPickGround(screen, 960, 720, ZoneBounds, out GroundPoint first));
        for (var iteration = 0; iteration < 20; iteration++)
        {
            Assert.True(camera.TryPickGround(screen, 960, 720, ZoneBounds, out GroundPoint repeated));
            Assert.Equal(first, repeated);
        }
    }

    [Fact]
    public void LogicalToDrawableMappingIsResolutionIndependentAtTheSameAspect()
    {
        var camera = CreateCamera();

        Assert.True(GroundMovementInput.TryCreateIntent(
            camera,
            ZoneBounds,
            logicalX: 240.0f,
            logicalY: 180.0f,
            logicalWidth: 960,
            logicalHeight: 720,
            drawableWidth: 960,
            drawableHeight: 720,
            out GroundMovementIntent standard));
        Assert.True(GroundMovementInput.TryCreateIntent(
            camera,
            ZoneBounds,
            logicalX: 240.0f,
            logicalY: 180.0f,
            logicalWidth: 960,
            logicalHeight: 720,
            drawableWidth: 1920,
            drawableHeight: 1440,
            out GroundMovementIntent highDensity));

        AssertPoint(standard.Destination, highDensity.Destination);
    }

    [Theory]
    [InlineData(100.0f, 100.0f)]
    [InlineData(94.0f, 97.0f)]
    [InlineData(107.0f, 103.0f)]
    public void ProjectedGroundPointsRoundTripThroughPicking(float xMetres, float zMetres)
    {
        var camera = CreateCamera();
        var expected = new GroundPoint(xMetres, zMetres);
        Vector2 normalized = ProjectGround(camera, expected, 1280, 720);

        Assert.InRange(normalized.X, 0.0f, 1.0f);
        Assert.InRange(normalized.Y, 0.0f, 1.0f);
        Assert.True(camera.TryPickGround(normalized, 1280, 720, ZoneBounds, out GroundPoint picked));
        AssertPoint(expected, picked);
    }

    [Fact]
    public void PickingRejectsPointsOutsideTheSuppliedGroundBounds()
    {
        var camera = CreateCamera();
        var excluded = new GroundBounds(new GroundPoint(0.0f, 0.0f), new GroundPoint(50.0f, 50.0f));

        Assert.False(camera.TryPickGround(new Vector2(0.5f, 0.5f), 960, 720, excluded, out _));
    }

    [Theory]
    [InlineData(float.NaN, 100.0f, 960, 720, 960u, 720u)]
    [InlineData(-1.0f, 100.0f, 960, 720, 960u, 720u)]
    [InlineData(100.0f, 721.0f, 960, 720, 960u, 720u)]
    [InlineData(100.0f, 100.0f, 0, 720, 960u, 720u)]
    [InlineData(100.0f, 100.0f, 960, 720, 0u, 720u)]
    public void InputRejectsMalformedViewportCoordinates(
        float logicalX,
        float logicalY,
        int logicalWidth,
        int logicalHeight,
        uint drawableWidth,
        uint drawableHeight)
    {
        Assert.False(GroundMovementInput.TryCreateIntent(
            CreateCamera(),
            ZoneBounds,
            logicalX,
            logicalY,
            logicalWidth,
            logicalHeight,
            drawableWidth,
            drawableHeight,
            out _));
    }

    [Fact]
    public void CameraSettingsRejectInvalidProjectionInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSettings(verticalFieldOfViewDegrees: 0.0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSettings(downwardPitchDegrees: 90.0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSettings(focusDistanceMetres: float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSettings(nearPlaneMetres: 0.0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSettings(nearPlaneMetres: 5.0f, farPlaneMetres: 5.0f));
    }

    private static PerspectiveIsometricCamera CreateCamera() => new(
        new GroundPoint(100.0f, 100.0f),
        PerspectiveIsometricCameraSettings.Draft0);

    private static PerspectiveIsometricCameraSettings CreateSettings(
        float verticalFieldOfViewDegrees = 28.0f,
        float downwardPitchDegrees = 42.0f,
        float yawDegrees = 45.0f,
        float focusDistanceMetres = 45.0f,
        float nearPlaneMetres = 0.1f,
        float farPlaneMetres = 300.0f) => new(
            verticalFieldOfViewDegrees,
            downwardPitchDegrees,
            yawDegrees,
            focusDistanceMetres,
            nearPlaneMetres,
            farPlaneMetres);

    private static Vector2 ProjectGround(
        PerspectiveIsometricCamera camera,
        GroundPoint point,
        uint width,
        uint height)
    {
        Vector4 clip = Vector4.Transform(new Vector4(point.Metres, 1.0f), camera.CreateViewProjection(width, height));
        Vector3 ndc = new(clip.X / clip.W, clip.Y / clip.W, clip.Z / clip.W);
        return new Vector2((ndc.X + 1.0f) * 0.5f, (1.0f - ndc.Y) * 0.5f);
    }

    private static void AssertPoint(GroundPoint expected, GroundPoint actual)
    {
        Assert.InRange(MathF.Abs(expected.XMetres - actual.XMetres), 0.0f, PickingToleranceMetres);
        Assert.InRange(MathF.Abs(expected.ZMetres - actual.ZMetres), 0.0f, PickingToleranceMetres);
    }
}
