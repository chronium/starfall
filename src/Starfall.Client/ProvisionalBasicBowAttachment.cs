using System.Numerics;
using ChronoFall.CharacterPresentation;

namespace Starfall.Client;

internal readonly record struct ProvisionalBasicBowFrame(
    SkeletonGlobalPose GlobalPose,
    Matrix4x4 SocketModelTransform,
    Matrix4x4 BowWorldTransform);

internal sealed class ProvisionalBasicBowAttachment
{
    internal const string JointName = "hand_l";
    internal const string SocketName = "basic-bow-left-hand";
    internal const float GripOffsetMetres = 0.09f;
    internal const float PalmDepthMetres = 0.03f;
    internal const float TwistDegrees = 80.0f;
    internal const float RollDegrees = -70.0f;

    private readonly SkeletonDefinition skeleton;
    private readonly SkeletonSocketSet sockets;
    private readonly JointTransform bowLocalTransform;

    internal ProvisionalBasicBowAttachment(SkeletonDefinition skeleton)
        : this(skeleton, DefaultBowLocalTransform)
    {
    }

    internal ProvisionalBasicBowAttachment(
        SkeletonDefinition skeleton,
        JointTransform bowLocalTransform)
    {
        ArgumentNullException.ThrowIfNull(skeleton);
        if (!skeleton.TryGetJointIndex(JointName, out int jointIndex))
            throw new InvalidOperationException($"Required Basic bow joint '{JointName}' was not found.");

        if (!IsFinite(bowLocalTransform.Translation) ||
            !IsFinite(bowLocalTransform.Scale) ||
            !IsFinite(bowLocalTransform.Rotation) ||
            MathF.Abs(bowLocalTransform.Rotation.LengthSquared() - 1.0f) > 1e-5f)
        {
            throw new ArgumentException(
                "The Basic bow-local transform must contain finite values and a normalized rotation.",
                nameof(bowLocalTransform));
        }
        if (bowLocalTransform.Scale != Vector3.One)
        {
            throw new ArgumentException(
                "The Basic bow-local transform must be rigid with identity scale.",
                nameof(bowLocalTransform));
        }

        this.skeleton = skeleton;
        this.bowLocalTransform = bowLocalTransform;
        sockets = new SkeletonSocketSet(
            skeleton,
            [new SkeletonSocketDefinition(SocketName, jointIndex, JointTransform.Identity)]);
    }

    internal static JointTransform DefaultBowLocalTransform => new(
        new Vector3(PalmDepthMetres, GripOffsetMetres, 0.0f),
        Quaternion.CreateFromRotationMatrix(
            Matrix4x4.CreateRotationY(TwistDegrees * MathF.PI / 180.0f) *
            Matrix4x4.CreateRotationX(RollDegrees * MathF.PI / 180.0f)),
        Vector3.One);

    internal int JointIndex => sockets.Sockets[0].JointIndex;

    internal JointTransform BowLocalTransform => bowLocalTransform;

    internal ProvisionalBasicBowFrame Evaluate(
        SkeletonGlobalPose globalPose,
        Matrix4x4 characterWorld)
    {
        ArgumentNullException.ThrowIfNull(globalPose);
        if (!ReferenceEquals(globalPose.Skeleton, skeleton))
        {
            throw new ArgumentException(
                "The Basic bow attachment requires its configured skeleton.",
                nameof(globalPose));
        }
        if (!Matrix4x4.Invert(characterWorld, out _))
            throw new ArgumentException("The character world transform must be invertible.", nameof(characterWorld));

        SkeletonSocketPose socketPose = SkeletonSocketEvaluator.EvaluateModelSpace(sockets, globalPose);
        if (!socketPose.TryGetModelTransform(SocketName, out Matrix4x4 socketModel))
            throw new InvalidOperationException("The Basic bow left-hand socket did not resolve.");

        Matrix4x4 bowWorld = bowLocalTransform.ToMatrix() * socketModel * characterWorld;
        return new ProvisionalBasicBowFrame(globalPose, socketModel, bowWorld);
    }

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    private static bool IsFinite(Quaternion value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) &&
        float.IsFinite(value.Z) && float.IsFinite(value.W);
}
