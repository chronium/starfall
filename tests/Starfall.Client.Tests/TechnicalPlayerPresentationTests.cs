using System.Numerics;
using ChronoFall.CharacterPresentation;
using SDL;
using Starfall.Client;
using Starfall.Content.Zones;

namespace Starfall.Client.Tests;

public sealed class TechnicalPlayerPresentationTests
{
    private const float Tolerance = 1e-4f;

    [Fact]
    public void LocalFixtureStartsAtTheRespawnAnchorWithExactTuningState()
    {
        GroundPoint respawn = Draft0GrayboxCatalog.FirstPlayable.Town.RespawnAnchor;
        var fixture = new Draft0LocalWalkingFixture(respawn);

        Assert.Equal(Draft0LocalWalkingFixture.Identity, fixture.Snapshot.Identity);
        Assert.Equal(0UL, fixture.Snapshot.Tick);
        Assert.Equal(respawn, fixture.Snapshot.Position);
        Assert.Equal(Vector2.Zero, fixture.Snapshot.VelocityMetresPerSecond);
        Assert.Equal(Vector2.UnitY, fixture.Snapshot.Facing);
        Assert.Equal(40, fixture.SpeedTenths);
        Assert.Equal(4.0f, fixture.SpeedMetresPerSecond);
        Assert.Null(fixture.Destination);
    }

    [Fact]
    public void FixedTicksMoveTowardAndClampExactlyAtTheLatestDestination()
    {
        var fixture = new Draft0LocalWalkingFixture(new GroundPoint(100.0f, 25.0f));
        fixture.Submit(new GroundMovementIntent(new GroundPoint(104.05f, 25.0f)));

        fixture.AdvanceTick();
        AssertPoint(fixture.Snapshot.Position, 100.0f + (4.0f / 60.0f), 25.0f);
        AssertVector(fixture.Snapshot.VelocityMetresPerSecond, 4.0f, 0.0f);
        Assert.Equal(Vector2.UnitX, fixture.Snapshot.Facing);

        for (var tick = 1; tick < 60; tick++)
            fixture.AdvanceTick();
        AssertPoint(fixture.Snapshot.Position, 104.0f, 25.0f);
        Assert.NotNull(fixture.Destination);

        fixture.AdvanceTick();
        AssertPoint(fixture.Snapshot.Position, 104.05f, 25.0f);
        Assert.Equal(Vector2.Zero, fixture.Snapshot.VelocityMetresPerSecond);
        Assert.Equal(Vector2.UnitX, fixture.Snapshot.Facing);
        Assert.Null(fixture.Destination);
        Assert.Equal(61UL, fixture.Snapshot.Tick);
    }

    [Fact]
    public void NewIntentReplacesDestinationAndReplayIsDeterministic()
    {
        Draft0LocalWalkingFixture first = CreateRedirectedFixture();
        Draft0LocalWalkingFixture second = CreateRedirectedFixture();

        Assert.Equal(first.Snapshot, second.Snapshot);
        Assert.Equal(first.Destination, second.Destination);
        Assert.True(first.Snapshot.Facing.Y > 0.0f);
        Assert.True(first.Snapshot.VelocityMetresPerSecond.Y > 0.0f);
    }

    [Fact]
    public void IntegerTenthsAdjustExactlyWithoutFloatAccumulation()
    {
        var fixture = new Draft0LocalWalkingFixture(new GroundPoint(100.0f, 25.0f));

        for (var index = 0; index < 80; index++)
            Assert.True(fixture.AdjustSpeedTenths(1));
        Assert.Equal(120, fixture.SpeedTenths);
        Assert.Equal(12.0f, fixture.SpeedMetresPerSecond);
        Assert.False(fixture.AdjustSpeedTenths(1));

        for (var index = 0; index < 119; index++)
            Assert.True(fixture.AdjustSpeedTenths(-1));
        Assert.Equal(1, fixture.SpeedTenths);
        Assert.Equal(0.1f, fixture.SpeedMetresPerSecond);
        Assert.False(fixture.AdjustSpeedTenths(-1));

        for (var index = 0; index < 39; index++)
            Assert.True(fixture.AdjustSpeedTenths(1));
        Assert.Equal(40, fixture.SpeedTenths);
        Assert.Equal(4.0f, fixture.SpeedMetresPerSecond);
    }

    [Fact]
    public void NumpadSpeedControlsIgnoreRepeatsAndMainKeyboardKeys()
    {
        var fixture = new Draft0LocalWalkingFixture(new GroundPoint(100.0f, 25.0f));

        Assert.False(Draft0LocalWalkingControls.TryAdjustSpeed(
            fixture,
            SDL_Keycode.SDLK_KP_PLUS,
            repeated: true));
        Assert.False(Draft0LocalWalkingControls.TryAdjustSpeed(
            fixture,
            SDL_Keycode.SDLK_PLUS,
            repeated: false));
        Assert.True(Draft0LocalWalkingControls.TryAdjustSpeed(
            fixture,
            SDL_Keycode.SDLK_KP_PLUS,
            repeated: false));
        Assert.Equal(41, fixture.SpeedTenths);
        Assert.True(Draft0LocalWalkingControls.TryAdjustSpeed(
            fixture,
            SDL_Keycode.SDLK_KP_MINUS,
            repeated: false));
        Assert.Equal(40, fixture.SpeedTenths);
    }

    [Fact]
    public void PreviewTitleFormatsIntegerSpeedAndCameraDistanceInvariantly()
    {
        Assert.Equal(
            "Starfall - Draft 0 Local Graybox [player-fixture] " +
            "[speed 4.0 m/s] [camera 22.5 m]",
            Draft0LocalPreviewTitle.Create("player-fixture", 40, 22.5f));
    }

    [Fact]
    public void SpeedChangeRetainsDestinationAndAppliesOnTheNextTick()
    {
        var fixture = new Draft0LocalWalkingFixture(new GroundPoint(100.0f, 25.0f));
        var destination = new GroundPoint(110.0f, 25.0f);
        fixture.Submit(new GroundMovementIntent(destination));

        Assert.True(fixture.AdjustSpeedTenths(1));
        Assert.Equal(destination, fixture.Destination);
        AssertPoint(fixture.Snapshot.Position, 100.0f, 25.0f);

        fixture.AdvanceTick();
        AssertPoint(fixture.Snapshot.Position, 100.0f + (4.1f / 60.0f), 25.0f);
        AssertVector(fixture.Snapshot.VelocityMetresPerSecond, 4.1f, 0.0f);
    }

    [Fact]
    public void FixedTickAccumulatorClampsOneElapsedUpdateAndPreservesRemainder()
    {
        var accumulator = new FixedTickAccumulator();
        var ticks = 0;

        int first = accumulator.Advance(1.0, () => ticks++);
        int second = accumulator.Advance(1.0 / 120.0, () => ticks++);
        int third = accumulator.Advance(1.0 / 120.0, () => ticks++);

        Assert.Equal(15, first);
        Assert.Equal(0, second);
        Assert.Equal(1, third);
        Assert.Equal(16, ticks);
    }

    [Fact]
    public void AdapterMapsCardinalFacingAndLatestSnapshotWithoutSmoothing()
    {
        var position = new GroundPoint(12.0f, 34.0f);
        AssertFacing(Vector2.UnitY, Vector3.UnitZ);
        AssertFacing(Vector2.UnitX, Vector3.UnitX);
        AssertFacing(-Vector2.UnitY, -Vector3.UnitZ);
        AssertFacing(-Vector2.UnitX, -Vector3.UnitX);

        var snapshot = new TechnicalPlayerSnapshot(
            "player",
            7,
            position,
            new Vector2(0.0f, 6.0f),
            Vector2.UnitY);
        TechnicalPlayerPresentationState presentation = TechnicalPlayerPresentationAdapter.Adapt(snapshot);

        Assert.Equal(snapshot, presentation.Snapshot);
        Assert.Equal(position.Metres, presentation.World.Translation);
        Assert.Equal(TechnicalPlayerLocomotion.Walking, presentation.Locomotion);
    }

    [Fact]
    public void SnapshotRejectsMalformedPresentationFacts()
    {
        GroundPoint point = new(1.0f, 2.0f);
        Assert.Throws<ArgumentException>(() =>
            new TechnicalPlayerSnapshot("", 0, point, Vector2.Zero, Vector2.UnitY));
        Assert.Throws<ArgumentException>(() =>
            new TechnicalPlayerSnapshot("player", 0, point, new Vector2(float.NaN, 0.0f), Vector2.UnitY));
        Assert.Throws<ArgumentException>(() =>
            new TechnicalPlayerSnapshot("player", 0, point, Vector2.Zero, Vector2.Zero));
        Assert.Throws<ArgumentException>(() =>
            new TechnicalPlayerSnapshot("player", 0, point, Vector2.Zero, new Vector2(2.0f, 0.0f)));
    }

    [Fact]
    public void LocomotionPlaybackBlendsOnlyWhenStateChangesAndSupportsReversal()
    {
        SkeletonDefinition skeleton = CreateSkeleton();
        AnimationClip idle = CreateClip("Idle", skeleton, 0.0f);
        AnimationClip walk = CreateClip("Walk", skeleton, 10.0f);
        var playback = new TechnicalPlayerLocomotionPlayback(idle, walk);

        playback.SetLocomotion(TechnicalPlayerLocomotion.Walking);
        Assert.True(playback.IsBlending);
        Assert.Equal(0.0f, playback.CreatePose().LocalTransforms[0].Translation.X);

        playback.Advance(
            TechnicalPlayerLocomotionPlayback.BlendDurationSeconds * 0.5f,
            TechnicalPlayerLocomotionPlayback.WalkReferenceSpeedMetresPerSecond);
        Assert.Equal(0.5f, playback.BlendAmount, precision: 5);
        Assert.Equal(5.0f, playback.CreatePose().LocalTransforms[0].Translation.X, precision: 5);

        playback.SetLocomotion(TechnicalPlayerLocomotion.Walking);
        Assert.Equal(0.5f, playback.BlendAmount, precision: 5);

        playback.SetLocomotion(TechnicalPlayerLocomotion.Idle);
        Assert.Equal(0.0f, playback.BlendAmount);
        playback.Advance(
            TechnicalPlayerLocomotionPlayback.BlendDurationSeconds * 0.5f,
            planarSpeedMetresPerSecond: 0.0f);
        Assert.Equal(2.5f, playback.CreatePose().LocalTransforms[0].Translation.X, precision: 5);
        playback.Advance(
            TechnicalPlayerLocomotionPlayback.BlendDurationSeconds * 0.5f,
            planarSpeedMetresPerSecond: 0.0f);
        Assert.False(playback.IsBlending);
        Assert.Equal(0.0f, playback.CreatePose().LocalTransforms[0].Translation.X);
    }

    [Fact]
    public void LocomotionPlaybackScalesWalkSamplingFromPlanarSnapshotSpeed()
    {
        SkeletonDefinition skeleton = CreateSkeleton();
        AnimationClip idle = CreateClip("Idle", skeleton, 0.0f);
        AnimationClip walk = CreateLinearClip("Walk", skeleton, translationX: 10.0f);
        var playback = new TechnicalPlayerLocomotionPlayback(idle, walk);

        playback.SetLocomotion(TechnicalPlayerLocomotion.Walking);
        playback.Advance(
            TechnicalPlayerLocomotionPlayback.BlendDurationSeconds,
            TechnicalPlayerLocomotionPlayback.WalkReferenceSpeedMetresPerSecond);
        Assert.False(playback.IsBlending);
        Assert.Equal(1.0f, playback.PlaybackRate);
        Assert.Equal(2.5f, playback.CreatePose().LocalTransforms[0].Translation.X, precision: 5);

        playback.Advance(elapsedSeconds: 0.1, planarSpeedMetresPerSecond: 4.0f);
        Assert.Equal(2.0f, playback.PlaybackRate);
        Assert.Equal(4.5f, playback.CreatePose().LocalTransforms[0].Translation.X, precision: 5);

        playback.Advance(elapsedSeconds: 0.1, planarSpeedMetresPerSecond: 9.0f);
        Assert.Equal(3.0f, playback.PlaybackRate);
        Assert.Equal(7.5f, playback.CreatePose().LocalTransforms[0].Translation.X, precision: 5);

        playback.SetLocomotion(TechnicalPlayerLocomotion.Idle);
        playback.Advance(elapsedSeconds: 0.1, planarSpeedMetresPerSecond: 12.0f);
        Assert.Equal(1.0f, playback.PlaybackRate);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            playback.Advance(elapsedSeconds: 0.1, planarSpeedMetresPerSecond: float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            playback.Advance(elapsedSeconds: 0.1, planarSpeedMetresPerSecond: -0.1f));
    }

    private static Draft0LocalWalkingFixture CreateRedirectedFixture()
    {
        var fixture = new Draft0LocalWalkingFixture(new GroundPoint(100.0f, 25.0f));
        fixture.Submit(new GroundMovementIntent(new GroundPoint(110.0f, 25.0f)));
        for (var tick = 0; tick < 10; tick++)
            fixture.AdvanceTick();
        fixture.Submit(new GroundMovementIntent(new GroundPoint(101.0f, 35.0f)));
        for (var tick = 0; tick < 20; tick++)
            fixture.AdvanceTick();
        return fixture;
    }

    private static void AssertFacing(Vector2 facing, Vector3 expectedForward)
    {
        var snapshot = new TechnicalPlayerSnapshot(
            "player",
            0,
            new GroundPoint(12.0f, 34.0f),
            Vector2.Zero,
            facing);
        TechnicalPlayerPresentationState presentation = TechnicalPlayerPresentationAdapter.Adapt(snapshot);
        Vector3 transformedForward = Vector3.TransformNormal(Vector3.UnitZ, presentation.World);
        Assert.InRange(Vector3.Distance(expectedForward, transformedForward), 0.0f, Tolerance);
    }

    private static SkeletonDefinition CreateSkeleton() => new(
    [
        new SkeletonJoint("root", -1, JointTransform.Identity),
    ]);

    private static AnimationClip CreateClip(
        string name,
        SkeletonDefinition skeleton,
        float translationX)
    {
        var translations = new Vector3AnimationChannel(
        [
            new Vector3Keyframe(0.0f, new Vector3(translationX, 0.0f, 0.0f)),
            new Vector3Keyframe(1.0f, new Vector3(translationX, 0.0f, 0.0f)),
        ]);
        var rotations = new QuaternionAnimationChannel(
        [
            new QuaternionKeyframe(0.0f, Quaternion.Identity),
            new QuaternionKeyframe(1.0f, Quaternion.Identity),
        ]);
        var scales = new Vector3AnimationChannel(
        [
            new Vector3Keyframe(0.0f, Vector3.One),
            new Vector3Keyframe(1.0f, Vector3.One),
        ]);
        return new AnimationClip(
            name,
            skeleton,
            [new JointAnimationTrack(0, translations, rotations, scales)]);
    }

    private static AnimationClip CreateLinearClip(
        string name,
        SkeletonDefinition skeleton,
        float translationX)
    {
        var translations = new Vector3AnimationChannel(
        [
            new Vector3Keyframe(0.0f, Vector3.Zero),
            new Vector3Keyframe(1.0f, new Vector3(translationX, 0.0f, 0.0f)),
        ]);
        var rotations = new QuaternionAnimationChannel(
        [
            new QuaternionKeyframe(0.0f, Quaternion.Identity),
            new QuaternionKeyframe(1.0f, Quaternion.Identity),
        ]);
        var scales = new Vector3AnimationChannel(
        [
            new Vector3Keyframe(0.0f, Vector3.One),
            new Vector3Keyframe(1.0f, Vector3.One),
        ]);
        return new AnimationClip(
            name,
            skeleton,
            [new JointAnimationTrack(0, translations, rotations, scales)]);
    }

    private static void AssertPoint(GroundPoint actual, float expectedX, float expectedZ)
    {
        Assert.InRange(MathF.Abs(actual.XMetres - expectedX), 0.0f, Tolerance);
        Assert.InRange(MathF.Abs(actual.ZMetres - expectedZ), 0.0f, Tolerance);
    }

    private static void AssertVector(Vector2 actual, float expectedX, float expectedY)
    {
        Assert.InRange(MathF.Abs(actual.X - expectedX), 0.0f, Tolerance);
        Assert.InRange(MathF.Abs(actual.Y - expectedY), 0.0f, Tolerance);
    }
}
