using System.Numerics;
using ChronoFall.CharacterPresentation;

namespace Starfall.Client.Tests;

public sealed class ProvisionalBasicBowAttachmentTests
{
    [Fact]
    public void DefaultTransformFreezesOwnerValidatedStarfallPlacement()
    {
        Assert.Equal("basic-bow-left-hand", ProvisionalBasicBowAttachment.SocketName);
        Assert.Equal("hand_l", ProvisionalBasicBowAttachment.JointName);
        Assert.Equal(0.09f, ProvisionalBasicBowAttachment.GripOffsetMetres);
        Assert.Equal(0.03f, ProvisionalBasicBowAttachment.PalmDepthMetres);
        Assert.Equal(80.0f, ProvisionalBasicBowAttachment.TwistDegrees);
        Assert.Equal(-70.0f, ProvisionalBasicBowAttachment.RollDegrees);

        JointTransform transform = ProvisionalBasicBowAttachment.DefaultBowLocalTransform;
        Assert.Equal(new Vector3(0.03f, 0.09f, 0.0f), transform.Translation);
        Assert.Equal(Vector3.One, transform.Scale);
        Assert.InRange(MathF.Abs(transform.Rotation.LengthSquared() - 1.0f), 0.0f, 1e-6f);
    }

    [Fact]
    public void EvaluatesBowLocalSocketAndCharacterWorldInRowVectorOrder()
    {
        SkeletonDefinition skeleton = CreateSkeleton();
        var bowLocal = new JointTransform(
            new Vector3(0.1f, 0.2f, 0.3f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.25f),
            Vector3.One);
        var attachment = new ProvisionalBasicBowAttachment(skeleton, bowLocal);
        SkeletonPose pose = new(
            skeleton,
            [
                JointTransform.Identity,
                new JointTransform(
                    new Vector3(1.0f, 2.0f, 3.0f),
                    Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.5f),
                    Vector3.One),
            ]);
        SkeletonGlobalPose global = SkeletonPoseEvaluator.EvaluateGlobal(pose);
        Matrix4x4 world =
            Matrix4x4.CreateRotationY(-0.3f) *
            Matrix4x4.CreateTranslation(4.0f, 0.0f, -2.0f);

        ProvisionalBasicBowFrame result = attachment.Evaluate(global, world);

        Assert.Equal(1, attachment.JointIndex);
        Assert.Same(global, result.GlobalPose);
        Assert.Equal(global.GlobalTransforms[1], result.SocketModelTransform);
        AssertMatrixEqual(
            bowLocal.ToMatrix() * global.GlobalTransforms[1] * world,
            result.BowWorldTransform);
    }

    [Fact]
    public void PosedHandMotionChangesBowTransformAndRepeatedEvaluationIsExact()
    {
        SkeletonDefinition skeleton = CreateSkeleton();
        var attachment = new ProvisionalBasicBowAttachment(skeleton);
        SkeletonGlobalPose first = SkeletonPoseEvaluator.EvaluateGlobal(
            new SkeletonPose(skeleton, [JointTransform.Identity, JointTransform.Identity]));
        SkeletonGlobalPose second = SkeletonPoseEvaluator.EvaluateGlobal(
            new SkeletonPose(
                skeleton,
                [
                    JointTransform.Identity,
                    new JointTransform(
                        new Vector3(0.5f, 0.25f, -0.1f),
                        Quaternion.Identity,
                        Vector3.One),
                ]));

        ProvisionalBasicBowFrame firstResult = attachment.Evaluate(first, Matrix4x4.Identity);
        ProvisionalBasicBowFrame repeated = attachment.Evaluate(first, Matrix4x4.Identity);
        ProvisionalBasicBowFrame secondResult = attachment.Evaluate(second, Matrix4x4.Identity);

        Assert.Equal(firstResult.BowWorldTransform, repeated.BowWorldTransform);
        Assert.NotEqual(firstResult.BowWorldTransform, secondResult.BowWorldTransform);
    }

    [Fact]
    public void RejectsMissingJointInvalidTransformMismatchedSkeletonAndWorld()
    {
        SkeletonDefinition missing = new(
            [new SkeletonJoint("root", -1, JointTransform.Identity)]);
        InvalidOperationException missingException = Assert.Throws<InvalidOperationException>(
            () => new ProvisionalBasicBowAttachment(missing));
        Assert.Contains("hand_l", missingException.Message, StringComparison.Ordinal);

        SkeletonDefinition skeleton = CreateSkeleton();
        Assert.Throws<ArgumentException>(
            () => new ProvisionalBasicBowAttachment(
                skeleton,
                new JointTransform(Vector3.Zero, Quaternion.Identity, new Vector3(2.0f))));
        Assert.Throws<ArgumentException>(() => new ProvisionalBasicBowAttachment(skeleton, default));

        var attachment = new ProvisionalBasicBowAttachment(skeleton);
        SkeletonDefinition other = CreateSkeleton();
        SkeletonGlobalPose otherPose = SkeletonPoseEvaluator.EvaluateGlobal(other.CreateBindPose());
        Assert.Throws<ArgumentException>(() => attachment.Evaluate(otherPose, Matrix4x4.Identity));

        SkeletonGlobalPose pose = SkeletonPoseEvaluator.EvaluateGlobal(skeleton.CreateBindPose());
        Assert.Throws<ArgumentException>(() => attachment.Evaluate(pose, new Matrix4x4()));
    }

    private static SkeletonDefinition CreateSkeleton() =>
        new(
            [
                new SkeletonJoint("root", -1, JointTransform.Identity),
                new SkeletonJoint("hand_l", 0, JointTransform.Identity),
            ]);

    private static void AssertMatrixEqual(Matrix4x4 expected, Matrix4x4 actual)
    {
        Assert.Equal(expected.M11, actual.M11);
        Assert.Equal(expected.M12, actual.M12);
        Assert.Equal(expected.M13, actual.M13);
        Assert.Equal(expected.M14, actual.M14);
        Assert.Equal(expected.M21, actual.M21);
        Assert.Equal(expected.M22, actual.M22);
        Assert.Equal(expected.M23, actual.M23);
        Assert.Equal(expected.M24, actual.M24);
        Assert.Equal(expected.M31, actual.M31);
        Assert.Equal(expected.M32, actual.M32);
        Assert.Equal(expected.M33, actual.M33);
        Assert.Equal(expected.M34, actual.M34);
        Assert.Equal(expected.M41, actual.M41);
        Assert.Equal(expected.M42, actual.M42);
        Assert.Equal(expected.M43, actual.M43);
        Assert.Equal(expected.M44, actual.M44);
    }
}
