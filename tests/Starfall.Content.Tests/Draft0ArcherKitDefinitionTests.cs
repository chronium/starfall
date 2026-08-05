using Starfall.Content.Characters;

namespace Starfall.Content.Tests;

public sealed class Draft0ArcherKitDefinitionTests
{
    [Fact]
    public void FirstPlayableRecordsTheApprovedKitInStableActionOrder()
    {
        Draft0ArcherKitDefinition kit = Draft0ArcherCatalog.FirstPlayable;

        Assert.Equal("dark_elf_archer", kit.ClassId);
        Assert.Equal(2_500, kit.InitialHealthUnits);
        Assert.Equal(Draft0AmmunitionPolicy.Unlimited, kit.AmmunitionPolicy);
        Assert.Collection(
            kit.Actions,
            action => AssertAction(action, "basic_arrow", Draft0ActionTargetKind.SelectedEntity, 300, usesMana: false),
            action => AssertAction(action, "fire_arrow", Draft0ActionTargetKind.SelectedEntity, 700, usesMana: true),
            action => AssertAction(action, "arrow_rain", Draft0ActionTargetKind.GroundCircle, 500, usesMana: true));
    }

    [Fact]
    public void CatalogUsesTheFrozenIntegerResourceAndProbabilityScales()
    {
        Assert.Equal(100, Draft0ArcherCatalog.ResourceUnitsPerDisplayedPoint);
        Assert.Equal(10_000, Draft0ArcherCatalog.FullProbabilityBasisPoints);
        Assert.Equal(2_500, 25 * Draft0ArcherCatalog.ResourceUnitsPerDisplayedPoint);
        Assert.Equal(300, 3 * Draft0ArcherCatalog.ResourceUnitsPerDisplayedPoint);
        Assert.Equal(500, 5 * Draft0ArcherCatalog.ResourceUnitsPerDisplayedPoint);
        Assert.Equal(700, 7 * Draft0ArcherCatalog.ResourceUnitsPerDisplayedPoint);
    }

    [Fact]
    public void KitCopiesTheActionInputIntoAnImmutableArray()
    {
        Draft0ActionDefinition action = CreateAction("test_action");
        var actions = new List<Draft0ActionDefinition> { action };

        Draft0ArcherKitDefinition kit = CreateKit(actions);
        actions.Clear();

        Assert.Single(kit.Actions);
        Assert.Same(action, kit.Actions[0]);
    }

    [Fact]
    public void DefinitionsRejectInvalidIdentitiesValuesAndEnums()
    {
        Assert.Throws<ArgumentException>(() => CreateAction("Bad-Action"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Draft0ActionDefinition("test_action", (Draft0ActionTargetKind)99, 100, false));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Draft0ActionDefinition("test_action", Draft0ActionTargetKind.SelectedEntity, 0, false));
        Assert.Throws<ArgumentException>(() =>
            new Draft0ArcherKitDefinition("Bad-Class", 100, Draft0AmmunitionPolicy.Unlimited, [CreateAction("action_one")]));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Draft0ArcherKitDefinition("test_class", 0, Draft0AmmunitionPolicy.Unlimited, [CreateAction("action_one")]));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Draft0ArcherKitDefinition("test_class", 100, (Draft0AmmunitionPolicy)99, [CreateAction("action_one")]));
    }

    [Fact]
    public void KitRejectsMissingNullAndDuplicateActions()
    {
        Assert.Throws<ArgumentNullException>(() => CreateKit(null!));
        Assert.Throws<ArgumentException>(() => CreateKit([]));
        Assert.Throws<ArgumentException>(() => CreateKit([null!]));
        Assert.Throws<ArgumentException>(() => CreateKit([CreateAction("same_action"), CreateAction("same_action")]));
    }

    private static Draft0ActionDefinition CreateAction(string id) => new(
        id,
        Draft0ActionTargetKind.SelectedEntity,
        authoritativeDamageUnits: 100,
        usesMana: false);

    private static Draft0ArcherKitDefinition CreateKit(IEnumerable<Draft0ActionDefinition> actions) => new(
        "test_class",
        initialHealthUnits: 100,
        Draft0AmmunitionPolicy.Unlimited,
        actions);

    private static void AssertAction(
        Draft0ActionDefinition action,
        string id,
        Draft0ActionTargetKind targetKind,
        int damageUnits,
        bool usesMana)
    {
        Assert.Equal(id, action.Id);
        Assert.Equal(targetKind, action.TargetKind);
        Assert.Equal(damageUnits, action.AuthoritativeDamageUnits);
        Assert.Equal(usesMana, action.UsesMana);
    }
}
