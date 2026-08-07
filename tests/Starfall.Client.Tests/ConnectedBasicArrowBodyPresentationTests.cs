using System.Numerics;
using ChronoFall.CharacterPresentation;
using Starfall.Client.Networking;
using Starfall.Protocol.Combat;
using Starfall.Protocol.Movement;

namespace Starfall.Client.Tests;

public sealed class ConnectedBasicArrowBodyPresentationTests
{
    [Fact]
    public void Exact_binding_reuses_only_selected_tracks_on_the_target_skeleton()
    {
        SkinDefinition source = CreateSkin();
        SkinDefinition target = CreateSkin();
        AnimationClip[] sourceClips =
        [
            CreateClip("Bow_Notch", source.Skeleton, 2.5f, 1.0f),
            CreateClip("Bow_Aim_Neutral", source.Skeleton, 2.5f, 2.0f),
            CreateClip("Bow_Shoot", source.Skeleton, 2.0f / 3.0f, 3.0f),
        ];

        AnimationClip[] rebound = BasicArrowBowAnimationSet.BindExact(
            source,
            sourceClips,
            target,
            ["Bow_Notch", "Bow_Aim_Neutral", "Bow_Shoot"]);

        Assert.Equal(["Bow_Notch", "Bow_Aim_Neutral", "Bow_Shoot"], rebound.Select(static clip => clip.Name));
        Assert.All(rebound, clip => Assert.Same(target.Skeleton, clip.Skeleton));
        Assert.Equal(sourceClips[2].Tracks, rebound[2].Tracks);
    }

    [Fact]
    public void Exact_binding_rejects_clip_and_skeleton_contract_changes()
    {
        SkinDefinition source = CreateSkin();
        SkinDefinition target = CreateSkin();
        AnimationClip[] sourceClips = CreateBowClips(source.Skeleton);

        Assert.Throws<InvalidDataException>(() => BasicArrowBowAnimationSet.BindExact(
            source,
            [.. sourceClips, CreateClip("Unexpected", source.Skeleton, 1.0f, 0.0f)],
            target,
            ["Bow_Notch", "Bow_Aim_Neutral", "Bow_Shoot"]));

        Matrix4x4[] changedMatrices = target.InverseBindMatrices.ToArray();
        changedMatrices[20] = Matrix4x4.CreateTranslation(0.01f, 0.0f, 0.0f);
        var changedTarget = new SkinDefinition(target.Skeleton, changedMatrices);
        Assert.Throws<InvalidDataException>(() => BasicArrowBowAnimationSet.BindExact(
            source,
            sourceClips,
            changedTarget,
            ["Bow_Notch", "Bow_Aim_Neutral", "Bow_Shoot"]));
    }

    [Fact]
    public void Accepted_action_compresses_notch_and_aim_but_waits_for_resolution_to_shoot()
    {
        ConnectedBasicArrowBodyPresentationController controller = CreateController(out SkeletonDefinition skeleton);
        SkeletonPose locomotion = CreateLocomotionPose(skeleton);
        ConnectedBasicArrowOutcome accepted = Accepted(sequence: 1, startTick: 5, resolveTick: 17);

        controller.HandleOutcome(accepted, locomotion);
        Assert.Equal(ConnectedBasicArrowBodyPhase.Notch, controller.Phase);
        controller.Advance(9.0 / 60.0, locomotion);
        Assert.Equal(ConnectedBasicArrowBodyPhase.AimTransition, controller.Phase);
        controller.Advance(3.0 / 60.0, locomotion);
        Assert.Equal(ConnectedBasicArrowBodyPhase.AimHold, controller.Phase);
        Assert.False(controller.TryDequeueReleaseMarker(out _));

        controller.HandleOutcome(Resolved(sequence: 1, startTick: 5, resolveTick: 17), locomotion);
        Assert.Equal(ConnectedBasicArrowBodyPhase.Shoot, controller.Phase);
        controller.Advance(0.099, locomotion);
        Assert.False(controller.TryDequeueReleaseMarker(out _));
        controller.Advance(0.002, locomotion);
        Assert.True(controller.TryDequeueReleaseMarker(out BasicArrowPresentationReleaseMarker marker));
        Assert.Equal(1UL, marker.Sequence.Value);
        Assert.Equal(ConnectedBasicArrowBodyPresentationController.ReleaseSampleTimeSeconds, marker.ShootSampleTime);
        Assert.Equal(ConnectedBasicArrowBodyPresentationController.ReleaseSampleFrame, marker.ShootSampleFrame);
        Assert.False(controller.TryDequeueReleaseMarker(out _));
    }

    [Fact]
    public void Early_resolution_is_held_until_windup_completes_and_release_is_emitted_once()
    {
        ConnectedBasicArrowBodyPresentationController controller = CreateController(out SkeletonDefinition skeleton);
        SkeletonPose locomotion = CreateLocomotionPose(skeleton);
        controller.HandleOutcome(Accepted(1, 5, 17), locomotion);
        controller.HandleOutcome(Resolved(1, 5, 17), locomotion);

        controller.Advance(0.19, locomotion);
        Assert.NotEqual(ConnectedBasicArrowBodyPhase.Shoot, controller.Phase);
        controller.Advance(0.01, locomotion);
        Assert.Equal(ConnectedBasicArrowBodyPhase.Shoot, controller.Phase);
        Assert.False(controller.TryDequeueReleaseMarker(out _));
        controller.Advance(0.2, locomotion);
        Assert.True(controller.TryDequeueReleaseMarker(out _));
        Assert.False(controller.TryDequeueReleaseMarker(out _));
    }

    [Fact]
    public void Layering_preserves_locomotion_root_pelvis_and_legs_exactly()
    {
        ConnectedBasicArrowBodyPresentationController controller = CreateController(out SkeletonDefinition skeleton);
        SkeletonPose locomotion = CreateLocomotionPose(skeleton);
        controller.HandleOutcome(Accepted(1, 5, 17), locomotion);
        controller.Advance(0.1, locomotion);

        SkeletonPose result = controller.CreatePose(locomotion);

        for (int index = 0; index < 12; index++)
            Assert.Equal(locomotion.LocalTransforms[index], result.LocalTransforms[index]);
        Assert.NotEqual(locomotion.LocalTransforms[12], result.LocalTransforms[12]);
    }

    [Fact]
    public void Cancellation_recovers_without_release_and_rejection_does_not_interrupt_active_action()
    {
        ConnectedBasicArrowBodyPresentationController controller = CreateController(out SkeletonDefinition skeleton);
        SkeletonPose locomotion = CreateLocomotionPose(skeleton);
        controller.HandleOutcome(Accepted(1, 5, 17), locomotion);
        controller.Advance(0.1, locomotion);
        controller.HandleOutcome(Rejected(sequence: 2), locomotion);
        Assert.Equal(ConnectedBasicArrowBodyPhase.Notch, controller.Phase);

        controller.HandleOutcome(Canceled(sequence: 1, startTick: 5, resolveTick: 17), locomotion);
        Assert.Equal(ConnectedBasicArrowBodyPhase.Recovery, controller.Phase);
        controller.Advance(ConnectedBasicArrowBodyPresentationController.RecoveryDurationSeconds, locomotion);
        Assert.Equal(ConnectedBasicArrowBodyPhase.Locomotion, controller.Phase);
        Assert.False(controller.TryDequeueReleaseMarker(out _));
    }

    [Fact]
    public void Repeated_acceptance_starts_from_the_current_displayed_pose()
    {
        ConnectedBasicArrowBodyPresentationController controller = CreateController(out SkeletonDefinition skeleton);
        SkeletonPose locomotion = CreateLocomotionPose(skeleton);
        controller.HandleOutcome(Accepted(1, 5, 17), locomotion);
        controller.Advance(0.1, locomotion);
        SkeletonPose before = controller.CreatePose(locomotion);

        controller.HandleOutcome(Accepted(2, 53, 65), locomotion);
        SkeletonPose after = controller.CreatePose(locomotion);

        Assert.Equal(before.LocalTransforms, after.LocalTransforms);
        Assert.Equal(2UL, controller.ActiveSequence!.Value.Value);
    }

    private static ConnectedBasicArrowBodyPresentationController CreateController(
        out SkeletonDefinition skeleton)
    {
        skeleton = CreateSkin().Skeleton;
        AnimationClip[] clips = CreateBowClips(skeleton);
        return new ConnectedBasicArrowBodyPresentationController(clips[0], clips[1], clips[2]);
    }

    private static AnimationClip[] CreateBowClips(SkeletonDefinition skeleton) =>
    [
        CreateClip("Bow_Notch", skeleton, 2.5f, 1.0f),
        CreateClip("Bow_Aim_Neutral", skeleton, 2.5f, 2.0f),
        CreateClip("Bow_Shoot", skeleton, 2.0f / 3.0f, 3.0f),
    ];

    private static SkinDefinition CreateSkin()
    {
        var joints = new List<SkeletonJoint>(65)
        {
            new("root", -1, JointTransform.Identity),
        };
        for (int index = 1; index < 12; index++)
            joints.Add(new SkeletonJoint($"lower_{index:D2}", index - 1, JointTransform.Identity));
        joints.Add(new SkeletonJoint("spine_01", 1, JointTransform.Identity));
        for (int index = 13; index < 65; index++)
            joints.Add(new SkeletonJoint($"upper_{index:D2}", index - 1, JointTransform.Identity));
        var skeleton = new SkeletonDefinition(joints);
        return new SkinDefinition(skeleton, Enumerable.Repeat(Matrix4x4.Identity, 65));
    }

    private static SkeletonPose CreateLocomotionPose(SkeletonDefinition skeleton) => new(
        skeleton,
        Enumerable.Range(0, skeleton.JointCount)
            .Select(index => new JointTransform(
                new Vector3(index * 0.01f, 0.0f, 0.0f),
                Quaternion.Identity,
                Vector3.One)));

    private static AnimationClip CreateClip(
        string name,
        SkeletonDefinition skeleton,
        float duration,
        float upperTranslation)
    {
        JointAnimationTrack[] tracks = Enumerable.Range(0, skeleton.JointCount)
            .Select(index => new JointAnimationTrack(
                index,
                new Vector3AnimationChannel([
                    new Vector3Keyframe(0.0f, Vector3.Zero),
                    new Vector3Keyframe(duration, index >= 12 ? new Vector3(upperTranslation, 0.0f, 0.0f) : Vector3.Zero),
                ]),
                new QuaternionAnimationChannel([
                    new QuaternionKeyframe(0.0f, Quaternion.Identity),
                    new QuaternionKeyframe(duration, Quaternion.Identity),
                ]),
                new Vector3AnimationChannel([
                    new Vector3Keyframe(0.0f, Vector3.One),
                    new Vector3Keyframe(duration, Vector3.One),
                ])))
            .ToArray();
        return new AnimationClip(name, skeleton, tracks);
    }

    private static ConnectedBasicArrowOutcome Accepted(ulong sequence, ulong startTick, ulong resolveTick) =>
        ConnectedBasicArrowOutcome.Accepted(new BasicArrowAccepted(
            new CombatCommandSequence(sequence),
            new WorldEntityId(1),
            new WorldEntityId(10),
            startTick,
            resolveTick));

    private static ConnectedBasicArrowOutcome Resolved(ulong sequence, ulong startTick, ulong resolveTick) =>
        ConnectedBasicArrowOutcome.Resolved(new BasicArrowResolved(
            new CombatCommandSequence(sequence),
            new WorldEntityId(1),
            new WorldEntityId(10),
            startTick,
            resolveTick,
            300,
            300,
            false));

    private static ConnectedBasicArrowOutcome Rejected(ulong sequence) =>
        ConnectedBasicArrowOutcome.Rejected(new BasicArrowRejected(
            new CombatCommandSequence(sequence),
            new WorldEntityId(1),
            new WorldEntityId(10),
            5,
            BasicArrowRejectionReason.ActionAlreadyPending));

    private static ConnectedBasicArrowOutcome Canceled(ulong sequence, ulong startTick, ulong resolveTick) =>
        ConnectedBasicArrowOutcome.Canceled(new BasicArrowCanceled(
            new CombatCommandSequence(sequence),
            new WorldEntityId(1),
            new WorldEntityId(10),
            startTick,
            resolveTick,
            startTick + 1,
            BasicArrowCancellationReason.CanceledByMovement));
}
