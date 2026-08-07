using ChronoFall.CharacterPresentation;
using ChronoFall.CharacterPresentation.Cooking;
using Starfall.Client.Networking;
using Starfall.Protocol.Combat;
using Starfall.Protocol.Movement;

namespace Starfall.Client;

internal static class BasicArrowBowAnimationSet
{
    internal static AnimationClip[] BindExact(
        CookedSkeletalCharacterAsset source,
        SkinDefinition targetSkin,
        IReadOnlyList<string> expectedClipNames)
    {
        ArgumentNullException.ThrowIfNull(source);
        return BindExact(
            source.Asset.Mesh.Skin,
            source.Asset.Animations,
            targetSkin,
            expectedClipNames);
    }

    internal static AnimationClip[] BindExact(
        SkinDefinition sourceSkin,
        IReadOnlyList<AnimationClip> sourceAnimations,
        SkinDefinition targetSkin,
        IReadOnlyList<string> expectedClipNames)
    {
        ArgumentNullException.ThrowIfNull(sourceSkin);
        ArgumentNullException.ThrowIfNull(sourceAnimations);
        ArgumentNullException.ThrowIfNull(targetSkin);
        ArgumentNullException.ThrowIfNull(expectedClipNames);
        if (expectedClipNames.Count == 0)
            throw new ArgumentException("At least one selected bow-body clip is required.", nameof(expectedClipNames));

        string[] expected = expectedClipNames.Order(StringComparer.Ordinal).ToArray();
        string[] actual = sourceAnimations
            .Select(static clip => clip.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                $"Expected selected bow-body clips [{string.Join(",", expected)}], " +
                $"received [{string.Join(",", actual)}].");
        }

        ValidateExactSkeleton(sourceSkin, targetSkin);
        return expectedClipNames
            .Select(name => sourceAnimations.Single(
                clip => string.Equals(clip.Name, name, StringComparison.Ordinal)))
            .Select(clip =>
            {
                if (!ReferenceEquals(clip.Skeleton, sourceSkin.Skeleton))
                {
                    throw new InvalidDataException(
                        $"Selected bow-body clip '{clip.Name}' does not use its cooked source skeleton.");
                }
                return new AnimationClip(clip.Name, targetSkin.Skeleton, clip.Tracks);
            })
            .ToArray();
    }

    private static void ValidateExactSkeleton(SkinDefinition sourceSkin, SkinDefinition targetSkin)
    {
        SkeletonDefinition source = sourceSkin.Skeleton;
        SkeletonDefinition target = targetSkin.Skeleton;
        if (source.JointCount != target.JointCount)
        {
            throw new InvalidDataException(
                $"Exact bow-body binding requires {target.JointCount} joints, received {source.JointCount}.");
        }

        for (int index = 0; index < source.JointCount; index++)
        {
            SkeletonJoint sourceJoint = source.Joints[index];
            SkeletonJoint targetJoint = target.Joints[index];
            if (!string.Equals(sourceJoint.Name, targetJoint.Name, StringComparison.Ordinal) ||
                sourceJoint.ParentIndex != targetJoint.ParentIndex ||
                sourceJoint.LocalBindTransform != targetJoint.LocalBindTransform ||
                sourceSkin.InverseBindMatrices[index] != targetSkin.InverseBindMatrices[index])
            {
                throw new InvalidDataException(
                    $"Exact bow-body skeleton mismatch at joint {index} " +
                    $"('{sourceJoint.Name}'/'{targetJoint.Name}').");
            }
        }
    }
}

internal enum ConnectedBasicArrowBodyPhase
{
    Locomotion,
    Notch,
    AimTransition,
    AimHold,
    Shoot,
    Recovery,
}

internal readonly record struct BasicArrowPresentationReleaseMarker(
    CombatCommandSequence Sequence,
    WorldEntityId ActorEntityId,
    WorldEntityId TargetEntityId,
    float ShootSampleTime,
    int ShootSampleFrame);

internal sealed class ConnectedBasicArrowBodyPresentationController
{
    private const double TimingComparisonToleranceSeconds = 1e-9;
    internal const int TickRate = 60;
    internal const float NotchWindupFraction = 0.75f;
    internal const float RecoveryDurationSeconds = 0.15f;
    internal const float ReleaseSampleTimeSeconds = 0.1f;
    internal const int ReleaseSampleFrame = 3;
    internal const string UpperBodyRootJoint = "spine_01";
    internal const int ExpectedUpperBodyJointCount = 53;

    private readonly AnimationClip notch;
    private readonly AnimationClip aim;
    private readonly AnimationClip shoot;
    private readonly SkeletonJointMask upperBodyMask;
    private readonly Queue<BasicArrowPresentationReleaseMarker> releaseMarkers = [];
    private SkeletonPose? entryPose;
    private SkeletonPose? recoveryPose;
    private CombatCommandSequence activeSequence;
    private WorldEntityId activeActor;
    private WorldEntityId activeTarget;
    private double windupDuration;
    private double phaseTime;
    private bool resolutionReceived;
    private bool releaseEmitted;

    internal ConnectedBasicArrowBodyPresentationController(
        AnimationClip notch,
        AnimationClip aim,
        AnimationClip shoot)
    {
        this.notch = RequireClip(notch, "Bow_Notch");
        this.aim = RequireClip(aim, "Bow_Aim_Neutral");
        this.shoot = RequireClip(shoot, "Bow_Shoot");
        if (!ReferenceEquals(this.notch.Skeleton, this.aim.Skeleton) ||
            !ReferenceEquals(this.notch.Skeleton, this.shoot.Skeleton))
        {
            throw new ArgumentException("Basic Arrow body clips must use the same skeleton instance.", nameof(aim));
        }
        if (!this.notch.Skeleton.TryGetJointIndex(UpperBodyRootJoint, out int spineIndex))
            throw new InvalidDataException($"Basic Arrow body skeleton has no '{UpperBodyRootJoint}' joint.");
        upperBodyMask = SkeletonJointMask.CreateSubtree(this.notch.Skeleton, spineIndex);
        if (upperBodyMask.IncludedJointCount != ExpectedUpperBodyJointCount)
        {
            throw new InvalidDataException(
                $"Expected {ExpectedUpperBodyJointCount} upper-body joints, " +
                $"received {upperBodyMask.IncludedJointCount}.");
        }
    }

    internal ConnectedBasicArrowBodyPhase Phase { get; private set; }

    internal CombatCommandSequence? ActiveSequence =>
        Phase == ConnectedBasicArrowBodyPhase.Locomotion ? null : activeSequence;

    internal float CurrentSampleTime => Phase switch
    {
        ConnectedBasicArrowBodyPhase.Notch => GetNotchSampleTime(),
        ConnectedBasicArrowBodyPhase.AimTransition or ConnectedBasicArrowBodyPhase.AimHold =>
            (float)Math.Max(0.0, phaseTime),
        ConnectedBasicArrowBodyPhase.Shoot => (float)Math.Min(phaseTime, shoot.Duration),
        _ => 0.0f,
    };

    internal void HandleOutcome(ConnectedBasicArrowOutcome outcome, SkeletonPose basePose)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ValidateBasePose(basePose);

        switch (outcome.Kind)
        {
            case ConnectedBasicArrowOutcomeKind.Accepted:
                StartAccepted(outcome, basePose);
                break;
            case ConnectedBasicArrowOutcomeKind.Resolved when IsActive(outcome.Sequence):
                resolutionReceived = true;
                if (Phase == ConnectedBasicArrowBodyPhase.AimHold)
                    StartShoot();
                break;
            case ConnectedBasicArrowOutcomeKind.Canceled when IsActive(outcome.Sequence):
                StartRecovery(CreatePose(basePose));
                break;
            case ConnectedBasicArrowOutcomeKind.Rejected:
            case ConnectedBasicArrowOutcomeKind.Canceled:
            case ConnectedBasicArrowOutcomeKind.Resolved:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), "Unknown Basic Arrow outcome kind.");
        }
    }

    internal void Advance(double elapsedSeconds, SkeletonPose basePose)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0.0)
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        ValidateBasePose(basePose);
        if (Phase == ConnectedBasicArrowBodyPhase.Locomotion || elapsedSeconds == 0.0)
            return;

        phaseTime += elapsedSeconds;
        switch (Phase)
        {
            case ConnectedBasicArrowBodyPhase.Notch:
                if (phaseTime + TimingComparisonToleranceSeconds >=
                    windupDuration * NotchWindupFraction)
                    Phase = ConnectedBasicArrowBodyPhase.AimTransition;
                if (phaseTime + TimingComparisonToleranceSeconds >= windupDuration)
                    CompleteWindup();
                break;
            case ConnectedBasicArrowBodyPhase.AimTransition:
                if (phaseTime + TimingComparisonToleranceSeconds >= windupDuration)
                    CompleteWindup();
                break;
            case ConnectedBasicArrowBodyPhase.AimHold:
                if (resolutionReceived)
                    StartShoot();
                break;
            case ConnectedBasicArrowBodyPhase.Shoot:
                if (!releaseEmitted && phaseTime >= ReleaseSampleTimeSeconds)
                {
                    releaseEmitted = true;
                    releaseMarkers.Enqueue(new BasicArrowPresentationReleaseMarker(
                        activeSequence,
                        activeActor,
                        activeTarget,
                        ReleaseSampleTimeSeconds,
                        ReleaseSampleFrame));
                }
                if (phaseTime >= shoot.Duration)
                {
                    SkeletonPose finalShoot = AnimationSampler.Sample(
                        shoot,
                        shoot.Duration,
                        AnimationPlaybackMode.Clamp);
                    StartRecovery(SkeletonPoseLayerer.Apply(basePose, finalShoot, upperBodyMask, 1.0f));
                }
                break;
            case ConnectedBasicArrowBodyPhase.Recovery:
                if (phaseTime >= RecoveryDurationSeconds)
                    ReturnToLocomotion();
                break;
            default:
                throw new InvalidOperationException($"Cannot advance Basic Arrow body phase {Phase}.");
        }
    }

    internal SkeletonPose CreatePose(SkeletonPose basePose)
    {
        ValidateBasePose(basePose);
        return Phase switch
        {
            ConnectedBasicArrowBodyPhase.Locomotion => basePose,
            ConnectedBasicArrowBodyPhase.Notch => SkeletonPoseLayerer.Apply(
                basePose,
                SkeletonPoseBlender.Blend(
                    entryPose!,
                    AnimationSampler.Sample(notch, GetNotchSampleTime(), AnimationPlaybackMode.Clamp),
                    GetNotchProgress()),
                upperBodyMask,
                1.0f),
            ConnectedBasicArrowBodyPhase.AimTransition => SkeletonPoseLayerer.Apply(
                basePose,
                SkeletonPoseBlender.Blend(
                    AnimationSampler.Sample(notch, notch.Duration, AnimationPlaybackMode.Clamp),
                    SampleAim(),
                    GetAimTransitionProgress()),
                upperBodyMask,
                1.0f),
            ConnectedBasicArrowBodyPhase.AimHold =>
                SkeletonPoseLayerer.Apply(basePose, SampleAim(), upperBodyMask, 1.0f),
            ConnectedBasicArrowBodyPhase.Shoot => SkeletonPoseLayerer.Apply(
                basePose,
                AnimationSampler.Sample(
                    shoot,
                    (float)Math.Min(phaseTime, shoot.Duration),
                    AnimationPlaybackMode.Clamp),
                upperBodyMask,
                1.0f),
            ConnectedBasicArrowBodyPhase.Recovery => SkeletonPoseLayerer.Apply(
                basePose,
                recoveryPose!,
                upperBodyMask,
                1.0f - Math.Clamp((float)(phaseTime / RecoveryDurationSeconds), 0.0f, 1.0f)),
            _ => throw new InvalidOperationException($"Unknown Basic Arrow body phase {Phase}."),
        };
    }

    internal bool TryDequeueReleaseMarker(out BasicArrowPresentationReleaseMarker marker) =>
        releaseMarkers.TryDequeue(out marker);

    private void StartAccepted(ConnectedBasicArrowOutcome outcome, SkeletonPose basePose)
    {
        if (outcome.ResolveTick <= outcome.StartTick)
            throw new ArgumentException("Accepted Basic Arrow timing must have a positive windup.", nameof(outcome));
        SkeletonPose currentPose = CreatePose(basePose);
        activeSequence = outcome.Sequence;
        activeActor = outcome.ActorEntityId;
        activeTarget = outcome.TargetEntityId;
        windupDuration = (outcome.ResolveTick - outcome.StartTick) / (double)TickRate;
        phaseTime = 0.0;
        resolutionReceived = false;
        releaseEmitted = false;
        entryPose = currentPose;
        recoveryPose = null;
        Phase = ConnectedBasicArrowBodyPhase.Notch;
    }

    private void CompleteWindup()
    {
        Phase = ConnectedBasicArrowBodyPhase.AimHold;
        if (resolutionReceived)
            StartShoot();
    }

    private void StartShoot()
    {
        Phase = ConnectedBasicArrowBodyPhase.Shoot;
        phaseTime = 0.0;
        releaseEmitted = false;
    }

    private void StartRecovery(SkeletonPose displayedPose)
    {
        recoveryPose = displayedPose;
        phaseTime = 0.0;
        resolutionReceived = false;
        Phase = ConnectedBasicArrowBodyPhase.Recovery;
    }

    private void ReturnToLocomotion()
    {
        Phase = ConnectedBasicArrowBodyPhase.Locomotion;
        phaseTime = 0.0;
        resolutionReceived = false;
        releaseEmitted = false;
        entryPose = null;
        recoveryPose = null;
    }

    private bool IsActive(CombatCommandSequence sequence) =>
        Phase != ConnectedBasicArrowBodyPhase.Locomotion && activeSequence == sequence;

    private float GetNotchProgress()
    {
        double duration = windupDuration * NotchWindupFraction;
        return duration <= 0.0 ? 1.0f : Math.Clamp((float)(phaseTime / duration), 0.0f, 1.0f);
    }

    private float GetNotchSampleTime() => notch.Duration * GetNotchProgress();

    private float GetAimTransitionProgress()
    {
        double start = windupDuration * NotchWindupFraction;
        double duration = windupDuration - start;
        return duration <= 0.0
            ? 1.0f
            : Math.Clamp((float)((phaseTime - start) / duration), 0.0f, 1.0f);
    }

    private SkeletonPose SampleAim()
    {
        double aimStart = windupDuration * NotchWindupFraction;
        return AnimationSampler.Sample(
            aim,
            (float)Math.Max(0.0, phaseTime - aimStart),
            AnimationPlaybackMode.Loop);
    }

    private void ValidateBasePose(SkeletonPose basePose)
    {
        ArgumentNullException.ThrowIfNull(basePose);
        if (!ReferenceEquals(basePose.Skeleton, notch.Skeleton))
            throw new ArgumentException("Basic Arrow base pose must use the selected character skeleton.", nameof(basePose));
    }

    private static AnimationClip RequireClip(AnimationClip? clip, string expectedName)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (!string.Equals(clip.Name, expectedName, StringComparison.Ordinal))
            throw new ArgumentException($"Expected clip '{expectedName}', received '{clip.Name}'.", nameof(clip));
        return clip;
    }
}
