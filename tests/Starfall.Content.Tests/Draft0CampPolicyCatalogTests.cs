using System.Collections.Immutable;
using Starfall.Content.Monsters;
using Starfall.Content.Zones;

namespace Starfall.Content.Tests;

public sealed class Draft0CampPolicyCatalogTests
{
    [Fact]
    public void FirstPlayableFreezesGeometryCapacityPopulationTimingSeedsAndPlacementOrder()
    {
        Draft0CampPolicyCatalogDefinition policies = Draft0CampPolicyCatalog.FirstPlayable;
        Draft0GrayboxLayout graybox = Draft0GrayboxCatalog.FirstPlayable;
        Draft0StarterMonsterCatalogDefinition monsters = Draft0StarterMonsterCatalog.FirstPlayable;

        Assert.Collection(
            policies.Camps,
            camp => AssertPolicy(camp, graybox.Branches[0], monsters.Camps[0], 3, 1),
            camp => AssertPolicy(camp, graybox.Branches[1], monsters.Camps[1], 4, 2),
            camp => AssertPolicy(camp, graybox.Branches[2], monsters.Camps[2], 3, 3));

        Assert.Equal(10, policies.Camps.Sum(static camp => camp.InitialPopulationCount));
        Assert.All(
            policies.Camps.SelectMany(static camp => camp.PlacementSlots),
            assignment =>
            {
                Assert.Equal(0.0f, assignment.Point.Metres.Y);
                Assert.DoesNotContain(graybox.Proxies, proxy => proxy.Contains(assignment.Point));
            });
    }

    [Fact]
    public void CatalogCopiesItsPolicyCollection()
    {
        List<Draft0CampPolicyDefinition> mutable = Draft0CampPolicyCatalog.FirstPlayable.Camps.ToList();
        Draft0CampPolicyCatalogDefinition copy = new(mutable);

        mutable.Clear();

        Assert.Equal(3, copy.Camps.Length);
        Assert.Equal(["camp_easy", "camp_mixed", "camp_hard"], copy.Camps.Select(static camp => camp.Camp.Id));
    }

    [Fact]
    public void PolicyRejectsInvalidStructure()
    {
        Draft0CampPolicyDefinition easy = Draft0CampPolicyCatalog.FirstPlayable.Camps[0];
        Draft0CampLayout otherCamp = new(
            "camp_other",
            easy.Camp.Bounds,
            easy.Camp.EntryAnchor,
            easy.Camp.Geometry);

        Assert.Throws<ArgumentNullException>(() => new Draft0CampPolicyDefinition(
            null!, easy.InitialComposition, 3, 600, 1));
        Assert.Throws<ArgumentNullException>(() => new Draft0CampPolicyDefinition(
            easy.Camp, null!, 3, 600, 1));
        Assert.Throws<ArgumentException>(() => new Draft0CampPolicyDefinition(
            otherCamp, easy.InitialComposition, 3, 600, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Draft0CampPolicyDefinition(
            easy.Camp, easy.InitialComposition, 0, 600, 1));
        Assert.Throws<ArgumentException>(() => new Draft0CampPolicyDefinition(
            easy.Camp, easy.InitialComposition, 2, 600, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Draft0CampPolicyDefinition(
            easy.Camp, easy.InitialComposition, 3, 0, 1));

        Draft0CampCompositionDefinition outsideCircle = new(
            "camp_easy",
            [new Draft0CampSpawnAssignment("spawn_test", "starter_flyer_light", new GroundPoint(45.0f, 55.0f))]);
        Assert.Throws<ArgumentException>(() => new Draft0CampPolicyDefinition(
            easy.Camp, outsideCircle, 1, 600, 1));
    }

    [Fact]
    public void AggregateRejectsMissingDefaultNullDuplicateAndNoncanonicalPolicies()
    {
        ImmutableArray<Draft0CampPolicyDefinition> canonical = Draft0CampPolicyCatalog.FirstPlayable.Camps;

        Assert.Throws<ArgumentNullException>(() => new Draft0CampPolicyCatalogDefinition(null!));
        Assert.Throws<ArgumentException>(() => new Draft0CampPolicyCatalogDefinition([]));
        Assert.Throws<ArgumentException>(() => new Draft0CampPolicyCatalogDefinition(
            default(ImmutableArray<Draft0CampPolicyDefinition>)));
        Assert.Throws<ArgumentException>(() => new Draft0CampPolicyCatalogDefinition([null!]));
        Assert.Throws<ArgumentException>(() => new Draft0CampPolicyCatalogDefinition(
            [canonical[0], canonical[0], canonical[2]]));
        Assert.Throws<ArgumentException>(() => new Draft0CampPolicyCatalogDefinition(
            [canonical[1], canonical[0], canonical[2]]));

        Draft0CampPolicyDefinition wrongSeed = new(
            canonical[0].Camp,
            canonical[0].InitialComposition,
            canonical[0].Capacity,
            canonical[0].ReplenishmentDelayTicks,
            99);
        Assert.Throws<ArgumentException>(() => new Draft0CampPolicyCatalogDefinition(
            [wrongSeed, canonical[1], canonical[2]]));

        Draft0CampPolicyDefinition wrongDelay = new(
            canonical[0].Camp,
            canonical[0].InitialComposition,
            canonical[0].Capacity,
            599,
            canonical[0].AuthoritativeSeed);
        Assert.Throws<ArgumentException>(() => new Draft0CampPolicyCatalogDefinition(
            [wrongDelay, canonical[1], canonical[2]]));
    }

    [Fact]
    public void RepeatedConstructionProducesIdenticalValues()
    {
        Draft0CampPolicyCatalogDefinition first = Draft0CampPolicyCatalog.FirstPlayable;
        Draft0CampPolicyCatalogDefinition second = new(first.Camps);

        for (var campIndex = 0; campIndex < first.Camps.Length; campIndex++)
        {
            Draft0CampPolicyDefinition left = first.Camps[campIndex];
            Draft0CampPolicyDefinition right = second.Camps[campIndex];
            Assert.Equal(left.Camp.Id, right.Camp.Id);
            Assert.Equal(left.Camp.Bounds, right.Camp.Bounds);
            Assert.Equal(left.Camp.EntryAnchor, right.Camp.EntryAnchor);
            Assert.Equal(left.Camp.Geometry, right.Camp.Geometry);
            Assert.Equal(left.Capacity, right.Capacity);
            Assert.Equal(left.ReplenishmentDelayTicks, right.ReplenishmentDelayTicks);
            Assert.Equal(left.AuthoritativeSeed, right.AuthoritativeSeed);
            Assert.Equal(left.PlacementSlots, right.PlacementSlots);
        }
    }

    private static void AssertPolicy(
        Draft0CampPolicyDefinition policy,
        Draft0BranchLayout branch,
        Draft0CampCompositionDefinition composition,
        int capacity,
        ulong seed)
    {
        Assert.Equal(branch.Camp.Id, policy.Camp.Id);
        Assert.Equal(branch.Camp.Bounds, policy.Camp.Bounds);
        Assert.Equal(branch.Camp.EntryAnchor, policy.Camp.EntryAnchor);
        Assert.Equal(branch.Camp.Geometry, policy.Camp.Geometry);
        Assert.Equal(capacity, policy.Capacity);
        Assert.Equal(capacity, policy.InitialPopulationCount);
        Assert.Equal(Draft0CampPolicyCatalog.ReplenishmentDelayTicks, policy.ReplenishmentDelayTicks);
        Assert.Equal(600UL, policy.ReplenishmentDelayTicks);
        Assert.Equal(seed, policy.AuthoritativeSeed);
        Assert.Equal(composition.Assignments, policy.PlacementSlots);

        for (var index = 0; index < policy.PlacementSlots.Length; index++)
        {
            Draft0CampSpawnAssignment assignment = policy.PlacementSlots[index];
            Assert.Equal(branch.SampleSpawns[index].Id, assignment.SpawnId);
            Assert.Equal(branch.SampleSpawns[index].Point, assignment.Point);
            Assert.True(policy.Camp.Contains(assignment.Point));
        }
    }
}
