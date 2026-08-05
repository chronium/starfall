using System.Collections.Immutable;
using Starfall.Content.Zones;

namespace Starfall.Content.Monsters;

public sealed class Draft0MonsterArchetypeDefinition
{
    public Draft0MonsterArchetypeDefinition(string id, int authoritativeHealthUnits)
    {
        ContentIdentityRules.Validate(id, nameof(id));
        if (authoritativeHealthUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(authoritativeHealthUnits));

        Id = id;
        AuthoritativeHealthUnits = authoritativeHealthUnits;
    }

    public string Id
    {
        get;
    }

    public int AuthoritativeHealthUnits
    {
        get;
    }
}

public sealed class Draft0CampSpawnAssignment
{
    public Draft0CampSpawnAssignment(string spawnId, string archetypeId, GroundPoint point)
    {
        ContentIdentityRules.Validate(spawnId, nameof(spawnId));
        ContentIdentityRules.Validate(archetypeId, nameof(archetypeId));

        SpawnId = spawnId;
        ArchetypeId = archetypeId;
        Point = point;
    }

    public string SpawnId
    {
        get;
    }

    public string ArchetypeId
    {
        get;
    }

    public GroundPoint Point
    {
        get;
    }
}

public sealed class Draft0CampCompositionDefinition
{
    public Draft0CampCompositionDefinition(
        string campId,
        IEnumerable<Draft0CampSpawnAssignment> assignments)
    {
        ContentIdentityRules.Validate(campId, nameof(campId));
        ImmutableArray<Draft0CampSpawnAssignment> copiedAssignments = CopyRequired(
            assignments,
            nameof(assignments),
            "A camp composition requires at least one non-null assignment.");
        if (copiedAssignments
            .Select(static assignment => assignment.SpawnId)
            .Distinct(StringComparer.Ordinal)
            .Count() != copiedAssignments.Length)
        {
            throw new ArgumentException("Spawn identities must be unique within a camp.", nameof(assignments));
        }

        CampId = campId;
        Assignments = copiedAssignments;
    }

    public string CampId
    {
        get;
    }

    public ImmutableArray<Draft0CampSpawnAssignment> Assignments
    {
        get;
    }

    private static ImmutableArray<T> CopyRequired<T>(
        IEnumerable<T> values,
        string parameterName,
        string errorMessage)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        if (values is ImmutableArray<T> immutableValues && immutableValues.IsDefault)
            throw new ArgumentException("Default immutable arrays are not valid input.", parameterName);

        ImmutableArray<T> copy = values.ToImmutableArray();
        if (copy.IsEmpty || copy.Any(static value => value is null))
            throw new ArgumentException(errorMessage, parameterName);
        return copy;
    }
}

public sealed class Draft0StarterMonsterCatalogDefinition
{
    private static readonly string[] ExpectedArchetypeIds =
    [
        "starter_flyer_light",
        "starter_flyer_heavy",
    ];

    private static readonly int[] ExpectedHealthUnits = [700, 2_000];

    public Draft0StarterMonsterCatalogDefinition(
        IEnumerable<Draft0MonsterArchetypeDefinition> archetypes,
        IEnumerable<Draft0CampCompositionDefinition> camps)
    {
        ImmutableArray<Draft0MonsterArchetypeDefinition> copiedArchetypes = CopyRequired(
            archetypes,
            nameof(archetypes),
            "A starter-monster catalog requires at least one non-null archetype.");
        ImmutableArray<Draft0CampCompositionDefinition> copiedCamps = CopyRequired(
            camps,
            nameof(camps),
            "A starter-monster catalog requires at least one non-null camp composition.");

        ValidateUniqueIdentities(copiedArchetypes, copiedCamps);
        ValidateFirstPlayable(copiedArchetypes, copiedCamps);

        Archetypes = copiedArchetypes;
        Camps = copiedCamps;
    }

    public ImmutableArray<Draft0MonsterArchetypeDefinition> Archetypes
    {
        get;
    }

    public ImmutableArray<Draft0CampCompositionDefinition> Camps
    {
        get;
    }

    private static void ValidateUniqueIdentities(
        ImmutableArray<Draft0MonsterArchetypeDefinition> archetypes,
        ImmutableArray<Draft0CampCompositionDefinition> camps)
    {
        if (archetypes.Select(static archetype => archetype.Id).Distinct(StringComparer.Ordinal).Count() != archetypes.Length)
            throw new ArgumentException("Monster archetype identities must be unique.", nameof(archetypes));
        if (camps.Select(static camp => camp.CampId).Distinct(StringComparer.Ordinal).Count() != camps.Length)
            throw new ArgumentException("Camp identities must be unique.", nameof(camps));

        string[] spawnIds = camps
            .SelectMany(static camp => camp.Assignments)
            .Select(static assignment => assignment.SpawnId)
            .ToArray();
        if (spawnIds.Distinct(StringComparer.Ordinal).Count() != spawnIds.Length)
            throw new ArgumentException("Spawn identities must be unique across the catalog.", nameof(camps));
    }

    private static void ValidateFirstPlayable(
        ImmutableArray<Draft0MonsterArchetypeDefinition> archetypes,
        ImmutableArray<Draft0CampCompositionDefinition> camps)
    {
        if (archetypes.Length != ExpectedArchetypeIds.Length)
            throw new ArgumentException("The Draft 0 catalog requires exactly two starter archetypes.", nameof(archetypes));
        for (var index = 0; index < ExpectedArchetypeIds.Length; index++)
        {
            if (!string.Equals(archetypes[index].Id, ExpectedArchetypeIds[index], StringComparison.Ordinal) ||
                archetypes[index].AuthoritativeHealthUnits != ExpectedHealthUnits[index])
            {
                throw new ArgumentException("Draft 0 archetype identity, order and health must match the approved catalog.", nameof(archetypes));
            }
        }

        HashSet<string> knownArchetypes = archetypes
            .Select(static archetype => archetype.Id)
            .ToHashSet(StringComparer.Ordinal);
        if (camps.SelectMany(static camp => camp.Assignments).Any(assignment => !knownArchetypes.Contains(assignment.ArchetypeId)))
            throw new ArgumentException("Every assignment must reference a known archetype.", nameof(camps));

        Draft0GrayboxLayout graybox = Draft0GrayboxCatalog.FirstPlayable;
        if (camps.Length != graybox.Branches.Count)
            throw new ArgumentException("The Draft 0 catalog requires exactly three camp compositions.", nameof(camps));

        string[][] expectedArchetypes =
        [
            [ExpectedArchetypeIds[0], ExpectedArchetypeIds[0], ExpectedArchetypeIds[0]],
            [ExpectedArchetypeIds[0], ExpectedArchetypeIds[0], ExpectedArchetypeIds[1], ExpectedArchetypeIds[1]],
            [ExpectedArchetypeIds[1], ExpectedArchetypeIds[1], ExpectedArchetypeIds[1]],
        ];

        for (var campIndex = 0; campIndex < camps.Length; campIndex++)
        {
            Draft0CampCompositionDefinition camp = camps[campIndex];
            Draft0BranchLayout branch = graybox.Branches[campIndex];
            if (!string.Equals(camp.CampId, branch.Camp.Id, StringComparison.Ordinal))
                throw new ArgumentException("Draft 0 camp identity and order must match the executable graybox.", nameof(camps));
            if (camp.Assignments.Length != branch.SampleSpawns.Count)
                throw new ArgumentException("Draft 0 camp assignments must cover every approved sample spawn exactly once.", nameof(camps));

            for (var assignmentIndex = 0; assignmentIndex < camp.Assignments.Length; assignmentIndex++)
            {
                Draft0CampSpawnAssignment assignment = camp.Assignments[assignmentIndex];
                Draft0SampleSpawn spawn = branch.SampleSpawns[assignmentIndex];
                if (!string.Equals(assignment.SpawnId, spawn.Id, StringComparison.Ordinal) ||
                    assignment.Point != spawn.Point ||
                    !string.Equals(assignment.ArchetypeId, expectedArchetypes[campIndex][assignmentIndex], StringComparison.Ordinal))
                {
                    throw new ArgumentException("Draft 0 spawn identity, position, order and archetype must match the approved camp composition.", nameof(camps));
                }
            }
        }
    }

    private static ImmutableArray<T> CopyRequired<T>(
        IEnumerable<T> values,
        string parameterName,
        string errorMessage)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        if (values is ImmutableArray<T> immutableValues && immutableValues.IsDefault)
            throw new ArgumentException("Default immutable arrays are not valid input.", parameterName);

        ImmutableArray<T> copy = values.ToImmutableArray();
        if (copy.IsEmpty || copy.Any(static value => value is null))
            throw new ArgumentException(errorMessage, parameterName);
        return copy;
    }
}

public static class Draft0StarterMonsterCatalog
{
    public static Draft0StarterMonsterCatalogDefinition FirstPlayable
    {
        get;
    } = CreateFirstPlayable();

    private static Draft0StarterMonsterCatalogDefinition CreateFirstPlayable()
    {
        Draft0GrayboxLayout graybox = Draft0GrayboxCatalog.FirstPlayable;
        return new Draft0StarterMonsterCatalogDefinition(
            [
                new Draft0MonsterArchetypeDefinition("starter_flyer_light", 7 * Draft0GameplayScales.ResourceUnitsPerDisplayedPoint),
                new Draft0MonsterArchetypeDefinition("starter_flyer_heavy", 20 * Draft0GameplayScales.ResourceUnitsPerDisplayedPoint),
            ],
            [
                CreateCamp(graybox.Branches[0], ["starter_flyer_light", "starter_flyer_light", "starter_flyer_light"]),
                CreateCamp(graybox.Branches[1], ["starter_flyer_light", "starter_flyer_light", "starter_flyer_heavy", "starter_flyer_heavy"]),
                CreateCamp(graybox.Branches[2], ["starter_flyer_heavy", "starter_flyer_heavy", "starter_flyer_heavy"]),
            ]);
    }

    private static Draft0CampCompositionDefinition CreateCamp(
        Draft0BranchLayout branch,
        IReadOnlyList<string> archetypeIds)
    {
        var assignments = new Draft0CampSpawnAssignment[branch.SampleSpawns.Count];
        for (var index = 0; index < assignments.Length; index++)
        {
            Draft0SampleSpawn spawn = branch.SampleSpawns[index];
            assignments[index] = new Draft0CampSpawnAssignment(spawn.Id, archetypeIds[index], spawn.Point);
        }

        return new Draft0CampCompositionDefinition(branch.Camp.Id, assignments);
    }
}
