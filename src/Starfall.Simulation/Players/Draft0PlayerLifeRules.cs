using Starfall.Content.Characters;
using Starfall.Content.Zones;
using Starfall.Simulation.Combat;
using Starfall.Simulation.Entities;
using Starfall.Simulation.Monsters;

namespace Starfall.Simulation.Players;

public enum Draft0PlayerLifeStatus
{
    Active,
    Defeated,
}

public sealed class Draft0PlayerLifeTuning
{
    public const ulong Draft0RespawnDelayTicks = 180;

    public Draft0PlayerLifeTuning(
        int maximumHealthUnits,
        int restoredHealthUnits,
        ulong respawnDelayTicks)
    {
        if (maximumHealthUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumHealthUnits));
        if (restoredHealthUnits <= 0 || restoredHealthUnits > maximumHealthUnits)
            throw new ArgumentOutOfRangeException(nameof(restoredHealthUnits));
        if (respawnDelayTicks == 0)
            throw new ArgumentOutOfRangeException(nameof(respawnDelayTicks));

        MaximumHealthUnits = maximumHealthUnits;
        RestoredHealthUnits = restoredHealthUnits;
        RespawnDelayTicks = respawnDelayTicks;
    }

    public int MaximumHealthUnits
    {
        get;
    }

    public int RestoredHealthUnits
    {
        get;
    }

    public ulong RespawnDelayTicks
    {
        get;
    }

    public static Draft0PlayerLifeTuning FirstPlayable { get; } = CreateFirstPlayable();

    private static Draft0PlayerLifeTuning CreateFirstPlayable()
    {
        int healthUnits = Draft0ArcherCatalog.FirstPlayable.InitialHealthUnits;
        return new(healthUnits, healthUnits, Draft0RespawnDelayTicks);
    }
}

public readonly record struct Draft0AppliedMonsterDamage
{
    public Draft0AppliedMonsterDamage(
        Draft0MonsterAttackResolution attack,
        AuthoritativeDamageResult damage)
    {
        if (attack.TargetEntityId.Value == 0)
            throw new ArgumentException("Damage requires a valid player target.", nameof(attack));
        if (attack.RequestedDamageUnits != damage.RequestedDamageUnits)
            throw new ArgumentException("Attack request and applied damage must agree.", nameof(damage));

        Attack = attack;
        Damage = damage;
    }

    public Draft0MonsterAttackResolution Attack
    {
        get;
    }

    public AuthoritativeDamageResult Damage
    {
        get;
    }
}

public readonly record struct Draft0PlayerRespawnOutcome
{
    public Draft0PlayerRespawnOutcome(
        WorldEntityId playerEntityId,
        ulong respawnedAtTick,
        GroundPoint position,
        int restoredHealthUnits)
    {
        if (playerEntityId.Value == 0)
            throw new ArgumentException("Respawn requires a valid player identity.", nameof(playerEntityId));
        if (restoredHealthUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(restoredHealthUnits));

        PlayerEntityId = playerEntityId;
        RespawnedAtTick = respawnedAtTick;
        Position = position;
        RestoredHealthUnits = restoredHealthUnits;
    }

    public WorldEntityId PlayerEntityId
    {
        get;
    }

    public ulong RespawnedAtTick
    {
        get;
    }

    public GroundPoint Position
    {
        get;
    }

    public int RestoredHealthUnits
    {
        get;
    }
}

public static class Draft0PlayerLifeRules
{
    public static ulong ScheduleRespawn(
        Draft0PlayerLifeTuning tuning,
        ulong defeatedAtTick)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        return checked(defeatedAtTick + tuning.RespawnDelayTicks);
    }

    public static bool IsHostileActionBlocked(
        Draft0TownLayout town,
        GroundPoint actorPosition)
    {
        ArgumentNullException.ThrowIfNull(town);
        return town.Bounds.Contains(actorPosition);
    }
}
