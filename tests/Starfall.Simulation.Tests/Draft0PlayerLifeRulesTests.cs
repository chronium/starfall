using Starfall.Content.Zones;
using Starfall.Simulation.Combat;
using Starfall.Simulation.Entities;
using Starfall.Simulation.Monsters;
using Starfall.Simulation.Players;

namespace Starfall.Simulation.Tests;

public sealed class Draft0PlayerLifeRulesTests
{
    [Fact]
    public void First_playable_uses_full_health_and_a_three_second_delay()
    {
        Draft0PlayerLifeTuning tuning = Draft0PlayerLifeTuning.FirstPlayable;

        Assert.Equal(2_500, tuning.MaximumHealthUnits);
        Assert.Equal(2_500, tuning.RestoredHealthUnits);
        Assert.Equal(180UL, tuning.RespawnDelayTicks);
        Assert.Equal(280UL, Draft0PlayerLifeRules.ScheduleRespawn(tuning, 100));
    }

    [Fact]
    public void Tuning_and_checked_schedule_reject_invalid_inputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Draft0PlayerLifeTuning(0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Draft0PlayerLifeTuning(100, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Draft0PlayerLifeTuning(100, 101, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Draft0PlayerLifeTuning(100, 100, 0));
        Assert.Throws<OverflowException>(() => Draft0PlayerLifeRules.ScheduleRespawn(
            new Draft0PlayerLifeTuning(100, 100, 2),
            ulong.MaxValue - 1));
    }

    [Fact]
    public void Protected_town_uses_inclusive_authoring_bounds()
    {
        Draft0TownLayout town = Draft0GrayboxCatalog.FirstPlayable.Town;

        Assert.True(Draft0PlayerLifeRules.IsHostileActionBlocked(town, town.RespawnAnchor));
        Assert.True(Draft0PlayerLifeRules.IsHostileActionBlocked(town, town.ExitAnchor));
        Assert.True(Draft0PlayerLifeRules.IsHostileActionBlocked(town, town.Bounds.Minimum));
        Assert.False(Draft0PlayerLifeRules.IsHostileActionBlocked(
            town,
            new GroundPoint(town.ExitAnchor.XMetres, town.ExitAnchor.ZMetres + 0.01f)));
    }

    [Fact]
    public void Applied_damage_preserves_the_authoritative_request()
    {
        var attack = new Draft0MonsterAttackResolution(
            new WorldEntityId(1),
            new WorldEntityId(2),
            42,
            200);
        AuthoritativeDamageResult damage = AuthoritativeIntegerDamage.Apply(100, 200);
        var outcome = new Draft0AppliedMonsterDamage(attack, damage);

        Assert.Equal(attack, outcome.Attack);
        Assert.Equal(100, outcome.Damage.AppliedDamageUnits);
        Assert.True(outcome.Damage.Defeated);
        Assert.Throws<ArgumentException>(() => new Draft0AppliedMonsterDamage(
            attack,
            AuthoritativeIntegerDamage.Apply(100, 100)));
    }
}
