using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Simulation.Camps;
using Starfall.Simulation.Entities;
using Starfall.World.Entities;

namespace Starfall.World.Monsters;

internal sealed class WorldMonsterPopulation
{
    private readonly Draft0CampPolicyCatalogDefinition policies;
    private readonly IReadOnlyDictionary<string, Draft0MonsterArchetypeDefinition> archetypes;
    private readonly Dictionary<WorldEntityId, WorldMonsterState> monsters = [];
    private readonly Dictionary<string, WorldEntityId> occupantsBySpawnId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Draft0CampVacancy> vacanciesBySpawnId = new(StringComparer.Ordinal);
    private bool initialized;

    internal WorldMonsterPopulation(
        Draft0GrayboxLayout layout,
        Draft0StarterMonsterCatalogDefinition monsterCatalog,
        Draft0CampPolicyCatalogDefinition policies)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(monsterCatalog);
        ArgumentNullException.ThrowIfNull(policies);

        ValidateCompatibleInputs(layout, monsterCatalog, policies);
        this.policies = policies;
        archetypes = monsterCatalog.Archetypes.ToDictionary(
            static archetype => archetype.Id,
            StringComparer.Ordinal);
    }

    internal int Count => monsters.Count;

    internal IReadOnlyList<WorldMonsterState> Snapshot()
    {
        WorldMonsterState[] snapshot = monsters.Values
            .OrderBy(static monster => monster.EntityId.Value)
            .ToArray();
        return Array.AsReadOnly(snapshot);
    }

    internal void Initialize(ulong currentTick, Func<WorldEntityId> allocateEntityId)
    {
        ArgumentNullException.ThrowIfNull(allocateEntityId);
        if (initialized)
            throw new InvalidOperationException("The world monster population is already initialized.");

        var initialPopulation = new List<WorldMonsterState>(
            policies.Camps.Sum(static policy => policy.InitialPopulationCount));
        foreach (Draft0CampPolicyDefinition policy in policies.Camps)
        {
            foreach (Draft0CampSpawnAssignment assignment in policy.PlacementSlots)
            {
                initialPopulation.Add(CreateMonster(
                    allocateEntityId(),
                    policy.Camp.Id,
                    assignment,
                    currentTick));
            }
        }

        foreach (WorldMonsterState monster in initialPopulation)
        {
            monsters.Add(monster.EntityId, monster);
            occupantsBySpawnId.Add(monster.SpawnId, monster.EntityId);
        }

        initialized = true;
    }

    internal bool TryGet(WorldEntityId entityId, out WorldMonsterState? monster) =>
        monsters.TryGetValue(entityId, out monster);

    internal bool Remove(WorldEntityId entityId, ulong removedAtTick)
    {
        RequireInitialized();
        if (!monsters.TryGetValue(entityId, out WorldMonsterState? monster))
            return false;

        var vacancy = new Draft0CampVacancy(monster.CampId, monster.SpawnId, removedAtTick);
        Draft0CampVacancy[] candidateVacancies = vacanciesBySpawnId.Values
            .Append(vacancy)
            .ToArray();

        // Validate the complete pending set, including checked eligibility arithmetic,
        // before changing authoritative occupancy.
        _ = Draft0CampReplenishmentSchedule.Create(policies, candidateVacancies);

        if (!monsters.Remove(entityId) || !occupantsBySpawnId.Remove(monster.SpawnId))
            throw new InvalidOperationException("Monster entity and placement-slot ownership diverged.");
        vacanciesBySpawnId.Add(monster.SpawnId, vacancy);
        return true;
    }

    internal void ApplyEligible(ulong currentTick, Func<WorldEntityId> allocateEntityId)
    {
        ArgumentNullException.ThrowIfNull(allocateEntityId);
        RequireInitialized();

        IReadOnlyList<Draft0CampReplenishmentDecision> decisions =
            Draft0CampReplenishmentSchedule.Create(policies, vacanciesBySpawnId.Values);
        foreach (Draft0CampReplenishmentDecision decision in decisions)
        {
            if (decision.EligibleAtTick > currentTick)
                break;
            if (occupantsBySpawnId.ContainsKey(decision.Assignment.SpawnId))
            {
                throw new InvalidOperationException(
                    $"Placement slot '{decision.Assignment.SpawnId}' is already occupied.");
            }

            WorldMonsterState monster = CreateMonster(
                allocateEntityId(),
                decision.CampId,
                decision.Assignment,
                currentTick);
            monsters.Add(monster.EntityId, monster);
            occupantsBySpawnId.Add(monster.SpawnId, monster.EntityId);
            if (!vacanciesBySpawnId.Remove(monster.SpawnId))
                throw new InvalidOperationException("Replenished placement slot had no pending vacancy.");
        }
    }

    internal void Clear()
    {
        monsters.Clear();
        occupantsBySpawnId.Clear();
        vacanciesBySpawnId.Clear();
    }

    private WorldMonsterState CreateMonster(
        WorldEntityId entityId,
        string campId,
        Draft0CampSpawnAssignment assignment,
        ulong spawnedAtTick)
    {
        if (!archetypes.TryGetValue(assignment.ArchetypeId, out Draft0MonsterArchetypeDefinition? archetype))
        {
            throw new InvalidOperationException(
                $"Placement slot '{assignment.SpawnId}' references unknown archetype '{assignment.ArchetypeId}'.");
        }

        return new WorldMonsterState(
            entityId,
            campId,
            assignment.SpawnId,
            assignment.ArchetypeId,
            assignment.Point,
            archetype.AuthoritativeHealthUnits,
            spawnedAtTick);
    }

    private void RequireInitialized()
    {
        if (!initialized)
            throw new InvalidOperationException("The world monster population is not initialized.");
    }

    private static void ValidateCompatibleInputs(
        Draft0GrayboxLayout layout,
        Draft0StarterMonsterCatalogDefinition monsterCatalog,
        Draft0CampPolicyCatalogDefinition policies)
    {
        if (layout.Branches.Count != policies.Camps.Length ||
            monsterCatalog.Camps.Length != policies.Camps.Length)
        {
            throw new ArgumentException("World layout, monster catalog and camp policies must describe the same camps.");
        }

        for (var campIndex = 0; campIndex < policies.Camps.Length; campIndex++)
        {
            Draft0BranchLayout branch = layout.Branches[campIndex];
            Draft0CampCompositionDefinition composition = monsterCatalog.Camps[campIndex];
            Draft0CampPolicyDefinition policy = policies.Camps[campIndex];
            if (!string.Equals(branch.Camp.Id, policy.Camp.Id, StringComparison.Ordinal) ||
                !string.Equals(composition.CampId, policy.Camp.Id, StringComparison.Ordinal) ||
                branch.Camp.Bounds != policy.Camp.Bounds ||
                branch.Camp.EntryAnchor != policy.Camp.EntryAnchor ||
                branch.Camp.Geometry != policy.Camp.Geometry ||
                branch.SampleSpawns.Count != policy.PlacementSlots.Length ||
                composition.Assignments.Length != policy.PlacementSlots.Length)
            {
                throw new ArgumentException("World layout, monster catalog and camp policies are not structurally compatible.");
            }

            for (var slotIndex = 0; slotIndex < policy.PlacementSlots.Length; slotIndex++)
            {
                Draft0SampleSpawn sample = branch.SampleSpawns[slotIndex];
                Draft0CampSpawnAssignment compositionAssignment = composition.Assignments[slotIndex];
                Draft0CampSpawnAssignment policyAssignment = policy.PlacementSlots[slotIndex];
                if (!string.Equals(sample.Id, policyAssignment.SpawnId, StringComparison.Ordinal) ||
                    sample.Point != policyAssignment.Point ||
                    !string.Equals(compositionAssignment.SpawnId, policyAssignment.SpawnId, StringComparison.Ordinal) ||
                    !string.Equals(compositionAssignment.ArchetypeId, policyAssignment.ArchetypeId, StringComparison.Ordinal) ||
                    compositionAssignment.Point != policyAssignment.Point)
                {
                    throw new ArgumentException("World monster placement inputs are not structurally compatible.");
                }
            }
        }
    }
}
