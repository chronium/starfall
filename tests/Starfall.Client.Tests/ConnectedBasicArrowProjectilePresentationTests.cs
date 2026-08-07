using System.Numerics;
using ChronoFall.CharacterPresentation;
using Starfall.Client.Networking;
using Starfall.Protocol.Combat;
using Starfall.Protocol.Movement;

namespace Starfall.Client.Tests;

public sealed class ConnectedBasicArrowProjectilePresentationTests
{
    [Fact]
    public void AcceptedActionNocksArrowWhileRejectedAndCanceledActionsLeaveNoStaleFrame()
    {
        var controller = new ConnectedBasicArrowProjectilePresentationController(CreateArrowMesh());
        Vector3 nock = new(1.0f, 2.0f, 3.0f);
        Vector3 target = new(1.0f, 2.0f, 13.0f);

        controller.HandleOutcome(Rejected(1), target);
        Assert.False(controller.TryCreateFrame(nock, out _));

        controller.HandleOutcome(Accepted(2), target);
        Assert.Equal(ConnectedBasicArrowProjectilePhase.Nocked, controller.Phase);
        Assert.True(controller.TryCreateFrame(nock, out BasicArrowProjectileFrame frame));
        Assert.Equal(2UL, frame.Sequence.Value);
        Assert.Equal(10UL, frame.TargetEntityId.Value);
        AssertDirection(Vector3.UnitZ, frame.World);

        controller.HandleOutcome(Canceled(2), target);
        Assert.Equal(ConnectedBasicArrowProjectilePhase.None, controller.Phase);
        Assert.False(controller.TryCreateFrame(nock, out _));
        Assert.False(controller.TryDequeueImpact(out _));
    }

    [Fact]
    public void MatchingResolvedReleaseUsesFrozenTargetAndExactFlightAndImpactDurations()
    {
        StaticMeshDefinition arrowMesh = CreateArrowMesh();
        var controller = new ConnectedBasicArrowProjectilePresentationController(arrowMesh);
        Vector3 nock = new(1.0f, 2.0f, 3.0f);
        Vector3 target = new(1.0f, 2.0f, 13.0f);
        controller.HandleOutcome(Accepted(7), target);
        controller.HandleOutcome(Resolved(7), target);

        controller.ObserveLiveTarget(new WorldEntityId(10), new Vector3(9.0f, 2.0f, 13.0f));
        Assert.False(controller.HandleRelease(Release(8), nock));
        Assert.True(controller.HandleRelease(Release(7), nock));
        Assert.Equal(ConnectedBasicArrowProjectilePhase.Flying, controller.Phase);

        controller.Advance(ConnectedBasicArrowProjectilePresentationController.FlightDurationSeconds / 2.0);
        Assert.True(controller.TryCreateFrame(nock, out BasicArrowProjectileFrame halfway));
        Assert.Equal(ConnectedBasicArrowProjectilePhase.Flying, halfway.Phase);
        AssertDirection(Vector3.UnitZ, halfway.World);
        Assert.False(controller.TryDequeueImpact(out _));

        controller.Advance(ConnectedBasicArrowProjectilePresentationController.FlightDurationSeconds / 2.0);
        Assert.Equal(ConnectedBasicArrowProjectilePhase.ImpactHold, controller.Phase);
        Assert.True(controller.TryDequeueImpact(out BasicArrowPresentationImpact impact));
        Assert.Equal(target, impact.WorldPoint);
        Assert.True(controller.TryCreateFrame(nock, out BasicArrowProjectileFrame impactFrame));
        float maximumZ = arrowMesh.Vertices.Max(static vertex => vertex.Position.Z);
        AssertVector(target, Vector3.Transform(new Vector3(0.0f, 0.0f, maximumZ), impactFrame.World));

        controller.Advance(ConnectedBasicArrowProjectilePresentationController.ImpactHoldDurationSeconds - 0.001);
        Assert.True(controller.TryCreateFrame(nock, out _));
        controller.Advance(0.001);
        Assert.Equal(ConnectedBasicArrowProjectilePhase.None, controller.Phase);
        Assert.False(controller.TryCreateFrame(nock, out _));
    }

    [Fact]
    public void ReleaseWithoutObservedTargetIsSuppressedAndMalformedInputsFailExplicitly()
    {
        var controller = new ConnectedBasicArrowProjectilePresentationController(CreateArrowMesh());
        controller.HandleOutcome(Accepted(1), liveTargetPoint: null);
        controller.HandleOutcome(Resolved(1), liveTargetPoint: null);

        Assert.False(controller.HandleRelease(Release(1), new Vector3(1.0f, 2.0f, 3.0f)));
        Assert.Equal(ConnectedBasicArrowProjectilePhase.None, controller.Phase);
        Assert.False(controller.TryCreateFrame(new Vector3(1.0f, 2.0f, 3.0f), out _));
        Assert.Throws<ArgumentException>(() => controller.ObserveLiveTarget(
            new WorldEntityId(10),
            new Vector3(float.NaN, 0.0f, 0.0f)));
        Assert.Throws<ArgumentOutOfRangeException>(() => controller.Advance(-0.1));
    }

    [Fact]
    public void RightHandNockUsesTheEvaluatedGlobalPoseAndCharacterWorldTransform()
    {
        var skeleton = new SkeletonDefinition([
            new SkeletonJoint("root", -1, JointTransform.Identity),
            new SkeletonJoint(
                ProvisionalBasicArrowNockAttachment.JointName,
                0,
                new JointTransform(new Vector3(0.2f, 1.0f, 0.3f), Quaternion.Identity, Vector3.One)),
        ]);
        var attachment = new ProvisionalBasicArrowNockAttachment(skeleton);
        var pose = new SkeletonPose(skeleton, skeleton.Joints.Select(static joint => joint.LocalBindTransform));
        SkeletonGlobalPose global = SkeletonPoseEvaluator.EvaluateGlobal(pose);

        Vector3 worldPoint = attachment.EvaluateWorldPoint(
            global,
            Matrix4x4.CreateTranslation(4.0f, 0.0f, 6.0f));

        AssertVector(new Vector3(4.2f, 1.0f, 6.3f), worldPoint);
    }

    private static StaticMeshDefinition CreateArrowMesh() => new(
        "test-basic-arrow",
        [
            new StaticVertex(new Vector3(-0.02f, 0.0f, -0.01f), Vector3.UnitY),
            new StaticVertex(new Vector3(0.02f, 0.0f, -0.01f), Vector3.UnitY),
            new StaticVertex(new Vector3(0.0f, 0.0f, 0.67f), Vector3.UnitY),
        ],
        [0U, 1U, 2U],
        [new StaticMeshSection("test", 0, 3)]);

    private static ConnectedBasicArrowOutcome Accepted(ulong sequence) =>
        ConnectedBasicArrowOutcome.Accepted(new BasicArrowAccepted(
            new CombatCommandSequence(sequence),
            new WorldEntityId(1),
            new WorldEntityId(10),
            5,
            17));

    private static ConnectedBasicArrowOutcome Rejected(ulong sequence) =>
        ConnectedBasicArrowOutcome.Rejected(new BasicArrowRejected(
            new CombatCommandSequence(sequence),
            new WorldEntityId(1),
            new WorldEntityId(10),
            5,
            BasicArrowRejectionReason.ActionAlreadyPending));

    private static ConnectedBasicArrowOutcome Canceled(ulong sequence) =>
        ConnectedBasicArrowOutcome.Canceled(new BasicArrowCanceled(
            new CombatCommandSequence(sequence),
            new WorldEntityId(1),
            new WorldEntityId(10),
            5,
            17,
            6,
            BasicArrowCancellationReason.CanceledByMovement));

    private static ConnectedBasicArrowOutcome Resolved(ulong sequence) =>
        ConnectedBasicArrowOutcome.Resolved(new BasicArrowResolved(
            new CombatCommandSequence(sequence),
            new WorldEntityId(1),
            new WorldEntityId(10),
            5,
            17,
            300,
            300,
            false));

    private static BasicArrowPresentationReleaseMarker Release(ulong sequence) => new(
        new CombatCommandSequence(sequence),
        new WorldEntityId(1),
        new WorldEntityId(10),
        ConnectedBasicArrowBodyPresentationController.ReleaseSampleTimeSeconds,
        ConnectedBasicArrowBodyPresentationController.ReleaseSampleFrame);

    private static void AssertDirection(Vector3 expected, Matrix4x4 world)
    {
        Vector3 actual = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, world));
        AssertVector(expected, actual);
    }

    private static void AssertVector(Vector3 expected, Vector3 actual)
    {
        Assert.InRange(MathF.Abs(expected.X - actual.X), 0.0f, 1e-5f);
        Assert.InRange(MathF.Abs(expected.Y - actual.Y), 0.0f, 1e-5f);
        Assert.InRange(MathF.Abs(expected.Z - actual.Z), 0.0f, 1e-5f);
    }
}
