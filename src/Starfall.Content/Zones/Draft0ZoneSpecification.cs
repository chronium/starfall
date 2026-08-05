namespace Starfall.Content.Zones;

public enum Draft0TownPlacement
{
    NearMapEdge,
}

public enum Draft0CampGeometry
{
    BroadOpenCircle,
    ElongatedOrDivided,
    TightBowlOrConstrainedApproach,
}

public sealed class Draft0BranchSpecification
{
    public Draft0BranchSpecification(
        string id,
        float approximateTravelDistanceMetres,
        Draft0CampGeometry campGeometry)
    {
        ContentIdentityRules.Validate(id, nameof(id));
        if (!float.IsFinite(approximateTravelDistanceMetres) || approximateTravelDistanceMetres <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(approximateTravelDistanceMetres),
                "Approximate travel distance must be a positive finite metre value.");
        }
        if (!Enum.IsDefined(campGeometry))
            throw new ArgumentOutOfRangeException(nameof(campGeometry));

        Id = id;
        ApproximateTravelDistanceMetres = approximateTravelDistanceMetres;
        CampGeometry = campGeometry;
    }

    public string Id
    {
        get;
    }

    public float ApproximateTravelDistanceMetres
    {
        get;
    }

    public Draft0CampGeometry CampGeometry
    {
        get;
    }

}

public sealed class Draft0TownSpecification
{
    public Draft0TownSpecification(
        GroundDimensions approximateDimensions,
        Draft0TownPlacement placement,
        bool isProtected,
        bool requiresRespawnAnchor,
        int minimumLandmarkCount,
        int maximumLandmarkCount,
        int exitCount)
    {
        if (!Enum.IsDefined(placement))
            throw new ArgumentOutOfRangeException(nameof(placement));
        if (minimumLandmarkCount < 0 || maximumLandmarkCount < minimumLandmarkCount)
            throw new ArgumentOutOfRangeException(nameof(minimumLandmarkCount), "Landmark count range is invalid.");
        if (exitCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(exitCount));

        ApproximateDimensions = approximateDimensions;
        Placement = placement;
        IsProtected = isProtected;
        RequiresRespawnAnchor = requiresRespawnAnchor;
        MinimumLandmarkCount = minimumLandmarkCount;
        MaximumLandmarkCount = maximumLandmarkCount;
        ExitCount = exitCount;
    }

    public GroundDimensions ApproximateDimensions
    {
        get;
    }

    public Draft0TownPlacement Placement
    {
        get;
    }

    public bool IsProtected
    {
        get;
    }

    public bool RequiresRespawnAnchor
    {
        get;
    }

    public int MinimumLandmarkCount
    {
        get;
    }

    public int MaximumLandmarkCount
    {
        get;
    }

    public int ExitCount
    {
        get;
    }
}

public sealed class Draft0EnvironmentSpecification
{
    public Draft0EnvironmentSpecification(
        string defaultSurfaceId,
        string routeSurfaceId,
        string boundaryPresentationId)
    {
        ContentIdentityRules.Validate(defaultSurfaceId, nameof(defaultSurfaceId));
        ContentIdentityRules.Validate(routeSurfaceId, nameof(routeSurfaceId));
        ContentIdentityRules.Validate(boundaryPresentationId, nameof(boundaryPresentationId));
        if (new[] { defaultSurfaceId, routeSurfaceId, boundaryPresentationId }
            .Distinct(StringComparer.Ordinal)
            .Count() != 3)
        {
            throw new ArgumentException("Environment semantic identities must be distinct.");
        }

        DefaultSurfaceId = defaultSurfaceId;
        RouteSurfaceId = routeSurfaceId;
        BoundaryPresentationId = boundaryPresentationId;
    }

    public string DefaultSurfaceId
    {
        get;
    }

    public string RouteSurfaceId
    {
        get;
    }

    public string BoundaryPresentationId
    {
        get;
    }
}

public sealed class Draft0ZoneSpecification
{
    public Draft0ZoneSpecification(
        string id,
        GroundBounds bounds,
        Draft0TownSpecification town,
        bool requiresExitJunction,
        IEnumerable<Draft0BranchSpecification> branches,
        Draft0EnvironmentSpecification environment)
    {
        ContentIdentityRules.Validate(id, nameof(id));
        ArgumentNullException.ThrowIfNull(town);
        ArgumentNullException.ThrowIfNull(branches);
        ArgumentNullException.ThrowIfNull(environment);

        Draft0BranchSpecification[] copiedBranches = branches.ToArray();
        if (copiedBranches.Length == 0 || copiedBranches.Any(static branch => branch is null))
            throw new ArgumentException("At least one non-null branch is required.", nameof(branches));
        if (copiedBranches.Select(static branch => branch.Id).Distinct(StringComparer.Ordinal).Count() != copiedBranches.Length)
            throw new ArgumentException("Branch identities must be unique.", nameof(branches));
        if (town.ApproximateDimensions.WidthMetres > bounds.Dimensions.WidthMetres ||
            town.ApproximateDimensions.DepthMetres > bounds.Dimensions.DepthMetres)
        {
            throw new ArgumentException("The target town dimensions cannot exceed the zone bounds.", nameof(town));
        }

        Id = id;
        Bounds = bounds;
        Town = town;
        RequiresExitJunction = requiresExitJunction;
        Branches = Array.AsReadOnly(copiedBranches);
        Environment = environment;
    }

    public string Id
    {
        get;
    }

    public GroundBounds Bounds
    {
        get;
    }

    public Draft0TownSpecification Town
    {
        get;
    }

    public bool RequiresExitJunction
    {
        get;
    }

    public IReadOnlyList<Draft0BranchSpecification> Branches
    {
        get;
    }

    public Draft0EnvironmentSpecification Environment
    {
        get;
    }
}

public static class Draft0ZoneCatalog
{
    public static Draft0ZoneSpecification FirstPlayable
    {
        get;
    } = new(
        "draft_0_first_playable_zone",
        new GroundBounds(new GroundPoint(0.0f, 0.0f), new GroundPoint(200.0f, 200.0f)),
        new Draft0TownSpecification(
            new GroundDimensions(50.0f, 50.0f),
            Draft0TownPlacement.NearMapEdge,
            isProtected: true,
            requiresRespawnAnchor: true,
            minimumLandmarkCount: 2,
            maximumLandmarkCount: 3,
            exitCount: 1),
        requiresExitJunction: true,
        [
            new Draft0BranchSpecification("branch_short", 25.0f, Draft0CampGeometry.BroadOpenCircle),
            new Draft0BranchSpecification("branch_medium", 45.0f, Draft0CampGeometry.ElongatedOrDivided),
            new Draft0BranchSpecification("branch_long", 70.0f, Draft0CampGeometry.TightBowlOrConstrainedApproach),
        ],
        new Draft0EnvironmentSpecification(
            "surface_grass",
            "surface_dirt_path",
            "boundary_rocks_boulders"));
}
