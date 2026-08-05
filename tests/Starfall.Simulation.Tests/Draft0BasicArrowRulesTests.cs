using System.Numerics;
using Starfall.Content.Zones;
using Starfall.Simulation.Combat;
using Starfall.Simulation.Entities;

namespace Starfall.Simulation.Tests;

public sealed class Draft0BasicArrowRulesTests
{
    private static readonly WorldEntityId ActorId = new(1);
    private static readonly WorldEntityId TargetId = new(2);

    [Fact]
    public void First_playable_tuning_consumes_the_content_contract_and_frozen_inputs()
    {
        Draft0BasicArrowTuning tuning = Draft0BasicArrowTuning.FirstPlayable;

        Assert.Equal("basic_arrow", tuning.ActionId);
        Assert.Equal(300, tuning.DamageUnits);
        Assert.Equal(12.0f, tuning.MaximumRangeMetres);
        Assert.Equal(0.70710677f, tuning.MinimumFacingDot);
        Assert.Equal(12UL, tuning.ResolveDelayTicks);
        Assert.Equal(48UL, tuning.CadenceTicks);
    }

    [Fact]
    public void Starts_at_the_inclusive_range_and_records_exact_ticks_and_facing()
    {
        BasicArrowStartEvaluation result = Start(
            actor: Actor(position: new GroundPoint(10.0f, 20.0f)),
            target: Target(position: new GroundPoint(22.0f, 20.0f)),
            currentTick: 7);

        Assert.Equal(BasicArrowStartDisposition.Accepted, result.Disposition);
        PendingBasicArrow pending = Assert.IsType<PendingBasicArrow>(result.PendingAction);
        Assert.Equal(7UL, pending.StartTick);
        Assert.Equal(19UL, pending.ResolveTick);
        Assert.Equal(55UL, pending.NextAllowedStartTick);
        Assert.Equal(Vector2.UnitX, pending.AcceptedFacing);
    }

    [Fact]
    public void Rejects_wrong_action_pending_cadence_coincident_and_out_of_range_inputs()
    {
        BasicArrowActorState actor = Actor();
        BasicArrowTargetState target = Target();

        Assert.Equal(
            BasicArrowStartDisposition.WrongAction,
            Start(actionId: "fire_arrow", actor: actor, target: target).Disposition);
        Assert.Equal(
            BasicArrowStartDisposition.ActionAlreadyPending,
            Start(actor: actor, target: target, hasPending: true).Disposition);
        Assert.Equal(
            BasicArrowStartDisposition.CadenceNotReady,
            Start(actor: actor, target: target, currentTick: 47, nextAllowedStartTick: 48).Disposition);
        Assert.Equal(
            BasicArrowStartDisposition.TargetCoincident,
            Start(actor: actor, target: Target(position: actor.Position)).Disposition);
        Assert.Equal(
            BasicArrowStartDisposition.TargetOutOfRange,
            Start(actor: actor, target: Target(position: new GroundPoint(12.001f, 0.0f))).Disposition);
    }

    [Fact]
    public void Checked_tick_arithmetic_fails_before_returning_a_pending_action()
    {
        Assert.Throws<OverflowException>(() => Start(currentTick: ulong.MaxValue - 11));
        Assert.Throws<OverflowException>(() => Start(currentTick: ulong.MaxValue - 47));
    }

    [Fact]
    public void Resolves_at_the_exact_tick_with_an_inclusive_forty_five_degree_facing()
    {
        const float diagonal = 8.485281f;
        BasicArrowTargetState target = Target(
            position: new GroundPoint(diagonal, diagonal),
            healthUnits: 700);
        BasicArrowStartEvaluation started = Start(target: target);
        PendingBasicArrow pending = Assert.IsType<PendingBasicArrow>(started.PendingAction);
        var actor = Actor(facing: Vector2.UnitX);

        BasicArrowResolution resolved = Draft0BasicArrowRules.Resolve(
            Draft0BasicArrowTuning.FirstPlayable,
            pending,
            actor,
            target,
            pending.ResolveTick);

        Assert.Equal(BasicArrowResolutionDisposition.Resolved, resolved.Disposition);
        AuthoritativeDamageResult damage = Assert.IsType<AuthoritativeDamageResult>(resolved.Damage);
        Assert.Equal(300, damage.RequestedDamageUnits);
        Assert.Equal(300, damage.AppliedDamageUnits);
        Assert.Equal(700, damage.PreviousHealthUnits);
        Assert.Equal(400, damage.RemainingHealthUnits);
        Assert.False(damage.Defeated);
    }

    [Fact]
    public void Resolve_rejects_the_wrong_tick_and_cancels_invalid_current_state()
    {
        BasicArrowStartEvaluation started = Start();
        PendingBasicArrow pending = Assert.IsType<PendingBasicArrow>(started.PendingAction);

        Assert.Throws<ArgumentOutOfRangeException>(() => Draft0BasicArrowRules.Resolve(
            Draft0BasicArrowTuning.FirstPlayable,
            pending,
            Actor(),
            Target(),
            pending.ResolveTick - 1));
        Assert.Equal(
            BasicArrowResolutionDisposition.ActorMoving,
            Resolve(pending, Actor(velocity: Vector2.UnitX), Target()).Disposition);
        Assert.Equal(
            BasicArrowResolutionDisposition.TargetCoincident,
            Resolve(pending, Actor(), Target(position: new GroundPoint(0.0f, 0.0f))).Disposition);
        Assert.Equal(
            BasicArrowResolutionDisposition.TargetOutOfRange,
            Resolve(pending, Actor(), Target(position: new GroundPoint(13.0f, 0.0f))).Disposition);
        Assert.Equal(
            BasicArrowResolutionDisposition.TargetOutsideFacing,
            Resolve(pending, Actor(facing: -Vector2.UnitX), Target()).Disposition);
    }

    [Fact]
    public void Integer_damage_preserves_the_three_and_seven_hit_breakpoints()
    {
        Assert.Equal([400, 100, 0], ApplyRepeatedly(700));
        Assert.Equal([1_700, 1_400, 1_100, 800, 500, 200, 0], ApplyRepeatedly(2_000));

        AuthoritativeDamageResult finalLightHit = AuthoritativeIntegerDamage.Apply(100, 300);
        Assert.Equal(100, finalLightHit.AppliedDamageUnits);
        Assert.True(finalLightHit.Defeated);
        Assert.Throws<ArgumentOutOfRangeException>(() => AuthoritativeIntegerDamage.Apply(0, 300));
        Assert.Throws<ArgumentOutOfRangeException>(() => AuthoritativeIntegerDamage.Apply(700, 0));
    }

    [Fact]
    public void Constructors_reject_malformed_authoritative_facts_and_tuning()
    {
        Assert.Throws<ArgumentException>(() => new BasicArrowIntent("basic_arrow", default, TargetId));
        Assert.Throws<ArgumentException>(() => new BasicArrowIntent("basic_arrow", ActorId, default));
        Assert.Throws<ArgumentException>(() => new BasicArrowActorState(
            default,
            new GroundPoint(0.0f, 0.0f),
            Vector2.Zero,
            Vector2.UnitX));
        Assert.Throws<ArgumentException>(() => new BasicArrowActorState(
            ActorId,
            new GroundPoint(0.0f, 0.0f),
            new Vector2(float.NaN, 0.0f),
            Vector2.UnitX));
        Assert.Throws<ArgumentException>(() => new BasicArrowActorState(
            ActorId,
            new GroundPoint(0.0f, 0.0f),
            Vector2.Zero,
            Vector2.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BasicArrowTargetState(
            TargetId,
            new GroundPoint(1.0f, 0.0f),
            0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PendingBasicArrow(
            "basic_arrow",
            ActorId,
            TargetId,
            12,
            12,
            48,
            Vector2.UnitX));
        Assert.Throws<ArgumentException>(() => new PendingBasicArrow(
            "basic_arrow",
            ActorId,
            TargetId,
            0,
            12,
            48,
            Vector2.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Draft0BasicArrowTuning(
            "basic_arrow",
            300,
            12.0f,
            0.7f,
            12,
            11));
    }

    private static BasicArrowStartEvaluation Start(
        string actionId = "basic_arrow",
        BasicArrowActorState? actor = null,
        BasicArrowTargetState? target = null,
        ulong currentTick = 0,
        ulong nextAllowedStartTick = 0,
        bool hasPending = false)
    {
        BasicArrowActorState actualActor = actor ?? Actor();
        BasicArrowTargetState actualTarget = target ?? Target();
        return Draft0BasicArrowRules.TryStart(
            Draft0BasicArrowTuning.FirstPlayable,
            new BasicArrowIntent(actionId, actualActor.EntityId, actualTarget.EntityId),
            actualActor,
            actualTarget,
            currentTick,
            nextAllowedStartTick,
            hasPending);
    }

    private static BasicArrowResolution Resolve(
        PendingBasicArrow pending,
        BasicArrowActorState actor,
        BasicArrowTargetState target) =>
        Draft0BasicArrowRules.Resolve(
            Draft0BasicArrowTuning.FirstPlayable,
            pending,
            actor,
            target,
            pending.ResolveTick);

    private static BasicArrowActorState Actor(
        GroundPoint? position = null,
        Vector2? velocity = null,
        Vector2? facing = null) =>
        new(
            ActorId,
            position ?? new GroundPoint(0.0f, 0.0f),
            velocity ?? Vector2.Zero,
            facing ?? Vector2.UnitX);

    private static BasicArrowTargetState Target(
        GroundPoint? position = null,
        int healthUnits = 700) =>
        new(
            TargetId,
            position ?? new GroundPoint(12.0f, 0.0f),
            healthUnits);

    private static int[] ApplyRepeatedly(int initialHealth)
    {
        var health = initialHealth;
        var results = new List<int>();
        while (health > 0)
        {
            AuthoritativeDamageResult damage = AuthoritativeIntegerDamage.Apply(health, 300);
            health = damage.RemainingHealthUnits;
            results.Add(health);
        }

        return results.ToArray();
    }
}
