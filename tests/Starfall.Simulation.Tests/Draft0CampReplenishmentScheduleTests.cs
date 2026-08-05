using System.Collections.Immutable;
using Starfall.Content.Monsters;
using Starfall.Simulation.Camps;

namespace Starfall.Simulation.Tests;

public sealed class Draft0CampReplenishmentScheduleTests
{
    [Fact]
    public void EmptyVacancySetProducesNoDecisions()
    {
        ImmutableArray<Draft0CampReplenishmentDecision> decisions = Draft0CampReplenishmentSchedule.Create(
            Draft0CampPolicyCatalog.FirstPlayable,
            []);

        Assert.Empty(decisions);
    }

    [Fact]
    public void OrdersByEligibilityThenCanonicalCampAndSlotOrder()
    {
        ImmutableArray<Draft0CampReplenishmentDecision> decisions = Draft0CampReplenishmentSchedule.Create(
            Draft0CampPolicyCatalog.FirstPlayable,
            [
                new Draft0CampVacancy("camp_hard", "spawn_hard_02", 5),
                new Draft0CampVacancy("camp_mixed", "spawn_mixed_03", 0),
                new Draft0CampVacancy("camp_easy", "spawn_easy_03", 0),
                new Draft0CampVacancy("camp_easy", "spawn_easy_01", 0),
            ]);

        Assert.Collection(
            decisions,
            decision => AssertDecision(decision, "camp_easy", "spawn_easy_01", "starter_flyer_light", 600),
            decision => AssertDecision(decision, "camp_easy", "spawn_easy_03", "starter_flyer_light", 600),
            decision => AssertDecision(decision, "camp_mixed", "spawn_mixed_03", "starter_flyer_heavy", 600),
            decision => AssertDecision(decision, "camp_hard", "spawn_hard_02", "starter_flyer_heavy", 605));
    }

    [Fact]
    public void EligibilityUsesRemovalTickAndCheckedUnsignedArithmetic()
    {
        Draft0CampReplenishmentDecision atTickZero = Assert.Single(Draft0CampReplenishmentSchedule.Create(
            Draft0CampPolicyCatalog.FirstPlayable,
            [new Draft0CampVacancy("camp_easy", "spawn_easy_01", 0)]));
        Assert.Equal(600UL, atTickZero.EligibleAtTick);

        Draft0CampReplenishmentDecision atMaximum = Assert.Single(Draft0CampReplenishmentSchedule.Create(
            Draft0CampPolicyCatalog.FirstPlayable,
            [new Draft0CampVacancy("camp_easy", "spawn_easy_01", ulong.MaxValue - 600)]));
        Assert.Equal(ulong.MaxValue, atMaximum.EligibleAtTick);

        Assert.Throws<OverflowException>(() => Draft0CampReplenishmentSchedule.Create(
            Draft0CampPolicyCatalog.FirstPlayable,
            [new Draft0CampVacancy("camp_easy", "spawn_easy_01", ulong.MaxValue - 599)]));
    }

    [Fact]
    public void RejectsMissingUnknownAndDuplicateVacancies()
    {
        Assert.Throws<ArgumentNullException>(() => Draft0CampReplenishmentSchedule.Create(
            null!,
            Array.Empty<Draft0CampVacancy>()));
        Assert.Throws<ArgumentNullException>(() => Draft0CampReplenishmentSchedule.Create(
            Draft0CampPolicyCatalog.FirstPlayable,
            null!));
        Assert.Throws<ArgumentException>(() => Draft0CampReplenishmentSchedule.Create(
            Draft0CampPolicyCatalog.FirstPlayable,
            default(ImmutableArray<Draft0CampVacancy>)));
        Assert.Throws<ArgumentException>(() => Draft0CampReplenishmentSchedule.Create(
            Draft0CampPolicyCatalog.FirstPlayable,
            [default(Draft0CampVacancy)]));
        Assert.Throws<ArgumentException>(() => Draft0CampReplenishmentSchedule.Create(
            Draft0CampPolicyCatalog.FirstPlayable,
            [new Draft0CampVacancy("camp_unknown", "spawn_easy_01", 0)]));
        Assert.Throws<ArgumentException>(() => Draft0CampReplenishmentSchedule.Create(
            Draft0CampPolicyCatalog.FirstPlayable,
            [new Draft0CampVacancy("camp_easy", "spawn_unknown", 0)]));
        Assert.Throws<ArgumentException>(() => Draft0CampReplenishmentSchedule.Create(
            Draft0CampPolicyCatalog.FirstPlayable,
            [
                new Draft0CampVacancy("camp_easy", "spawn_easy_01", 0),
                new Draft0CampVacancy("camp_easy", "spawn_easy_01", 1),
            ]));
    }

    [Fact]
    public void RepeatedSchedulingIsStableAndDoesNotConsumeSeeds()
    {
        Draft0CampVacancy[] vacancies =
        [
            new Draft0CampVacancy("camp_hard", "spawn_hard_03", 12),
            new Draft0CampVacancy("camp_mixed", "spawn_mixed_01", 12),
            new Draft0CampVacancy("camp_easy", "spawn_easy_02", 12),
        ];

        ImmutableArray<Draft0CampReplenishmentDecision> first = Draft0CampReplenishmentSchedule.Create(
            Draft0CampPolicyCatalog.FirstPlayable,
            vacancies);
        ImmutableArray<Draft0CampReplenishmentDecision> second = Draft0CampReplenishmentSchedule.Create(
            Draft0CampPolicyCatalog.FirstPlayable,
            vacancies.Reverse());

        Assert.Equal(
            first.Select(ToComparable),
            second.Select(ToComparable));
        Assert.Equal([1UL, 2UL, 3UL], Draft0CampPolicyCatalog.FirstPlayable.Camps.Select(static camp => camp.AuthoritativeSeed));
    }

    private static (string CampId, string SpawnId, ulong EligibleAtTick) ToComparable(
        Draft0CampReplenishmentDecision decision) =>
        (decision.CampId, decision.Assignment.SpawnId, decision.EligibleAtTick);

    private static void AssertDecision(
        Draft0CampReplenishmentDecision decision,
        string campId,
        string spawnId,
        string archetypeId,
        ulong eligibleAtTick)
    {
        Assert.Equal(campId, decision.CampId);
        Assert.Equal(spawnId, decision.Assignment.SpawnId);
        Assert.Equal(archetypeId, decision.Assignment.ArchetypeId);
        Assert.Equal(eligibleAtTick, decision.EligibleAtTick);
    }
}
