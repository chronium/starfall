using System.Numerics;

namespace Starfall.Content.Zones;

public enum Draft0ProxyRole
{
    TownLandmark,
    CampDivider,
    CampWall,
}

public sealed class Draft0TownLayout
{
    public Draft0TownLayout(
        string id,
        GroundBounds bounds,
        GroundPoint respawnAnchor,
        GroundPoint exitAnchor)
    {
        Draft0BranchSpecification.ValidateIdentity(id, nameof(id));
        if (!bounds.Contains(respawnAnchor))
            throw new ArgumentOutOfRangeException(nameof(respawnAnchor), respawnAnchor, "Respawn anchor lies outside the town.");
        if (!Draft0GrayboxLayout.IsOnBoundsBoundary(bounds, exitAnchor))
            throw new ArgumentException("Town exit anchor must lie on the town boundary.", nameof(exitAnchor));

        Id = id;
        Bounds = bounds;
        RespawnAnchor = respawnAnchor;
        ExitAnchor = exitAnchor;
    }

    public string Id
    {
        get;
    }

    public GroundBounds Bounds
    {
        get;
    }

    public GroundPoint RespawnAnchor
    {
        get;
    }

    public GroundPoint ExitAnchor
    {
        get;
    }
}

public sealed class Draft0RouteCorridor
{
    public Draft0RouteCorridor(
        string id,
        float widthMetres,
        IEnumerable<GroundPoint> points)
    {
        Draft0BranchSpecification.ValidateIdentity(id, nameof(id));
        ArgumentNullException.ThrowIfNull(points);
        if (!float.IsFinite(widthMetres) || widthMetres <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(widthMetres), "Route width must be a positive finite metre value.");

        GroundPoint[] copiedPoints = points.ToArray();
        if (copiedPoints.Length < 2)
            throw new ArgumentException("A route requires at least two points.", nameof(points));
        for (var index = 1; index < copiedPoints.Length; index++)
        {
            if (copiedPoints[index] == copiedPoints[index - 1])
                throw new ArgumentException("Consecutive route points must be distinct.", nameof(points));
        }

        Id = id;
        WidthMetres = widthMetres;
        Points = Array.AsReadOnly(copiedPoints);

        var lengthMetres = 0.0f;
        for (var index = 1; index < copiedPoints.Length; index++)
            lengthMetres += Vector3.Distance(copiedPoints[index - 1].Metres, copiedPoints[index].Metres);
        LengthMetres = lengthMetres;
    }

    public string Id
    {
        get;
    }

    public float WidthMetres
    {
        get;
    }

    public float HalfWidthMetres => WidthMetres * 0.5f;

    public IReadOnlyList<GroundPoint> Points
    {
        get;
    }

    public GroundPoint Start => Points[0];

    public GroundPoint End => Points[^1];

    public float LengthMetres
    {
        get;
    }

    public bool ContainsPresentationFootprint(GroundPoint point)
    {
        float maximumDistanceSquared = HalfWidthMetres * HalfWidthMetres;
        Vector2 candidate = ToPlane(point);
        for (var index = 1; index < Points.Count; index++)
        {
            Vector2 start = ToPlane(Points[index - 1]);
            Vector2 end = ToPlane(Points[index]);
            Vector2 segment = end - start;
            float segmentLengthSquared = segment.LengthSquared();
            float position = Vector2.Dot(candidate - start, segment) / segmentLengthSquared;
            Vector2 closest = start + (segment * Math.Clamp(position, 0.0f, 1.0f));
            if (Vector2.DistanceSquared(candidate, closest) <= maximumDistanceSquared)
                return true;
        }

        return false;
    }

    internal bool PresentationFootprintIsWithin(GroundBounds bounds) =>
        Points.All(point =>
            point.XMetres - HalfWidthMetres >= bounds.Minimum.XMetres &&
            point.XMetres + HalfWidthMetres <= bounds.Maximum.XMetres &&
            point.ZMetres - HalfWidthMetres >= bounds.Minimum.ZMetres &&
            point.ZMetres + HalfWidthMetres <= bounds.Maximum.ZMetres);

    private static Vector2 ToPlane(GroundPoint point) => new(point.XMetres, point.ZMetres);
}

public sealed class Draft0CampLayout
{
    public Draft0CampLayout(
        string id,
        GroundBounds bounds,
        GroundPoint entryAnchor,
        Draft0CampGeometry geometry)
    {
        Draft0BranchSpecification.ValidateIdentity(id, nameof(id));
        if (!Enum.IsDefined(geometry))
            throw new ArgumentOutOfRangeException(nameof(geometry));
        if (geometry == Draft0CampGeometry.BroadOpenCircle &&
            !Draft0GrayboxLayout.NearlyEqual(bounds.Dimensions.WidthMetres, bounds.Dimensions.DepthMetres))
        {
            throw new ArgumentException("Circular camp bounds must be square.", nameof(bounds));
        }
        if (!IsOnFootprintBoundary(bounds, entryAnchor, geometry))
            throw new ArgumentException("Camp entry anchor must lie on the camp footprint boundary.", nameof(entryAnchor));

        Id = id;
        Bounds = bounds;
        EntryAnchor = entryAnchor;
        Geometry = geometry;
    }

    public string Id
    {
        get;
    }

    public GroundBounds Bounds
    {
        get;
    }

    public GroundPoint EntryAnchor
    {
        get;
    }

    public Draft0CampGeometry Geometry
    {
        get;
    }

    public bool Contains(GroundPoint point)
    {
        if (!Bounds.Contains(point))
            return false;
        if (Geometry != Draft0CampGeometry.BroadOpenCircle)
            return true;

        Vector2 offset = new(point.XMetres - Center.XMetres, point.ZMetres - Center.ZMetres);
        float radiusWithTolerance = RadiusMetres + Draft0GrayboxLayout.ValidationToleranceMetres;
        return offset.LengthSquared() <= radiusWithTolerance * radiusWithTolerance;
    }

    public GroundPoint Center => new(
        (Bounds.Minimum.XMetres + Bounds.Maximum.XMetres) * 0.5f,
        (Bounds.Minimum.ZMetres + Bounds.Maximum.ZMetres) * 0.5f);

    public float RadiusMetres => Math.Min(Bounds.Dimensions.WidthMetres, Bounds.Dimensions.DepthMetres) * 0.5f;

    private static bool IsOnFootprintBoundary(
        GroundBounds bounds,
        GroundPoint point,
        Draft0CampGeometry geometry)
    {
        if (!bounds.Contains(point))
            return false;
        if (geometry != Draft0CampGeometry.BroadOpenCircle)
            return Draft0GrayboxLayout.IsOnBoundsBoundary(bounds, point);

        GroundPoint center = new(
            (bounds.Minimum.XMetres + bounds.Maximum.XMetres) * 0.5f,
            (bounds.Minimum.ZMetres + bounds.Maximum.ZMetres) * 0.5f);
        float radius = bounds.Dimensions.WidthMetres * 0.5f;
        float distance = Vector2.Distance(
            new Vector2(point.XMetres, point.ZMetres),
            new Vector2(center.XMetres, center.ZMetres));
        return Draft0GrayboxLayout.NearlyEqual(distance, radius);
    }
}

public sealed class Draft0ProxyBlock
{
    public Draft0ProxyBlock(
        string id,
        Draft0ProxyRole role,
        GroundBounds footprint,
        float heightMetres)
    {
        Draft0BranchSpecification.ValidateIdentity(id, nameof(id));
        if (!Enum.IsDefined(role))
            throw new ArgumentOutOfRangeException(nameof(role));
        if (!float.IsFinite(heightMetres) || heightMetres <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(heightMetres), "Proxy height must be a positive finite metre value.");

        Id = id;
        Role = role;
        Footprint = footprint;
        HeightMetres = heightMetres;
    }

    public string Id
    {
        get;
    }

    public Draft0ProxyRole Role
    {
        get;
    }

    public GroundBounds Footprint
    {
        get;
    }

    public float HeightMetres
    {
        get;
    }

    public bool Contains(GroundPoint point) => Footprint.Contains(point);
}

public sealed class Draft0SampleSpawn
{
    public Draft0SampleSpawn(string id, GroundPoint point)
    {
        Draft0BranchSpecification.ValidateIdentity(id, nameof(id));
        Id = id;
        Point = point;
    }

    public string Id
    {
        get;
    }

    public GroundPoint Point
    {
        get;
    }
}

public sealed class Draft0BranchLayout
{
    public Draft0BranchLayout(
        string id,
        Draft0RouteCorridor route,
        Draft0CampLayout camp,
        IEnumerable<Draft0SampleSpawn> sampleSpawns)
    {
        Draft0BranchSpecification.ValidateIdentity(id, nameof(id));
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(camp);
        ArgumentNullException.ThrowIfNull(sampleSpawns);

        Draft0SampleSpawn[] copiedSpawns = sampleSpawns.ToArray();
        if (copiedSpawns.Length == 0 || copiedSpawns.Any(static spawn => spawn is null))
            throw new ArgumentException("A branch requires at least one non-null sample spawn.", nameof(sampleSpawns));
        if (copiedSpawns.Select(static spawn => spawn.Id).Distinct(StringComparer.Ordinal).Count() != copiedSpawns.Length)
            throw new ArgumentException("Sample spawn identities must be unique within a branch.", nameof(sampleSpawns));
        if (!Draft0GrayboxLayout.NearlySamePoint(route.End, camp.EntryAnchor))
            throw new ArgumentException("Route end must match the camp entry anchor.", nameof(route));
        if (copiedSpawns.Any(spawn => !camp.Contains(spawn.Point)))
            throw new ArgumentException("Every sample spawn must lie inside its camp footprint.", nameof(sampleSpawns));

        Id = id;
        Route = route;
        Camp = camp;
        SampleSpawns = Array.AsReadOnly(copiedSpawns);
    }

    public string Id
    {
        get;
    }

    public Draft0RouteCorridor Route
    {
        get;
    }

    public Draft0CampLayout Camp
    {
        get;
    }

    public IReadOnlyList<Draft0SampleSpawn> SampleSpawns
    {
        get;
    }
}

public sealed class Draft0GrayboxLayout
{
    public const float ValidationToleranceMetres = 0.001f;

    public Draft0GrayboxLayout(
        Draft0ZoneSpecification specification,
        GroundBounds walkableBounds,
        Draft0TownLayout town,
        Draft0RouteCorridor exitRoute,
        GroundPoint junction,
        IEnumerable<Draft0BranchLayout> branches,
        IEnumerable<Draft0ProxyBlock> proxies)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(town);
        ArgumentNullException.ThrowIfNull(exitRoute);
        ArgumentNullException.ThrowIfNull(branches);
        ArgumentNullException.ThrowIfNull(proxies);

        Draft0BranchLayout[] copiedBranches = branches.ToArray();
        Draft0ProxyBlock[] copiedProxies = proxies.ToArray();
        if (copiedBranches.Length == 0 || copiedBranches.Any(static branch => branch is null))
            throw new ArgumentException("At least one non-null branch layout is required.", nameof(branches));
        if (copiedProxies.Any(static proxy => proxy is null))
            throw new ArgumentException("Proxy collections cannot contain null values.", nameof(proxies));

        RequireContainsBounds(specification.Bounds, walkableBounds, nameof(walkableBounds));
        RequireContainsBounds(walkableBounds, town.Bounds, nameof(town));
        RequireContains(walkableBounds, town.RespawnAnchor, nameof(town));
        RequireContains(walkableBounds, town.ExitAnchor, nameof(town));
        RequireContains(walkableBounds, junction, nameof(junction));
        ValidateRouteBounds(exitRoute, walkableBounds, nameof(exitRoute));
        if (!NearlySamePoint(exitRoute.Start, town.ExitAnchor) || !NearlySamePoint(exitRoute.End, junction))
            throw new ArgumentException("Exit route must connect the town exit to the junction.", nameof(exitRoute));

        if (copiedBranches.Length != specification.Branches.Count)
            throw new ArgumentException("Executable branch count must match the durable specification.", nameof(branches));
        for (var index = 0; index < copiedBranches.Length; index++)
        {
            Draft0BranchLayout branch = copiedBranches[index];
            Draft0BranchSpecification expected = specification.Branches[index];
            if (!string.Equals(branch.Id, expected.Id, StringComparison.Ordinal))
                throw new ArgumentException("Executable branch identity/order must match the durable specification.", nameof(branches));
            if (branch.Camp.Geometry != expected.CampGeometry)
                throw new ArgumentException("Executable camp geometry must match the durable specification.", nameof(branches));
            if (!NearlyEqual(branch.Route.LengthMetres, expected.ApproximateTravelDistanceMetres))
                throw new ArgumentException("Executable route length must match the approved Draft 0 distance.", nameof(branches));
            if (!NearlySamePoint(branch.Route.Start, junction))
                throw new ArgumentException("Every branch route must start at the junction.", nameof(branches));

            ValidateRouteBounds(branch.Route, walkableBounds, nameof(branches));
            RequireContainsBounds(walkableBounds, branch.Camp.Bounds, nameof(branches));
            RequireContains(walkableBounds, branch.Camp.EntryAnchor, nameof(branches));
            foreach (Draft0SampleSpawn spawn in branch.SampleSpawns)
                RequireContains(walkableBounds, spawn.Point, nameof(branches));
        }

        foreach (Draft0ProxyBlock proxy in copiedProxies)
            RequireContainsBounds(walkableBounds, proxy.Footprint, nameof(proxies));

        ValidateProxyOwnership(town, copiedBranches, copiedProxies);
        ValidateIdentities(specification, town, exitRoute, copiedBranches, copiedProxies);
        ValidateProxyClearance(town, exitRoute, junction, copiedBranches, copiedProxies);

        Specification = specification;
        WalkableBounds = walkableBounds;
        Town = town;
        ExitRoute = exitRoute;
        Junction = junction;
        Branches = Array.AsReadOnly(copiedBranches);
        Proxies = Array.AsReadOnly(copiedProxies);
    }

    public Draft0ZoneSpecification Specification
    {
        get;
    }

    public GroundBounds WalkableBounds
    {
        get;
    }

    public Draft0TownLayout Town
    {
        get;
    }

    public Draft0RouteCorridor ExitRoute
    {
        get;
    }

    public GroundPoint Junction
    {
        get;
    }

    public IReadOnlyList<Draft0BranchLayout> Branches
    {
        get;
    }

    public IReadOnlyList<Draft0ProxyBlock> Proxies
    {
        get;
    }

    internal static bool NearlyEqual(float left, float right) =>
        MathF.Abs(left - right) <= ValidationToleranceMetres;

    internal static bool NearlySamePoint(GroundPoint left, GroundPoint right) =>
        NearlyEqual(left.XMetres, right.XMetres) && NearlyEqual(left.ZMetres, right.ZMetres);

    internal static bool IsOnBoundsBoundary(GroundBounds bounds, GroundPoint point) =>
        bounds.Contains(point) &&
        (NearlyEqual(point.XMetres, bounds.Minimum.XMetres) ||
         NearlyEqual(point.XMetres, bounds.Maximum.XMetres) ||
         NearlyEqual(point.ZMetres, bounds.Minimum.ZMetres) ||
         NearlyEqual(point.ZMetres, bounds.Maximum.ZMetres));

    private static void ValidateRouteBounds(
        Draft0RouteCorridor route,
        GroundBounds walkableBounds,
        string parameterName)
    {
        if (!route.PresentationFootprintIsWithin(walkableBounds))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                route,
                "The complete thick route presentation footprint must lie inside the walkable bounds.");
        }
    }

    private static void ValidateProxyOwnership(
        Draft0TownLayout town,
        IReadOnlyList<Draft0BranchLayout> branches,
        IReadOnlyList<Draft0ProxyBlock> proxies)
    {
        Draft0CampLayout mixedCamp = RequireSingleCamp(branches, Draft0CampGeometry.ElongatedOrDivided);
        Draft0CampLayout hardCamp = RequireSingleCamp(branches, Draft0CampGeometry.TightBowlOrConstrainedApproach);

        foreach (Draft0ProxyBlock proxy in proxies)
        {
            GroundBounds owner = proxy.Role switch
            {
                Draft0ProxyRole.TownLandmark => town.Bounds,
                Draft0ProxyRole.CampDivider => mixedCamp.Bounds,
                Draft0ProxyRole.CampWall => hardCamp.Bounds,
                _ => throw new ArgumentOutOfRangeException(nameof(proxies)),
            };
            RequireContainsBounds(owner, proxy.Footprint, nameof(proxies));
        }
    }

    private static Draft0CampLayout RequireSingleCamp(
        IReadOnlyList<Draft0BranchLayout> branches,
        Draft0CampGeometry geometry)
    {
        Draft0CampLayout[] camps = branches
            .Select(static branch => branch.Camp)
            .Where(camp => camp.Geometry == geometry)
            .ToArray();
        if (camps.Length != 1)
            throw new ArgumentException($"Exactly one {geometry} camp is required.", nameof(branches));
        return camps[0];
    }

    private static void ValidateIdentities(
        Draft0ZoneSpecification specification,
        Draft0TownLayout town,
        Draft0RouteCorridor exitRoute,
        IReadOnlyList<Draft0BranchLayout> branches,
        IReadOnlyList<Draft0ProxyBlock> proxies)
    {
        var identities = new HashSet<string>(StringComparer.Ordinal);
        AddIdentity(specification.Id);
        AddIdentity(specification.Environment.DefaultSurfaceId);
        AddIdentity(specification.Environment.RouteSurfaceId);
        AddIdentity(specification.Environment.BoundaryPresentationId);
        AddIdentity(town.Id);
        AddIdentity(exitRoute.Id);
        foreach (Draft0BranchLayout branch in branches)
        {
            AddIdentity(branch.Id);
            AddIdentity(branch.Route.Id);
            AddIdentity(branch.Camp.Id);
            foreach (Draft0SampleSpawn spawn in branch.SampleSpawns)
                AddIdentity(spawn.Id);
        }
        foreach (Draft0ProxyBlock proxy in proxies)
            AddIdentity(proxy.Id);

        void AddIdentity(string identity)
        {
            if (!identities.Add(identity))
                throw new ArgumentException($"Duplicate Draft 0 identity '{identity}'.");
        }
    }

    private static void ValidateProxyClearance(
        Draft0TownLayout town,
        Draft0RouteCorridor exitRoute,
        GroundPoint junction,
        IReadOnlyList<Draft0BranchLayout> branches,
        IReadOnlyList<Draft0ProxyBlock> proxies)
    {
        var criticalAnchors = new List<GroundPoint>
        {
            town.RespawnAnchor,
            town.ExitAnchor,
            junction,
        };
        criticalAnchors.AddRange(exitRoute.Points);
        foreach (Draft0BranchLayout branch in branches)
        {
            criticalAnchors.Add(branch.Camp.EntryAnchor);
            criticalAnchors.AddRange(branch.Route.Points);
        }

        foreach (Draft0ProxyBlock proxy in proxies)
        {
            if (criticalAnchors.Any(proxy.Contains))
                throw new ArgumentException($"Critical anchor lies inside proxy '{proxy.Id}'.", nameof(proxies));
            foreach (Draft0SampleSpawn spawn in branches.SelectMany(static branch => branch.SampleSpawns))
            {
                if (proxy.Contains(spawn.Point))
                    throw new ArgumentException($"Sample spawn '{spawn.Id}' lies inside proxy '{proxy.Id}'.", nameof(proxies));
            }
        }
    }

    private static void RequireContainsBounds(
        GroundBounds outer,
        GroundBounds inner,
        string parameterName)
    {
        if (!outer.Contains(inner.Minimum) || !outer.Contains(inner.Maximum))
            throw new ArgumentOutOfRangeException(parameterName, inner, "Ground bounds lie outside their owner.");
    }

    private static void RequireContains(
        GroundBounds bounds,
        GroundPoint point,
        string parameterName)
    {
        if (!bounds.Contains(point))
            throw new ArgumentOutOfRangeException(parameterName, point, "Ground point lies outside its required bounds.");
    }
}

public static class Draft0GrayboxCatalog
{
    public static Draft0GrayboxLayout FirstPlayable
    {
        get;
    } = new(
        Draft0ZoneCatalog.FirstPlayable,
        Bounds(5.0f, 5.0f, 195.0f, 195.0f),
        new Draft0TownLayout(
            "town_safe",
            Bounds(75.0f, 5.0f, 125.0f, 55.0f),
            Point(100.0f, 25.0f),
            Point(100.0f, 55.0f)),
        new Draft0RouteCorridor(
            "route_town_exit",
            8.0f,
            [Point(100.0f, 55.0f), Point(100.0f, 70.0f)]),
        Point(100.0f, 70.0f),
        [
            new Draft0BranchLayout(
                "branch_short",
                new Draft0RouteCorridor(
                    "route_branch_short",
                    6.0f,
                    [Point(100.0f, 70.0f), Point(75.0f, 70.0f)]),
                new Draft0CampLayout(
                    "camp_easy",
                    Bounds(45.0f, 55.0f, 75.0f, 85.0f),
                    Point(75.0f, 70.0f),
                    Draft0CampGeometry.BroadOpenCircle),
                [
                    new Draft0SampleSpawn("spawn_easy_01", Point(55.0f, 65.0f)),
                    new Draft0SampleSpawn("spawn_easy_02", Point(60.0f, 75.0f)),
                    new Draft0SampleSpawn("spawn_easy_03", Point(65.0f, 65.0f)),
                ]),
            new Draft0BranchLayout(
                "branch_medium",
                new Draft0RouteCorridor(
                    "route_branch_medium",
                    6.0f,
                    [Point(100.0f, 70.0f), Point(100.0f, 115.0f)]),
                new Draft0CampLayout(
                    "camp_mixed",
                    Bounds(90.0f, 115.0f, 110.0f, 150.0f),
                    Point(100.0f, 115.0f),
                    Draft0CampGeometry.ElongatedOrDivided),
                [
                    new Draft0SampleSpawn("spawn_mixed_01", Point(95.0f, 122.0f)),
                    new Draft0SampleSpawn("spawn_mixed_02", Point(105.0f, 122.0f)),
                    new Draft0SampleSpawn("spawn_mixed_03", Point(95.0f, 144.0f)),
                    new Draft0SampleSpawn("spawn_mixed_04", Point(105.0f, 144.0f)),
                ]),
            new Draft0BranchLayout(
                "branch_long",
                new Draft0RouteCorridor(
                    "route_branch_long",
                    6.0f,
                    [Point(100.0f, 70.0f), Point(145.0f, 70.0f), Point(145.0f, 95.0f)]),
                new Draft0CampLayout(
                    "camp_hard",
                    Bounds(130.0f, 95.0f, 160.0f, 125.0f),
                    Point(145.0f, 95.0f),
                    Draft0CampGeometry.TightBowlOrConstrainedApproach),
                [
                    new Draft0SampleSpawn("spawn_hard_01", Point(140.0f, 104.0f)),
                    new Draft0SampleSpawn("spawn_hard_02", Point(150.0f, 104.0f)),
                    new Draft0SampleSpawn("spawn_hard_03", Point(145.0f, 114.0f)),
                ]),
        ],
        [
            new Draft0ProxyBlock(
                "landmark_west_south",
                Draft0ProxyRole.TownLandmark,
                Bounds(80.0f, 12.0f, 94.0f, 26.0f),
                8.0f),
            new Draft0ProxyBlock(
                "landmark_east_south",
                Draft0ProxyRole.TownLandmark,
                Bounds(106.0f, 12.0f, 120.0f, 26.0f),
                8.0f),
            new Draft0ProxyBlock(
                "landmark_west_north",
                Draft0ProxyRole.TownLandmark,
                Bounds(80.0f, 34.0f, 94.0f, 48.0f),
                7.0f),
            new Draft0ProxyBlock(
                "mixed_divider",
                Draft0ProxyRole.CampDivider,
                Bounds(99.0f, 126.0f, 101.0f, 140.0f),
                2.0f),
            new Draft0ProxyBlock(
                "hard_bowl_wall_west",
                Draft0ProxyRole.CampWall,
                Bounds(130.0f, 99.0f, 134.0f, 125.0f),
                3.0f),
            new Draft0ProxyBlock(
                "hard_bowl_wall_east",
                Draft0ProxyRole.CampWall,
                Bounds(156.0f, 99.0f, 160.0f, 125.0f),
                3.0f),
            new Draft0ProxyBlock(
                "hard_bowl_wall_north",
                Draft0ProxyRole.CampWall,
                Bounds(134.0f, 121.0f, 156.0f, 125.0f),
                3.0f),
        ]);

    private static GroundPoint Point(float xMetres, float zMetres) => new(xMetres, zMetres);

    private static GroundBounds Bounds(
        float minimumXMetres,
        float minimumZMetres,
        float maximumXMetres,
        float maximumZMetres) => new(
            Point(minimumXMetres, minimumZMetres),
            Point(maximumXMetres, maximumZMetres));
}
