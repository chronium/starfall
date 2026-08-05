using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.World.Entities;
using Starfall.World.Monsters;

namespace Starfall.World.Tests;

public sealed class WorldMonsterPopulationTests
{
    [Fact]
    public void Checked_eligibility_failure_preserves_existing_occupancy()
    {
        var population = new WorldMonsterPopulation(
            Draft0GrayboxCatalog.FirstPlayable,
            Draft0StarterMonsterCatalog.FirstPlayable,
            Draft0CampPolicyCatalog.FirstPlayable);
        var identities = new WorldEntityIdSequence();
        population.Initialize(0, identities.Allocate);
        WorldMonsterState original = population.Snapshot()[0];

        Assert.Throws<OverflowException>(() =>
            population.Remove(
                original.EntityId,
                ulong.MaxValue - Draft0CampPolicyCatalog.ReplenishmentDelayTicks + 1));

        Assert.Equal(10, population.Count);
        Assert.True(population.TryGet(original.EntityId, out WorldMonsterState? retained));
        Assert.Same(original, retained);
    }
}
