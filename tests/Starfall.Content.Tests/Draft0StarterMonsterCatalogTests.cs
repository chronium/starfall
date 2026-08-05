using System.Collections.Immutable;
using Starfall.Content.Monsters;
using Starfall.Content.Zones;

namespace Starfall.Content.Tests;

public sealed class Draft0StarterMonsterCatalogTests
{
    [Fact]
    public void FirstPlayableFreezesArchetypesHealthAndCampAssignmentOrder()
    {
        Draft0StarterMonsterCatalogDefinition catalog = Draft0StarterMonsterCatalog.FirstPlayable;

        Assert.Collection(
            catalog.Archetypes,
            archetype => AssertArchetype(archetype, "starter_flyer_light", 700),
            archetype => AssertArchetype(archetype, "starter_flyer_heavy", 2_000));
        Assert.Equal(7 * Draft0GameplayScales.ResourceUnitsPerDisplayedPoint, catalog.Archetypes[0].AuthoritativeHealthUnits);
        Assert.Equal(20 * Draft0GameplayScales.ResourceUnitsPerDisplayedPoint, catalog.Archetypes[1].AuthoritativeHealthUnits);

        Assert.Collection(
            catalog.Camps,
            camp => AssertCamp(
                camp,
                "camp_easy",
                ("spawn_easy_01", "starter_flyer_light", Point(55.0f, 65.0f)),
                ("spawn_easy_02", "starter_flyer_light", Point(60.0f, 75.0f)),
                ("spawn_easy_03", "starter_flyer_light", Point(65.0f, 65.0f))),
            camp => AssertCamp(
                camp,
                "camp_mixed",
                ("spawn_mixed_01", "starter_flyer_light", Point(95.0f, 122.0f)),
                ("spawn_mixed_02", "starter_flyer_light", Point(105.0f, 122.0f)),
                ("spawn_mixed_03", "starter_flyer_heavy", Point(95.0f, 144.0f)),
                ("spawn_mixed_04", "starter_flyer_heavy", Point(105.0f, 144.0f))),
            camp => AssertCamp(
                camp,
                "camp_hard",
                ("spawn_hard_01", "starter_flyer_heavy", Point(140.0f, 104.0f)),
                ("spawn_hard_02", "starter_flyer_heavy", Point(150.0f, 104.0f)),
                ("spawn_hard_03", "starter_flyer_heavy", Point(145.0f, 114.0f))));

        Assert.Equal(10, catalog.Camps.Sum(static camp => camp.Assignments.Length));
        Assert.Equal(5, catalog.Camps.SelectMany(static camp => camp.Assignments).Count(IsLight));
        Assert.Equal(5, catalog.Camps.SelectMany(static camp => camp.Assignments).Count(IsHeavy));
    }

    [Fact]
    public void CatalogCopiesMutableCollectionsAndStoresOnlyAssignmentValues()
    {
        List<Draft0MonsterArchetypeDefinition> archetypes = CanonicalArchetypes().ToList();
        List<Draft0CampCompositionDefinition> camps = CanonicalCamps().ToList();
        List<Draft0CampSpawnAssignment> assignments = camps[0].Assignments.ToList();
        Draft0CampCompositionDefinition copiedCamp = new(camps[0].CampId, assignments);
        Draft0StarterMonsterCatalogDefinition catalog = new(archetypes, camps);

        archetypes.Clear();
        camps.Clear();
        assignments.Clear();

        Assert.Equal(2, catalog.Archetypes.Length);
        Assert.Equal(3, catalog.Camps.Length);
        Assert.Equal(3, copiedCamp.Assignments.Length);
        Draft0SampleSpawn grayboxSpawn = Draft0GrayboxCatalog.FirstPlayable.Branches[0].SampleSpawns[0];
        Draft0CampSpawnAssignment assignment = catalog.Camps[0].Assignments[0];
        Assert.Equal(grayboxSpawn.Id, assignment.SpawnId);
        Assert.Equal(grayboxSpawn.Point, assignment.Point);
        Assert.IsNotType<Draft0SampleSpawn>(assignment);
    }

    [Fact]
    public void StructuralDefinitionsRejectInvalidIdentitiesAndHealth()
    {
        Assert.Throws<ArgumentException>(() => new Draft0MonsterArchetypeDefinition("Bad-Flyer", 700));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Draft0MonsterArchetypeDefinition("valid_flyer", 0));
        Assert.Throws<ArgumentException>(() => new Draft0CampSpawnAssignment("Bad-Spawn", "valid_flyer", Point(1.0f, 1.0f)));
        Assert.Throws<ArgumentException>(() => new Draft0CampSpawnAssignment("valid_spawn", "Bad-Flyer", Point(1.0f, 1.0f)));
        Assert.Throws<ArgumentException>(() => new Draft0CampCompositionDefinition("Bad-Camp", [Assignment("spawn_one")]));
    }

    [Fact]
    public void CampCompositionRejectsMissingDefaultNullAndDuplicateAssignments()
    {
        Assert.Throws<ArgumentNullException>(() => new Draft0CampCompositionDefinition("camp_test", null!));
        Assert.Throws<ArgumentException>(() => new Draft0CampCompositionDefinition("camp_test", []));
        Assert.Throws<ArgumentException>(() => new Draft0CampCompositionDefinition(
            "camp_test",
            default(ImmutableArray<Draft0CampSpawnAssignment>)));
        Assert.Throws<ArgumentException>(() => new Draft0CampCompositionDefinition("camp_test", [null!]));
        Assert.Throws<ArgumentException>(() => new Draft0CampCompositionDefinition(
            "camp_test",
            [Assignment("same_spawn"), Assignment("same_spawn")]));
    }

    [Fact]
    public void AggregateRejectsMissingDefaultNullAndDuplicateCollections()
    {
        Assert.Throws<ArgumentNullException>(() => new Draft0StarterMonsterCatalogDefinition(null!, CanonicalCamps()));
        Assert.Throws<ArgumentException>(() => new Draft0StarterMonsterCatalogDefinition([], CanonicalCamps()));
        Assert.Throws<ArgumentException>(() => new Draft0StarterMonsterCatalogDefinition(
            default(ImmutableArray<Draft0MonsterArchetypeDefinition>),
            CanonicalCamps()));
        Assert.Throws<ArgumentException>(() => new Draft0StarterMonsterCatalogDefinition([null!], CanonicalCamps()));
        Assert.Throws<ArgumentNullException>(() => new Draft0StarterMonsterCatalogDefinition(CanonicalArchetypes(), null!));
        Assert.Throws<ArgumentException>(() => new Draft0StarterMonsterCatalogDefinition(
            CanonicalArchetypes(),
            default(ImmutableArray<Draft0CampCompositionDefinition>)));
        Assert.Throws<ArgumentException>(() => new Draft0StarterMonsterCatalogDefinition(
            [
                new Draft0MonsterArchetypeDefinition("starter_flyer_light", 700),
                new Draft0MonsterArchetypeDefinition("starter_flyer_light", 2_000),
            ],
            CanonicalCamps()));

        Draft0CampCompositionDefinition easy = CanonicalCamps()[0];
        Assert.Throws<ArgumentException>(() => new Draft0StarterMonsterCatalogDefinition(
            CanonicalArchetypes(),
            [easy, easy, CanonicalCamps()[2]]));
    }

    [Fact]
    public void AggregateRejectsUnknownArchetypesAndWrongCanonicalOrder()
    {
        ImmutableArray<Draft0MonsterArchetypeDefinition> archetypes = CanonicalArchetypes();
        Assert.Throws<ArgumentException>(() => new Draft0StarterMonsterCatalogDefinition(
            [archetypes[1], archetypes[0]],
            CanonicalCamps()));
        Assert.Throws<ArgumentException>(() => new Draft0StarterMonsterCatalogDefinition(
            [new Draft0MonsterArchetypeDefinition("starter_flyer_light", 701), archetypes[1]],
            CanonicalCamps()));

        ImmutableArray<Draft0CampCompositionDefinition> camps = CanonicalCamps();
        Assert.Throws<ArgumentException>(() => new Draft0StarterMonsterCatalogDefinition(
            archetypes,
            [camps[1], camps[0], camps[2]]));

        Draft0CampCompositionDefinition easy = camps[0];
        Draft0CampSpawnAssignment[] unknown = easy.Assignments.ToArray();
        unknown[0] = new Draft0CampSpawnAssignment(unknown[0].SpawnId, "unknown_flyer", unknown[0].Point);
        Assert.Throws<ArgumentException>(() => CreateWithCamp(0, new Draft0CampCompositionDefinition(easy.CampId, unknown)));

        Draft0CampSpawnAssignment[] wrongKnownArchetype = easy.Assignments.ToArray();
        wrongKnownArchetype[0] = new Draft0CampSpawnAssignment(
            wrongKnownArchetype[0].SpawnId,
            "starter_flyer_heavy",
            wrongKnownArchetype[0].Point);
        Assert.Throws<ArgumentException>(() =>
            CreateWithCamp(0, new Draft0CampCompositionDefinition(easy.CampId, wrongKnownArchetype)));
    }

    [Fact]
    public void AggregateRejectsMissingExtraRepeatedOrMovedSpawns()
    {
        Draft0CampCompositionDefinition easy = CanonicalCamps()[0];

        Assert.Throws<ArgumentException>(() => CreateWithCamp(
            0,
            new Draft0CampCompositionDefinition(easy.CampId, easy.Assignments.RemoveAt(2))));

        Assert.Throws<ArgumentException>(() => CreateWithCamp(
            0,
            new Draft0CampCompositionDefinition(
                easy.CampId,
                easy.Assignments.Add(new Draft0CampSpawnAssignment("spawn_extra", "starter_flyer_light", Point(60.0f, 70.0f))))));

        Draft0CampSpawnAssignment[] repeated = easy.Assignments.ToArray();
        repeated[1] = new Draft0CampSpawnAssignment("spawn_easy_01", "starter_flyer_light", repeated[1].Point);
        Assert.Throws<ArgumentException>(() => new Draft0CampCompositionDefinition(easy.CampId, repeated));

        Draft0CampSpawnAssignment[] moved = easy.Assignments.ToArray();
        moved[0] = new Draft0CampSpawnAssignment(moved[0].SpawnId, moved[0].ArchetypeId, Point(55.001f, 65.0f));
        Assert.Throws<ArgumentException>(() => CreateWithCamp(0, new Draft0CampCompositionDefinition(easy.CampId, moved)));
    }

    private static bool IsLight(Draft0CampSpawnAssignment assignment) =>
        assignment.ArchetypeId == "starter_flyer_light";

    private static bool IsHeavy(Draft0CampSpawnAssignment assignment) =>
        assignment.ArchetypeId == "starter_flyer_heavy";

    private static Draft0CampSpawnAssignment Assignment(string spawnId) =>
        new(spawnId, "starter_flyer_light", Point(1.0f, 1.0f));

    private static ImmutableArray<Draft0MonsterArchetypeDefinition> CanonicalArchetypes() =>
        Draft0StarterMonsterCatalog.FirstPlayable.Archetypes;

    private static ImmutableArray<Draft0CampCompositionDefinition> CanonicalCamps() =>
        Draft0StarterMonsterCatalog.FirstPlayable.Camps;

    private static Draft0StarterMonsterCatalogDefinition CreateWithCamp(
        int index,
        Draft0CampCompositionDefinition replacement)
    {
        Draft0CampCompositionDefinition[] camps = CanonicalCamps().ToArray();
        camps[index] = replacement;
        return new Draft0StarterMonsterCatalogDefinition(CanonicalArchetypes(), camps);
    }

    private static GroundPoint Point(float xMetres, float zMetres) => new(xMetres, zMetres);

    private static void AssertArchetype(
        Draft0MonsterArchetypeDefinition archetype,
        string id,
        int healthUnits)
    {
        Assert.Equal(id, archetype.Id);
        Assert.Equal(healthUnits, archetype.AuthoritativeHealthUnits);
    }

    private static void AssertCamp(
        Draft0CampCompositionDefinition camp,
        string campId,
        params (string SpawnId, string ArchetypeId, GroundPoint Point)[] expected)
    {
        Assert.Equal(campId, camp.CampId);
        Assert.Equal(expected.Length, camp.Assignments.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(expected[index].SpawnId, camp.Assignments[index].SpawnId);
            Assert.Equal(expected[index].ArchetypeId, camp.Assignments[index].ArchetypeId);
            Assert.Equal(expected[index].Point, camp.Assignments[index].Point);
        }
    }
}
