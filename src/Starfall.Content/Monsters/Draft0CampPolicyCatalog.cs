using System.Collections.Immutable;
using Starfall.Content.Zones;

namespace Starfall.Content.Monsters;

public sealed class Draft0CampPolicyDefinition
{
    public Draft0CampPolicyDefinition(
        Draft0CampLayout camp,
        Draft0CampCompositionDefinition initialComposition,
        int capacity,
        ulong replenishmentDelayTicks,
        ulong authoritativeSeed)
    {
        ArgumentNullException.ThrowIfNull(camp);
        ArgumentNullException.ThrowIfNull(initialComposition);
        if (!string.Equals(camp.Id, initialComposition.CampId, StringComparison.Ordinal))
            throw new ArgumentException("Camp layout and composition identities must match.", nameof(initialComposition));
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        if (capacity != initialComposition.Assignments.Length)
        {
            throw new ArgumentException(
                "Draft 0 fixed-slot capacity must equal its initial population and placement-slot count.",
                nameof(capacity));
        }
        if (replenishmentDelayTicks == 0)
            throw new ArgumentOutOfRangeException(nameof(replenishmentDelayTicks));
        if (initialComposition.Assignments.Any(assignment => !camp.Contains(assignment.Point)))
            throw new ArgumentException("Every placement slot must lie inside its camp footprint.", nameof(initialComposition));

        Camp = camp;
        InitialComposition = initialComposition;
        Capacity = capacity;
        ReplenishmentDelayTicks = replenishmentDelayTicks;
        AuthoritativeSeed = authoritativeSeed;
    }

    public Draft0CampLayout Camp
    {
        get;
    }

    public Draft0CampCompositionDefinition InitialComposition
    {
        get;
    }

    public int Capacity
    {
        get;
    }

    public int InitialPopulationCount => InitialComposition.Assignments.Length;

    public ImmutableArray<Draft0CampSpawnAssignment> PlacementSlots => InitialComposition.Assignments;

    public ulong ReplenishmentDelayTicks
    {
        get;
    }

    public ulong AuthoritativeSeed
    {
        get;
    }
}

public sealed class Draft0CampPolicyCatalogDefinition
{
    private static readonly int[] ExpectedCapacities = [3, 4, 3];
    private static readonly ulong[] ExpectedSeeds = [1, 2, 3];

    public Draft0CampPolicyCatalogDefinition(IEnumerable<Draft0CampPolicyDefinition> camps)
    {
        ArgumentNullException.ThrowIfNull(camps);
        if (camps is ImmutableArray<Draft0CampPolicyDefinition> immutableCamps && immutableCamps.IsDefault)
            throw new ArgumentException("Default immutable arrays are not valid input.", nameof(camps));

        ImmutableArray<Draft0CampPolicyDefinition> copy = camps.ToImmutableArray();
        if (copy.IsEmpty || copy.Any(static camp => camp is null))
            throw new ArgumentException("A camp-policy catalog requires non-null camp policies.", nameof(camps));
        if (copy.Select(static camp => camp.Camp.Id).Distinct(StringComparer.Ordinal).Count() != copy.Length)
            throw new ArgumentException("Camp-policy identities must be unique.", nameof(camps));
        if (copy
            .SelectMany(static camp => camp.PlacementSlots)
            .Select(static assignment => assignment.SpawnId)
            .Distinct(StringComparer.Ordinal)
            .Count() != copy.Sum(static camp => camp.PlacementSlots.Length))
        {
            throw new ArgumentException("Placement-slot identities must be unique across the catalog.", nameof(camps));
        }

        ValidateFirstPlayable(copy);
        Camps = copy;
    }

    public ImmutableArray<Draft0CampPolicyDefinition> Camps
    {
        get;
    }

    private static void ValidateFirstPlayable(ImmutableArray<Draft0CampPolicyDefinition> camps)
    {
        Draft0GrayboxLayout graybox = Draft0GrayboxCatalog.FirstPlayable;
        Draft0StarterMonsterCatalogDefinition monsters = Draft0StarterMonsterCatalog.FirstPlayable;
        if (camps.Length != graybox.Branches.Count || camps.Length != monsters.Camps.Length)
            throw new ArgumentException("The Draft 0 policy catalog requires exactly three camps.", nameof(camps));

        for (var campIndex = 0; campIndex < camps.Length; campIndex++)
        {
            Draft0CampPolicyDefinition policy = camps[campIndex];
            Draft0CampLayout expectedCamp = graybox.Branches[campIndex].Camp;
            Draft0CampCompositionDefinition expectedComposition = monsters.Camps[campIndex];
            if (!SameCamp(policy.Camp, expectedCamp) ||
                policy.Capacity != ExpectedCapacities[campIndex] ||
                policy.InitialPopulationCount != ExpectedCapacities[campIndex] ||
                policy.ReplenishmentDelayTicks != Draft0CampPolicyCatalog.ReplenishmentDelayTicks ||
                policy.AuthoritativeSeed != ExpectedSeeds[campIndex] ||
                !SameComposition(policy.InitialComposition, expectedComposition))
            {
                throw new ArgumentException(
                    "Draft 0 camp order, geometry, population, timing, seed and placements must match the approved policy.",
                    nameof(camps));
            }
        }
    }

    private static bool SameCamp(Draft0CampLayout left, Draft0CampLayout right) =>
        string.Equals(left.Id, right.Id, StringComparison.Ordinal) &&
        left.Bounds == right.Bounds &&
        left.EntryAnchor == right.EntryAnchor &&
        left.Geometry == right.Geometry;

    private static bool SameComposition(
        Draft0CampCompositionDefinition left,
        Draft0CampCompositionDefinition right)
    {
        if (!string.Equals(left.CampId, right.CampId, StringComparison.Ordinal) ||
            left.Assignments.Length != right.Assignments.Length)
        {
            return false;
        }

        for (var index = 0; index < left.Assignments.Length; index++)
        {
            Draft0CampSpawnAssignment candidate = left.Assignments[index];
            Draft0CampSpawnAssignment expected = right.Assignments[index];
            if (!string.Equals(candidate.SpawnId, expected.SpawnId, StringComparison.Ordinal) ||
                !string.Equals(candidate.ArchetypeId, expected.ArchetypeId, StringComparison.Ordinal) ||
                candidate.Point != expected.Point)
            {
                return false;
            }
        }

        return true;
    }
}

public static class Draft0CampPolicyCatalog
{
    public const ulong ReplenishmentDelayTicks = 600;

    public static Draft0CampPolicyCatalogDefinition FirstPlayable
    {
        get;
    } = CreateFirstPlayable();

    private static Draft0CampPolicyCatalogDefinition CreateFirstPlayable()
    {
        Draft0GrayboxLayout graybox = Draft0GrayboxCatalog.FirstPlayable;
        Draft0StarterMonsterCatalogDefinition monsters = Draft0StarterMonsterCatalog.FirstPlayable;
        return new Draft0CampPolicyCatalogDefinition(
        [
            new Draft0CampPolicyDefinition(graybox.Branches[0].Camp, monsters.Camps[0], 3, ReplenishmentDelayTicks, 1),
            new Draft0CampPolicyDefinition(graybox.Branches[1].Camp, monsters.Camps[1], 4, ReplenishmentDelayTicks, 2),
            new Draft0CampPolicyDefinition(graybox.Branches[2].Camp, monsters.Camps[2], 3, ReplenishmentDelayTicks, 3),
        ]);
    }
}
