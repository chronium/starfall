using System.Numerics;
using Starfall.Content.Zones;
using Starfall.Simulation.Combat;
using Starfall.Simulation.Entities;
using Starfall.World.Combat;

namespace Starfall.World.Tests;

public sealed class WorldBasicArrowCombatTests
{
    [Fact]
    public void Accepted_movement_cancels_pending_action_but_preserves_cadence()
    {
        var combat = new WorldBasicArrowCombat();
        WorldEntityId actorId = new(1);
        WorldEntityId targetId = new(2);
        var intent = new BasicArrowIntent("basic_arrow", actorId, targetId);
        var actor = new BasicArrowActorState(
            actorId,
            new GroundPoint(0.0f, 0.0f),
            Vector2.Zero,
            Vector2.UnitX);
        var target = new BasicArrowTargetState(
            targetId,
            new GroundPoint(10.0f, 0.0f),
            700);

        BasicArrowStartEvaluation started = combat.TryStart(intent, actor, target, 10);
        Assert.Equal(BasicArrowStartDisposition.Accepted, started.Disposition);
        Assert.Equal(1, combat.PendingCount);

        BasicArrowResolution canceled = Assert.IsType<BasicArrowResolution>(
            combat.CancelForMovement(actorId, 11));
        Assert.Equal(BasicArrowResolutionDisposition.CanceledByMovement, canceled.Disposition);
        Assert.Equal(0, combat.PendingCount);
        Assert.Equal(58UL, combat.GetNextAllowedStartTick(actorId));
        Assert.Equal(
            BasicArrowStartDisposition.CadenceNotReady,
            combat.TryStart(intent, actor, target, 57).Disposition);
        Assert.Equal(
            BasicArrowStartDisposition.Accepted,
            combat.TryStart(intent, actor, target, 58).Disposition);
    }

    [Fact]
    public void Due_actions_are_removed_in_actor_identity_order_and_cleanup_is_explicit()
    {
        var combat = new WorldBasicArrowCombat();
        BasicArrowTargetState target = new(
            new WorldEntityId(3),
            new GroundPoint(10.0f, 0.0f),
            2_000);
        foreach (ulong actorValue in new ulong[] { 2, 1 })
        {
            WorldEntityId actorId = new(actorValue);
            var actor = new BasicArrowActorState(
                actorId,
                new GroundPoint(0.0f, 0.0f),
                Vector2.Zero,
                Vector2.UnitX);
            Assert.Equal(
                BasicArrowStartDisposition.Accepted,
                combat.TryStart(
                    new BasicArrowIntent("basic_arrow", actorId, target.EntityId),
                    actor,
                    target,
                    0).Disposition);
        }

        Assert.Empty(combat.TakeDue(11));
        Assert.Equal([1UL, 2UL], combat.TakeDue(12).Select(static action => action.ActorId.Value));
        Assert.Equal(0, combat.PendingCount);

        combat.RemoveActor(new WorldEntityId(1));
        Assert.Equal(0UL, combat.GetNextAllowedStartTick(new WorldEntityId(1)));
        combat.Clear();
        Assert.Equal(0UL, combat.GetNextAllowedStartTick(new WorldEntityId(2)));
    }
}
