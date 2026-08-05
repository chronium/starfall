using System.Collections.Immutable;
using Starfall.Content.Monsters;

namespace Starfall.Simulation.Camps;

public readonly record struct Draft0CampVacancy
{
    public Draft0CampVacancy(string campId, string spawnId, ulong removedAtTick)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campId);
        ArgumentException.ThrowIfNullOrWhiteSpace(spawnId);

        CampId = campId;
        SpawnId = spawnId;
        RemovedAtTick = removedAtTick;
    }

    public string CampId
    {
        get;
    }

    public string SpawnId
    {
        get;
    }

    public ulong RemovedAtTick
    {
        get;
    }
}

public sealed class Draft0CampReplenishmentDecision
{
    internal Draft0CampReplenishmentDecision(
        string campId,
        Draft0CampSpawnAssignment assignment,
        ulong eligibleAtTick)
    {
        CampId = campId;
        Assignment = assignment;
        EligibleAtTick = eligibleAtTick;
    }

    public string CampId
    {
        get;
    }

    public Draft0CampSpawnAssignment Assignment
    {
        get;
    }

    public ulong EligibleAtTick
    {
        get;
    }
}

public static class Draft0CampReplenishmentSchedule
{
    public static ImmutableArray<Draft0CampReplenishmentDecision> Create(
        Draft0CampPolicyCatalogDefinition catalog,
        IEnumerable<Draft0CampVacancy> vacancies)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(vacancies);
        if (vacancies is ImmutableArray<Draft0CampVacancy> immutableVacancies && immutableVacancies.IsDefault)
            throw new ArgumentException("Default immutable arrays are not valid input.", nameof(vacancies));

        Draft0CampVacancy[] copiedVacancies = vacancies.ToArray();
        var seenSlots = new HashSet<CampSlotIdentity>();
        var pending = new List<PendingDecision>(copiedVacancies.Length);
        foreach (Draft0CampVacancy vacancy in copiedVacancies)
        {
            if (string.IsNullOrWhiteSpace(vacancy.CampId) || string.IsNullOrWhiteSpace(vacancy.SpawnId))
                throw new ArgumentException("Vacancies require camp and spawn identities.", nameof(vacancies));
            if (!seenSlots.Add(new CampSlotIdentity(vacancy.CampId, vacancy.SpawnId)))
                throw new ArgumentException("A placement slot can appear at most once in a vacancy set.", nameof(vacancies));

            int campIndex = FindCamp(catalog, vacancy.CampId);
            Draft0CampPolicyDefinition policy = catalog.Camps[campIndex];
            int slotIndex = FindSlot(policy, vacancy.SpawnId);
            ulong eligibleAtTick = checked(vacancy.RemovedAtTick + policy.ReplenishmentDelayTicks);
            pending.Add(new PendingDecision(
                new Draft0CampReplenishmentDecision(
                    policy.Camp.Id,
                    policy.PlacementSlots[slotIndex],
                    eligibleAtTick),
                campIndex,
                slotIndex));
        }

        return pending
            .OrderBy(static item => item.Decision.EligibleAtTick)
            .ThenBy(static item => item.CampIndex)
            .ThenBy(static item => item.SlotIndex)
            .Select(static item => item.Decision)
            .ToImmutableArray();
    }

    private static int FindCamp(Draft0CampPolicyCatalogDefinition catalog, string campId)
    {
        for (var index = 0; index < catalog.Camps.Length; index++)
        {
            if (string.Equals(catalog.Camps[index].Camp.Id, campId, StringComparison.Ordinal))
                return index;
        }

        throw new ArgumentException($"Unknown camp identity '{campId}'.", nameof(campId));
    }

    private static int FindSlot(Draft0CampPolicyDefinition policy, string spawnId)
    {
        for (var index = 0; index < policy.PlacementSlots.Length; index++)
        {
            if (string.Equals(policy.PlacementSlots[index].SpawnId, spawnId, StringComparison.Ordinal))
                return index;
        }

        throw new ArgumentException(
            $"Unknown placement slot '{spawnId}' for camp '{policy.Camp.Id}'.",
            nameof(spawnId));
    }

    private readonly record struct CampSlotIdentity(string CampId, string SpawnId);

    private readonly record struct PendingDecision(
        Draft0CampReplenishmentDecision Decision,
        int CampIndex,
        int SlotIndex);
}
