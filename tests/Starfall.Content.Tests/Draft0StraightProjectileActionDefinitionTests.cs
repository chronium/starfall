using Starfall.Content.Characters;
using Starfall.Content.Combat;

namespace Starfall.Content.Tests;

public sealed class Draft0StraightProjectileActionDefinitionTests
{
    [Fact]
    public void BasicArrowRecordsTheApprovedStraightProjectileInputs()
    {
        Draft0StraightProjectileActionDefinition definition = Draft0StraightProjectileCatalog.BasicArrow;
        Draft0ActionDefinition basicArrow = Draft0ArcherCatalog.FirstPlayable.Actions[0];

        Assert.Same(basicArrow, definition.Action);
        Assert.Equal("basic_arrow", definition.Action.Id);
        Assert.Equal(Draft0ActionTargetKind.SelectedEntity, definition.Action.TargetKind);
        Assert.Equal(300, definition.Action.AuthoritativeDamageUnits);
        Assert.False(definition.Action.UsesMana);
        Assert.Equal(18UL, definition.ReleaseDelayTicks);
        Assert.Equal(60.0f, definition.SpeedMetresPerSecond);
        Assert.Equal(0.05f, definition.ProjectileRadiusMetres);
        Assert.Equal(12.0f, definition.MaximumTravelMetres);
        Assert.Equal(12.0f, definition.SelectionRangeMetres);
        Assert.Equal(0.70710677f, definition.MinimumFacingDot);
        Assert.Equal(48UL, definition.CadenceTicks);
        Assert.Equal(Draft0AmmunitionPolicy.Unlimited, Draft0ArcherCatalog.FirstPlayable.AmmunitionPolicy);
    }

    [Fact]
    public void DefinitionRejectsMissingOrUnsupportedActionsAndZeroTicks()
    {
        Assert.Throws<ArgumentNullException>(() => new Draft0StraightProjectileActionDefinition(
            null!,
            releaseDelayTicks: 18,
            speedMetresPerSecond: 60.0f,
            projectileRadiusMetres: 0.05f,
            maximumTravelMetres: 12.0f,
            selectionRangeMetres: 12.0f,
            minimumFacingDot: 0.70710677f,
            cadenceTicks: 48));
        Assert.Throws<ArgumentException>(() => Create(action: CreateAction(Draft0ActionTargetKind.GroundCircle)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(releaseDelayTicks: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(cadenceTicks: 0));
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-1.0f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void DefinitionRejectsNonPositiveOrNonFiniteSpatialInputs(float value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(speedMetresPerSecond: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(projectileRadiusMetres: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(maximumTravelMetres: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(selectionRangeMetres: value));
    }

    [Theory]
    [InlineData(-0.01f)]
    [InlineData(1.01f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void DefinitionRejectsInvalidFacingThresholds(float value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(minimumFacingDot: value));
    }

    [Fact]
    public void DefinitionRejectsTravelThatCannotReachTheSelectionBoundary()
    {
        Assert.Throws<ArgumentException>(() => Create(maximumTravelMetres: 11.99f));
    }

    private static Draft0StraightProjectileActionDefinition Create(
        Draft0ActionDefinition? action = null,
        ulong releaseDelayTicks = 18,
        float speedMetresPerSecond = 60.0f,
        float projectileRadiusMetres = 0.05f,
        float maximumTravelMetres = 12.0f,
        float selectionRangeMetres = 12.0f,
        float minimumFacingDot = 0.70710677f,
        ulong cadenceTicks = 48) => new(
            action ?? CreateAction(Draft0ActionTargetKind.SelectedEntity),
            releaseDelayTicks,
            speedMetresPerSecond,
            projectileRadiusMetres,
            maximumTravelMetres,
            selectionRangeMetres,
            minimumFacingDot,
            cadenceTicks);

    private static Draft0ActionDefinition CreateAction(Draft0ActionTargetKind targetKind) => new(
        "test_action",
        targetKind,
        authoritativeDamageUnits: 100,
        usesMana: false);
}
