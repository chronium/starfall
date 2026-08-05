using System.Collections.Immutable;

namespace Starfall.Content.Characters;

public enum Draft0ActionTargetKind
{
    SelectedEntity,
    GroundCircle,
}

public enum Draft0AmmunitionPolicy
{
    Unlimited,
}

public sealed class Draft0ActionDefinition
{
    public Draft0ActionDefinition(
        string id,
        Draft0ActionTargetKind targetKind,
        int authoritativeDamageUnits,
        bool usesMana)
    {
        ContentIdentityRules.Validate(id, nameof(id));
        if (!Enum.IsDefined(targetKind))
            throw new ArgumentOutOfRangeException(nameof(targetKind));
        if (authoritativeDamageUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(authoritativeDamageUnits));

        Id = id;
        TargetKind = targetKind;
        AuthoritativeDamageUnits = authoritativeDamageUnits;
        UsesMana = usesMana;
    }

    public string Id
    {
        get;
    }

    public Draft0ActionTargetKind TargetKind
    {
        get;
    }

    public int AuthoritativeDamageUnits
    {
        get;
    }

    public bool UsesMana
    {
        get;
    }
}

public sealed class Draft0ArcherKitDefinition
{
    public Draft0ArcherKitDefinition(
        string classId,
        int initialHealthUnits,
        Draft0AmmunitionPolicy ammunitionPolicy,
        IEnumerable<Draft0ActionDefinition> actions)
    {
        ContentIdentityRules.Validate(classId, nameof(classId));
        if (initialHealthUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(initialHealthUnits));
        if (!Enum.IsDefined(ammunitionPolicy))
            throw new ArgumentOutOfRangeException(nameof(ammunitionPolicy));
        ArgumentNullException.ThrowIfNull(actions);

        ImmutableArray<Draft0ActionDefinition> copiedActions = actions.ToImmutableArray();
        if (copiedActions.IsEmpty || copiedActions.Any(static action => action is null))
            throw new ArgumentException("At least one non-null action is required.", nameof(actions));
        if (copiedActions
            .Select(static action => action.Id)
            .Distinct(StringComparer.Ordinal)
            .Count() != copiedActions.Length)
        {
            throw new ArgumentException("Action identities must be unique.", nameof(actions));
        }

        ClassId = classId;
        InitialHealthUnits = initialHealthUnits;
        AmmunitionPolicy = ammunitionPolicy;
        Actions = copiedActions;
    }

    public string ClassId
    {
        get;
    }

    public int InitialHealthUnits
    {
        get;
    }

    public Draft0AmmunitionPolicy AmmunitionPolicy
    {
        get;
    }

    public ImmutableArray<Draft0ActionDefinition> Actions
    {
        get;
    }
}

public static class Draft0ArcherCatalog
{
    public static Draft0ArcherKitDefinition FirstPlayable
    {
        get;
    } = new(
        "dark_elf_archer",
        initialHealthUnits: 2_500,
        Draft0AmmunitionPolicy.Unlimited,
        [
            new Draft0ActionDefinition(
                "basic_arrow",
                Draft0ActionTargetKind.SelectedEntity,
                authoritativeDamageUnits: 300,
                usesMana: false),
            new Draft0ActionDefinition(
                "fire_arrow",
                Draft0ActionTargetKind.SelectedEntity,
                authoritativeDamageUnits: 700,
                usesMana: true),
            new Draft0ActionDefinition(
                "arrow_rain",
                Draft0ActionTargetKind.GroundCircle,
                authoritativeDamageUnits: 500,
                usesMana: true),
        ]);
}
