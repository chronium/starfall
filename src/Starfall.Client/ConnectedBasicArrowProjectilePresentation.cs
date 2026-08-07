using System.Numerics;
using ChronoFall.CharacterPresentation;
using Starfall.Client.Networking;
using Starfall.Protocol.Combat;
using Starfall.Protocol.Movement;

namespace Starfall.Client;

internal enum ConnectedBasicArrowProjectilePhase
{
    None,
    Nocked,
    Flying,
    ImpactHold,
}

internal readonly record struct BasicArrowProjectileFrame(
    ConnectedBasicArrowProjectilePhase Phase,
    CombatCommandSequence Sequence,
    WorldEntityId TargetEntityId,
    Matrix4x4 World);

internal readonly record struct BasicArrowPresentationImpact(
    CombatCommandSequence Sequence,
    WorldEntityId TargetEntityId,
    Vector3 WorldPoint);

internal sealed class ProvisionalBasicArrowNockAttachment
{
    internal const string JointName = "hand_r";
    internal const string SocketName = "basic-arrow-nock-right-hand";

    private readonly SkeletonDefinition skeleton;
    private readonly SkeletonSocketSet sockets;

    internal ProvisionalBasicArrowNockAttachment(SkeletonDefinition skeleton)
    {
        ArgumentNullException.ThrowIfNull(skeleton);
        if (!skeleton.TryGetJointIndex(JointName, out int jointIndex))
            throw new InvalidOperationException($"Required Basic Arrow nock joint '{JointName}' was not found.");

        this.skeleton = skeleton;
        sockets = new SkeletonSocketSet(
            skeleton,
            [new SkeletonSocketDefinition(SocketName, jointIndex, JointTransform.Identity)]);
    }

    internal Vector3 EvaluateWorldPoint(SkeletonGlobalPose globalPose, Matrix4x4 characterWorld)
    {
        ArgumentNullException.ThrowIfNull(globalPose);
        if (!ReferenceEquals(globalPose.Skeleton, skeleton))
        {
            throw new ArgumentException(
                "The Basic Arrow nock attachment requires its configured skeleton.",
                nameof(globalPose));
        }
        if (!Matrix4x4.Invert(characterWorld, out _))
            throw new ArgumentException("The character world transform must be invertible.", nameof(characterWorld));

        SkeletonSocketPose socketPose = SkeletonSocketEvaluator.EvaluateModelSpace(sockets, globalPose);
        if (!socketPose.TryGetModelTransform(SocketName, out Matrix4x4 socketModel))
            throw new InvalidOperationException("The Basic Arrow right-hand nock socket did not resolve.");

        return Vector3.Transform(Vector3.Zero, socketModel * characterWorld);
    }
}

internal sealed class ConnectedBasicArrowProjectilePresentationController
{
    private const float DirectionLengthToleranceSquared = 1e-8f;
    internal const double FlightDurationSeconds = 0.15;
    internal const double ImpactHoldDurationSeconds = 0.08;
    internal static readonly Vector3 ArrowColor = new(0.95f, 0.78f, 0.20f);

    private readonly float arrowMinimumZ;
    private readonly float arrowMaximumZ;
    private readonly Queue<BasicArrowPresentationImpact> impacts = [];
    private CombatCommandSequence activeSequence;
    private WorldEntityId activeTarget;
    private Vector3? observedTargetPoint;
    private Vector3? frozenTargetPoint;
    private Matrix4x4 flightRotation;
    private Vector3 flightStartTranslation;
    private Vector3 flightEndTranslation;
    private double phaseSeconds;
    private bool resolved;

    internal ConnectedBasicArrowProjectilePresentationController(StaticMeshDefinition arrowMesh)
    {
        ArgumentNullException.ThrowIfNull(arrowMesh);
        arrowMinimumZ = arrowMesh.Vertices.Min(static vertex => vertex.Position.Z);
        arrowMaximumZ = arrowMesh.Vertices.Max(static vertex => vertex.Position.Z);
        if (!float.IsFinite(arrowMinimumZ) || !float.IsFinite(arrowMaximumZ) ||
            arrowMaximumZ <= arrowMinimumZ)
        {
            throw new InvalidDataException("The Basic Arrow mesh must have a positive finite local +Z extent.");
        }
    }

    internal ConnectedBasicArrowProjectilePhase Phase { get; private set; }

    internal CombatCommandSequence? ActiveSequence =>
        Phase == ConnectedBasicArrowProjectilePhase.None ? null : activeSequence;

    internal WorldEntityId? ActiveTarget =>
        Phase == ConnectedBasicArrowProjectilePhase.None ? null : activeTarget;

    internal float ArrowLengthMetres => arrowMaximumZ - arrowMinimumZ;

    internal void ObserveLiveTarget(WorldEntityId targetEntityId, Vector3 worldPoint)
    {
        ValidatePoint(worldPoint, nameof(worldPoint));
        if (Phase == ConnectedBasicArrowProjectilePhase.Nocked &&
            !resolved &&
            activeTarget == targetEntityId)
        {
            observedTargetPoint = worldPoint;
        }
    }

    internal void HandleOutcome(ConnectedBasicArrowOutcome outcome, Vector3? liveTargetPoint)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        if (liveTargetPoint is { } point)
            ValidatePoint(point, nameof(liveTargetPoint));

        switch (outcome.Kind)
        {
            case ConnectedBasicArrowOutcomeKind.Accepted:
                StartNocked(outcome, liveTargetPoint);
                break;
            case ConnectedBasicArrowOutcomeKind.Resolved when IsActive(outcome.Sequence):
                if (liveTargetPoint is { } resolvedPoint)
                    observedTargetPoint = resolvedPoint;
                frozenTargetPoint = observedTargetPoint;
                resolved = true;
                break;
            case ConnectedBasicArrowOutcomeKind.Canceled when IsActive(outcome.Sequence):
                Clear();
                break;
            case ConnectedBasicArrowOutcomeKind.Rejected:
            case ConnectedBasicArrowOutcomeKind.Canceled:
            case ConnectedBasicArrowOutcomeKind.Resolved:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), "Unknown Basic Arrow outcome kind.");
        }
    }

    internal bool HandleRelease(BasicArrowPresentationReleaseMarker marker, Vector3 nockWorldPoint)
    {
        ValidatePoint(nockWorldPoint, nameof(nockWorldPoint));
        if (Phase != ConnectedBasicArrowProjectilePhase.Nocked ||
            !resolved ||
            marker.Sequence != activeSequence ||
            marker.TargetEntityId != activeTarget)
        {
            return false;
        }
        if (frozenTargetPoint is not { } target)
        {
            Clear();
            return false;
        }

        Vector3 direction = target - nockWorldPoint;
        if (!IsFinite(direction) || direction.LengthSquared() <= DirectionLengthToleranceSquared)
        {
            Clear();
            return false;
        }

        direction = Vector3.Normalize(direction);
        flightRotation = CreatePositiveZRotation(direction);
        flightStartTranslation = nockWorldPoint - (direction * arrowMinimumZ);
        flightEndTranslation = target - (direction * arrowMaximumZ);
        phaseSeconds = 0.0;
        Phase = ConnectedBasicArrowProjectilePhase.Flying;
        return true;
    }

    internal void Advance(double elapsedSeconds)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0.0)
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        if (Phase is ConnectedBasicArrowProjectilePhase.None or ConnectedBasicArrowProjectilePhase.Nocked ||
            elapsedSeconds == 0.0)
        {
            return;
        }

        phaseSeconds += elapsedSeconds;
        if (Phase == ConnectedBasicArrowProjectilePhase.Flying && phaseSeconds >= FlightDurationSeconds)
        {
            phaseSeconds -= FlightDurationSeconds;
            Phase = ConnectedBasicArrowProjectilePhase.ImpactHold;
            impacts.Enqueue(new BasicArrowPresentationImpact(
                activeSequence,
                activeTarget,
                frozenTargetPoint!.Value));
        }
        if (Phase == ConnectedBasicArrowProjectilePhase.ImpactHold &&
            phaseSeconds >= ImpactHoldDurationSeconds)
        {
            Clear();
        }
    }

    internal bool TryCreateFrame(Vector3 nockWorldPoint, out BasicArrowProjectileFrame frame)
    {
        ValidatePoint(nockWorldPoint, nameof(nockWorldPoint));
        if (Phase == ConnectedBasicArrowProjectilePhase.None)
        {
            frame = default;
            return false;
        }

        Matrix4x4 world;
        if (Phase == ConnectedBasicArrowProjectilePhase.Nocked)
        {
            Vector3? target = resolved ? frozenTargetPoint : observedTargetPoint;
            if (target is not { } targetPoint)
            {
                frame = default;
                return false;
            }

            Vector3 direction = targetPoint - nockWorldPoint;
            if (!IsFinite(direction) || direction.LengthSquared() <= DirectionLengthToleranceSquared)
            {
                frame = default;
                return false;
            }
            direction = Vector3.Normalize(direction);
            world = CreatePositiveZRotation(direction) *
                Matrix4x4.CreateTranslation(nockWorldPoint - (direction * arrowMinimumZ));
        }
        else
        {
            float progress = Phase == ConnectedBasicArrowProjectilePhase.Flying
                ? Math.Clamp((float)(phaseSeconds / FlightDurationSeconds), 0.0f, 1.0f)
                : 1.0f;
            world = flightRotation * Matrix4x4.CreateTranslation(
                Vector3.Lerp(flightStartTranslation, flightEndTranslation, progress));
        }

        frame = new BasicArrowProjectileFrame(Phase, activeSequence, activeTarget, world);
        return true;
    }

    internal bool TryDequeueImpact(out BasicArrowPresentationImpact impact) => impacts.TryDequeue(out impact);

    private void StartNocked(ConnectedBasicArrowOutcome outcome, Vector3? liveTargetPoint)
    {
        activeSequence = outcome.Sequence;
        activeTarget = outcome.TargetEntityId;
        observedTargetPoint = liveTargetPoint;
        frozenTargetPoint = null;
        resolved = false;
        phaseSeconds = 0.0;
        Phase = ConnectedBasicArrowProjectilePhase.Nocked;
    }

    private bool IsActive(CombatCommandSequence sequence) =>
        Phase != ConnectedBasicArrowProjectilePhase.None && activeSequence == sequence;

    private void Clear()
    {
        Phase = ConnectedBasicArrowProjectilePhase.None;
        activeSequence = default;
        activeTarget = default;
        observedTargetPoint = null;
        frozenTargetPoint = null;
        resolved = false;
        phaseSeconds = 0.0;
        flightRotation = default;
        flightStartTranslation = default;
        flightEndTranslation = default;
    }

    private static Matrix4x4 CreatePositiveZRotation(Vector3 direction)
    {
        Vector3 upReference = MathF.Abs(Vector3.Dot(direction, Vector3.UnitY)) < 0.99f
            ? Vector3.UnitY
            : Vector3.UnitX;
        Vector3 right = Vector3.Normalize(Vector3.Cross(upReference, direction));
        Vector3 up = Vector3.Normalize(Vector3.Cross(direction, right));
        return new Matrix4x4(
            right.X, right.Y, right.Z, 0.0f,
            up.X, up.Y, up.Z, 0.0f,
            direction.X, direction.Y, direction.Z, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f);
    }

    private static void ValidatePoint(Vector3 value, string parameterName)
    {
        if (!IsFinite(value))
            throw new ArgumentException("Basic Arrow presentation points must be finite.", parameterName);
    }

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
