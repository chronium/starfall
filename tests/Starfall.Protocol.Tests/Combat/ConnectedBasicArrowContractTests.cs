using Starfall.Protocol.Combat;
using Starfall.Protocol.Movement;

namespace Starfall.Protocol.Tests.Combat;

public sealed class ConnectedBasicArrowContractTests
{
    [Fact]
    public void Combat_identifiers_are_positive_bounded_and_canonical()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CombatCommandSequence(0));
        Assert.Equal("42", new CombatCommandSequence(42).ToString());

        Assert.ThrowsAny<ArgumentException>(() => new CombatActionId(null!));
        Assert.ThrowsAny<ArgumentException>(() => new CombatActionId(""));
        Assert.ThrowsAny<ArgumentException>(() => new CombatActionId("Basic_arrow"));
        Assert.ThrowsAny<ArgumentException>(() => new CombatActionId("1_basic_arrow"));
        Assert.ThrowsAny<ArgumentException>(() => new CombatActionId("basic-arrow"));
        Assert.ThrowsAny<ArgumentException>(() => new CombatActionId(new string('a', 65)));

        string maximum = $"a{new string('0', 63)}";
        Assert.Equal(maximum, new CombatActionId(maximum).Value);
        Assert.Equal("basic_arrow", ConnectedBasicArrow.ActionId.Value);
        Assert.Equal("basic_arrow", ConnectedBasicArrow.ActionId.ToString());
    }

    [Fact]
    public void Command_contains_only_correlation_action_and_target_facts()
    {
        var command = new BasicArrowCommand(
            new CombatCommandSequence(3),
            new WorldEntityId(200));

        Assert.Equal(3UL, command.Sequence.Value);
        Assert.Equal(ConnectedBasicArrow.ActionId, command.ActionId);
        Assert.Equal(200UL, command.TargetEntityId.Value);
        Assert.DoesNotContain(
            typeof(BasicArrowCommand).GetProperties(),
            static property => property.Name.Contains("Actor", StringComparison.Ordinal));

        Assert.ThrowsAny<ArgumentException>(() => new BasicArrowCommand(default, new WorldEntityId(200)));
        Assert.ThrowsAny<ArgumentException>(() => new BasicArrowCommand(new CombatCommandSequence(3), default));
    }

    [Fact]
    public void Acceptance_preserves_authoritative_actor_target_and_fixed_ticks()
    {
        var accepted = new BasicArrowAccepted(
            new CombatCommandSequence(4),
            new WorldEntityId(100),
            new WorldEntityId(200),
            startTick: 0,
            resolveTick: 12);

        Assert.Equal(4UL, accepted.Sequence.Value);
        Assert.Equal(ConnectedBasicArrow.ActionId, accepted.ActionId);
        Assert.Equal(100UL, accepted.ActorEntityId.Value);
        Assert.Equal(200UL, accepted.TargetEntityId.Value);
        Assert.Equal(0UL, accepted.StartTick);
        Assert.Equal(12UL, accepted.ResolveTick);
    }

    [Theory]
    [InlineData(BasicArrowRejectionReason.ActorUnavailable)]
    [InlineData(BasicArrowRejectionReason.TargetUnavailable)]
    [InlineData(BasicArrowRejectionReason.ActorDefeated)]
    [InlineData(BasicArrowRejectionReason.ActorInProtectedTown)]
    [InlineData(BasicArrowRejectionReason.ActionAlreadyPending)]
    [InlineData(BasicArrowRejectionReason.CadenceNotReady)]
    [InlineData(BasicArrowRejectionReason.TargetCoincident)]
    [InlineData(BasicArrowRejectionReason.TargetOutOfRange)]
    public void Rejection_preserves_the_reachable_authoritative_reason(BasicArrowRejectionReason reason)
    {
        var rejected = new BasicArrowRejected(
            new CombatCommandSequence(5),
            new WorldEntityId(100),
            new WorldEntityId(200),
            decisionTick: 0,
            reason);

        Assert.Equal(5UL, rejected.Sequence.Value);
        Assert.Equal(ConnectedBasicArrow.ActionId, rejected.ActionId);
        Assert.Equal(100UL, rejected.ActorEntityId.Value);
        Assert.Equal(200UL, rejected.TargetEntityId.Value);
        Assert.Equal(0UL, rejected.DecisionTick);
        Assert.Equal(reason, rejected.Reason);
    }

    [Theory]
    [InlineData(BasicArrowCancellationReason.CanceledByMovement)]
    [InlineData(BasicArrowCancellationReason.ActorDefeated)]
    [InlineData(BasicArrowCancellationReason.ActorUnavailable)]
    [InlineData(BasicArrowCancellationReason.TargetUnavailable)]
    [InlineData(BasicArrowCancellationReason.ActorMoving)]
    [InlineData(BasicArrowCancellationReason.TargetCoincident)]
    [InlineData(BasicArrowCancellationReason.TargetOutOfRange)]
    [InlineData(BasicArrowCancellationReason.TargetOutsideFacing)]
    public void Cancellation_preserves_accepted_timing_and_reason(BasicArrowCancellationReason reason)
    {
        var canceled = new BasicArrowCanceled(
            new CombatCommandSequence(6),
            new WorldEntityId(100),
            new WorldEntityId(200),
            startTick: 10,
            resolveTick: 22,
            cancellationTick: 15,
            reason);

        Assert.Equal(6UL, canceled.Sequence.Value);
        Assert.Equal(ConnectedBasicArrow.ActionId, canceled.ActionId);
        Assert.Equal(100UL, canceled.ActorEntityId.Value);
        Assert.Equal(200UL, canceled.TargetEntityId.Value);
        Assert.Equal(10UL, canceled.StartTick);
        Assert.Equal(22UL, canceled.ResolveTick);
        Assert.Equal(15UL, canceled.CancellationTick);
        Assert.Equal(reason, canceled.Reason);
    }

    [Theory]
    [InlineData(300, false)]
    [InlineData(100, true)]
    public void Resolution_preserves_requested_effective_damage_and_defeat(
        int effectiveDamageUnits,
        bool targetDefeated)
    {
        var resolved = new BasicArrowResolved(
            new CombatCommandSequence(7),
            new WorldEntityId(100),
            new WorldEntityId(200),
            startTick: 20,
            resolveTick: 32,
            requestedDamageUnits: ConnectedBasicArrow.RequestedDamageUnits,
            effectiveDamageUnits,
            targetDefeated);

        Assert.Equal(7UL, resolved.Sequence.Value);
        Assert.Equal(ConnectedBasicArrow.ActionId, resolved.ActionId);
        Assert.Equal(100UL, resolved.ActorEntityId.Value);
        Assert.Equal(200UL, resolved.TargetEntityId.Value);
        Assert.Equal(20UL, resolved.StartTick);
        Assert.Equal(32UL, resolved.ResolveTick);
        Assert.Equal(300, resolved.RequestedDamageUnits);
        Assert.Equal(effectiveDamageUnits, resolved.EffectiveDamageUnits);
        Assert.Equal(targetDefeated, resolved.TargetDefeated);
    }

    [Fact]
    public void Outcome_facts_reject_default_identity_and_invalid_relationships()
    {
        Assert.ThrowsAny<ArgumentException>(() => new BasicArrowAccepted(
            default,
            new WorldEntityId(100),
            new WorldEntityId(200),
            0,
            12));
        Assert.ThrowsAny<ArgumentException>(() => new BasicArrowAccepted(
            new CombatCommandSequence(1),
            default,
            new WorldEntityId(200),
            0,
            12));
        Assert.ThrowsAny<ArgumentException>(() => new BasicArrowAccepted(
            new CombatCommandSequence(1),
            new WorldEntityId(100),
            default,
            0,
            12));
        Assert.ThrowsAny<ArgumentException>(() => new BasicArrowAccepted(
            new CombatCommandSequence(1),
            new WorldEntityId(100),
            new WorldEntityId(100),
            0,
            12));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BasicArrowRejected(
            new CombatCommandSequence(1),
            new WorldEntityId(100),
            new WorldEntityId(200),
            0,
            (BasicArrowRejectionReason)byte.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BasicArrowCanceled(
            new CombatCommandSequence(1),
            new WorldEntityId(100),
            new WorldEntityId(200),
            0,
            12,
            1,
            (BasicArrowCancellationReason)byte.MaxValue));
    }

    [Fact]
    public void Accepted_and_terminal_facts_reject_invalid_tick_or_damage_state()
    {
        Assert.Equal(10UL, CreateCanceled(cancellationTick: 10).CancellationTick);
        Assert.Equal(22UL, CreateCanceled(cancellationTick: 22).CancellationTick);
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateAccepted(startTick: 12, resolveTick: 12));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateAccepted(startTick: 13, resolveTick: 12));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateCanceled(cancellationTick: 9));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateCanceled(cancellationTick: 23));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateResolved(requestedDamageUnits: 299));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateResolved(effectiveDamageUnits: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateResolved(effectiveDamageUnits: 301));
    }

    private static BasicArrowAccepted CreateAccepted(ulong startTick, ulong resolveTick) =>
        new(
            new CombatCommandSequence(1),
            new WorldEntityId(100),
            new WorldEntityId(200),
            startTick,
            resolveTick);

    private static BasicArrowCanceled CreateCanceled(ulong cancellationTick) =>
        new(
            new CombatCommandSequence(1),
            new WorldEntityId(100),
            new WorldEntityId(200),
            startTick: 10,
            resolveTick: 22,
            cancellationTick,
            BasicArrowCancellationReason.CanceledByMovement);

    private static BasicArrowResolved CreateResolved(
        int requestedDamageUnits = ConnectedBasicArrow.RequestedDamageUnits,
        int effectiveDamageUnits = ConnectedBasicArrow.RequestedDamageUnits) =>
        new(
            new CombatCommandSequence(1),
            new WorldEntityId(100),
            new WorldEntityId(200),
            startTick: 10,
            resolveTick: 22,
            requestedDamageUnits,
            effectiveDamageUnits,
            targetDefeated: false);
}
