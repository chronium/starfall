using Starfall.Content.Characters;

namespace Starfall.Content.Combat;

public sealed class Draft0StraightProjectileActionDefinition
{
    public Draft0StraightProjectileActionDefinition(
        Draft0ActionDefinition action,
        ulong releaseDelayTicks,
        float speedMetresPerSecond,
        float projectileRadiusMetres,
        float maximumTravelMetres,
        float selectionRangeMetres,
        float minimumFacingDot,
        ulong cadenceTicks)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action.TargetKind != Draft0ActionTargetKind.SelectedEntity)
            throw new ArgumentException("A straight projectile action must target a selected entity.", nameof(action));
        if (releaseDelayTicks == 0)
            throw new ArgumentOutOfRangeException(nameof(releaseDelayTicks));
        ValidatePositiveFinite(speedMetresPerSecond, nameof(speedMetresPerSecond));
        ValidatePositiveFinite(projectileRadiusMetres, nameof(projectileRadiusMetres));
        ValidatePositiveFinite(maximumTravelMetres, nameof(maximumTravelMetres));
        ValidatePositiveFinite(selectionRangeMetres, nameof(selectionRangeMetres));
        if (maximumTravelMetres < selectionRangeMetres)
        {
            throw new ArgumentException(
                "Maximum travel must reach every position accepted by the selection range.",
                nameof(maximumTravelMetres));
        }
        if (!float.IsFinite(minimumFacingDot) || minimumFacingDot < 0.0f || minimumFacingDot > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(minimumFacingDot));
        if (cadenceTicks == 0)
            throw new ArgumentOutOfRangeException(nameof(cadenceTicks));

        Action = action;
        ReleaseDelayTicks = releaseDelayTicks;
        SpeedMetresPerSecond = speedMetresPerSecond;
        ProjectileRadiusMetres = projectileRadiusMetres;
        MaximumTravelMetres = maximumTravelMetres;
        SelectionRangeMetres = selectionRangeMetres;
        MinimumFacingDot = minimumFacingDot;
        CadenceTicks = cadenceTicks;
    }

    public Draft0ActionDefinition Action { get; }

    public ulong ReleaseDelayTicks { get; }

    public float SpeedMetresPerSecond { get; }

    public float ProjectileRadiusMetres { get; }

    public float MaximumTravelMetres { get; }

    public float SelectionRangeMetres { get; }

    public float MinimumFacingDot { get; }

    public ulong CadenceTicks { get; }

    private static void ValidatePositiveFinite(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value <= 0.0f)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}

public static class Draft0StraightProjectileCatalog
{
    public static Draft0StraightProjectileActionDefinition BasicArrow { get; } = CreateBasicArrow();

    private static Draft0StraightProjectileActionDefinition CreateBasicArrow()
    {
        Draft0ActionDefinition action = Draft0ArcherCatalog.FirstPlayable.Actions
            .Single(static candidate => string.Equals(candidate.Id, "basic_arrow", StringComparison.Ordinal));

        return new(
            action,
            releaseDelayTicks: 18,
            speedMetresPerSecond: 60.0f,
            projectileRadiusMetres: 0.05f,
            maximumTravelMetres: 12.0f,
            selectionRangeMetres: 12.0f,
            minimumFacingDot: 0.70710677f,
            cadenceTicks: 48);
    }
}
