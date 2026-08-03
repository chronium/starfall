using Starfall.Content.Zones;

namespace Starfall.Content.Tests;

public sealed class Draft0ExecutableGrayboxTests
{
    [Fact]
    public void FirstPlayableFreezesTheApprovedTownRoutesAndCamps()
    {
        Draft0GrayboxLayout layout = Draft0GrayboxCatalog.FirstPlayable;

        Assert.Same(Draft0ZoneCatalog.FirstPlayable, layout.Specification);
        AssertBounds(layout.WalkableBounds, 5.0f, 5.0f, 195.0f, 195.0f);
        Assert.Equal("town_safe", layout.Town.Id);
        AssertBounds(layout.Town.Bounds, 75.0f, 5.0f, 125.0f, 55.0f);
        Assert.Equal(new GroundPoint(100.0f, 25.0f), layout.Town.RespawnAnchor);
        Assert.Equal(new GroundPoint(100.0f, 55.0f), layout.Town.ExitAnchor);
        Assert.Equal(new GroundPoint(100.0f, 70.0f), layout.Junction);
        AssertRoute(
            layout.ExitRoute,
            "route_town_exit",
            8.0f,
            15.0f,
            new GroundPoint(100.0f, 55.0f),
            new GroundPoint(100.0f, 70.0f));

        Assert.Collection(
            layout.Branches,
            branch => AssertBranch(
                branch,
                "branch_short",
                "route_branch_short",
                25.0f,
                "camp_easy",
                new GroundPoint(75.0f, 70.0f),
                Draft0CampGeometry.BroadOpenCircle,
                new GroundBounds(new GroundPoint(45.0f, 55.0f), new GroundPoint(75.0f, 85.0f))),
            branch => AssertBranch(
                branch,
                "branch_medium",
                "route_branch_medium",
                45.0f,
                "camp_mixed",
                new GroundPoint(100.0f, 115.0f),
                Draft0CampGeometry.ElongatedOrDivided,
                new GroundBounds(new GroundPoint(90.0f, 115.0f), new GroundPoint(110.0f, 150.0f))),
            branch => AssertBranch(
                branch,
                "branch_long",
                "route_branch_long",
                70.0f,
                "camp_hard",
                new GroundPoint(145.0f, 95.0f),
                Draft0CampGeometry.TightBowlOrConstrainedApproach,
                new GroundBounds(new GroundPoint(130.0f, 95.0f), new GroundPoint(160.0f, 125.0f))));

        Assert.Collection(
            layout.Branches[0].Route.Points,
            point => Assert.Equal(new GroundPoint(100.0f, 70.0f), point),
            point => Assert.Equal(new GroundPoint(75.0f, 70.0f), point));
        Assert.Collection(
            layout.Branches[1].Route.Points,
            point => Assert.Equal(new GroundPoint(100.0f, 70.0f), point),
            point => Assert.Equal(new GroundPoint(100.0f, 115.0f), point));
        Assert.Collection(
            layout.Branches[2].Route.Points,
            point => Assert.Equal(new GroundPoint(100.0f, 70.0f), point),
            point => Assert.Equal(new GroundPoint(145.0f, 70.0f), point),
            point => Assert.Equal(new GroundPoint(145.0f, 95.0f), point));
    }

    [Fact]
    public void FirstPlayableFreezesProxyAndSpawnIdentityOrder()
    {
        Draft0GrayboxLayout layout = Draft0GrayboxCatalog.FirstPlayable;

        Assert.Collection(
            layout.Proxies,
            proxy => AssertProxy(proxy, "landmark_west_south", Draft0ProxyRole.TownLandmark, 80.0f, 12.0f, 94.0f, 26.0f, 8.0f),
            proxy => AssertProxy(proxy, "landmark_east_south", Draft0ProxyRole.TownLandmark, 106.0f, 12.0f, 120.0f, 26.0f, 8.0f),
            proxy => AssertProxy(proxy, "landmark_west_north", Draft0ProxyRole.TownLandmark, 80.0f, 34.0f, 94.0f, 48.0f, 7.0f),
            proxy => AssertProxy(proxy, "mixed_divider", Draft0ProxyRole.CampDivider, 99.0f, 126.0f, 101.0f, 140.0f, 2.0f),
            proxy => AssertProxy(proxy, "hard_bowl_wall_west", Draft0ProxyRole.CampWall, 130.0f, 99.0f, 134.0f, 125.0f, 3.0f),
            proxy => AssertProxy(proxy, "hard_bowl_wall_east", Draft0ProxyRole.CampWall, 156.0f, 99.0f, 160.0f, 125.0f, 3.0f),
            proxy => AssertProxy(proxy, "hard_bowl_wall_north", Draft0ProxyRole.CampWall, 134.0f, 121.0f, 156.0f, 125.0f, 3.0f));

        AssertSpawns(
            layout.Branches[0],
            ("spawn_easy_01", new GroundPoint(55.0f, 65.0f)),
            ("spawn_easy_02", new GroundPoint(60.0f, 75.0f)),
            ("spawn_easy_03", new GroundPoint(65.0f, 65.0f)));
        AssertSpawns(
            layout.Branches[1],
            ("spawn_mixed_01", new GroundPoint(95.0f, 122.0f)),
            ("spawn_mixed_02", new GroundPoint(105.0f, 122.0f)),
            ("spawn_mixed_03", new GroundPoint(95.0f, 144.0f)),
            ("spawn_mixed_04", new GroundPoint(105.0f, 144.0f)));
        AssertSpawns(
            layout.Branches[2],
            ("spawn_hard_01", new GroundPoint(140.0f, 104.0f)),
            ("spawn_hard_02", new GroundPoint(150.0f, 104.0f)),
            ("spawn_hard_03", new GroundPoint(145.0f, 114.0f)));
    }

    [Fact]
    public void RouteFootprintsUseRoundCapsAndOverlapTheEasyCamp()
    {
        Draft0BranchLayout easy = Draft0GrayboxCatalog.FirstPlayable.Branches[0];
        GroundPoint overlap = new(73.0f, 70.0f);

        Assert.True(easy.Route.ContainsPresentationFootprint(overlap));
        Assert.True(easy.Camp.Contains(overlap));
        Assert.True(easy.Route.ContainsPresentationFootprint(new GroundPoint(75.0f, 73.0f)));
        Assert.False(easy.Route.ContainsPresentationFootprint(new GroundPoint(75.0f, 73.01f)));
        Assert.False(easy.Camp.Contains(new GroundPoint(75.0f, 73.0f)));
    }

    [Fact]
    public void CatalogSpawnsAreInsideActualCampFootprintsAndOutsideEveryProxy()
    {
        Draft0GrayboxLayout layout = Draft0GrayboxCatalog.FirstPlayable;

        foreach (Draft0BranchLayout branch in layout.Branches)
        {
            foreach (Draft0SampleSpawn spawn in branch.SampleSpawns)
            {
                Assert.True(branch.Camp.Contains(spawn.Point), spawn.Id);
                Assert.DoesNotContain(layout.Proxies, proxy => proxy.Contains(spawn.Point));
            }
        }
    }

    [Fact]
    public void ValueObjectsRejectInvalidRouteCampTownAndProxyInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Draft0RouteCorridor("route", float.NaN, [Point(10.0f, 10.0f), Point(20.0f, 10.0f)]));
        Assert.Throws<ArgumentException>(() =>
            new Draft0RouteCorridor("route", 2.0f, [Point(10.0f, 10.0f), Point(10.0f, 10.0f)]));
        Assert.Throws<ArgumentException>(() =>
            new Draft0CampLayout(
                "camp",
                Bounds(10.0f, 10.0f, 30.0f, 40.0f),
                Point(30.0f, 25.0f),
                Draft0CampGeometry.BroadOpenCircle));
        Assert.Throws<ArgumentException>(() =>
            new Draft0CampLayout(
                "camp",
                Bounds(10.0f, 10.0f, 30.0f, 30.0f),
                Point(20.0f, 20.0f),
                Draft0CampGeometry.BroadOpenCircle));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Draft0TownLayout("town", Bounds(10.0f, 10.0f, 30.0f, 30.0f), Point(31.0f, 20.0f), Point(20.0f, 30.0f)));
        Assert.Throws<ArgumentException>(() =>
            new Draft0TownLayout("town", Bounds(10.0f, 10.0f, 30.0f, 30.0f), Point(20.0f, 20.0f), Point(20.0f, 20.0f)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Draft0ProxyBlock("proxy", Draft0ProxyRole.CampWall, Bounds(10.0f, 10.0f, 12.0f, 12.0f), 0.0f));
    }

    [Fact]
    public void LayoutRejectsDuplicateAndMisownedIdentityBearingInputs()
    {
        Draft0GrayboxLayout source = Draft0GrayboxCatalog.FirstPlayable;
        var duplicate = source.Proxies.Append(new Draft0ProxyBlock(
            "town_safe",
            Draft0ProxyRole.TownLandmark,
            Bounds(96.0f, 12.0f, 98.0f, 14.0f),
            2.0f));
        Assert.Throws<ArgumentException>(() => CreateLayout(proxies: duplicate));

        Draft0ProxyBlock[] misplacedLandmark = source.Proxies.ToArray();
        misplacedLandmark[0] = new Draft0ProxyBlock(
            "landmark_west_south",
            Draft0ProxyRole.TownLandmark,
            Bounds(60.0f, 60.0f, 64.0f, 64.0f),
            8.0f);
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateLayout(proxies: misplacedLandmark));

        Draft0ProxyBlock[] misplacedDivider = source.Proxies.ToArray();
        misplacedDivider[3] = new Draft0ProxyBlock(
            "mixed_divider",
            Draft0ProxyRole.CampDivider,
            Bounds(115.0f, 126.0f, 117.0f, 140.0f),
            2.0f);
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateLayout(proxies: misplacedDivider));
    }

    [Fact]
    public void LayoutRejectsBlockedSpawnsAndCriticalAnchors()
    {
        Draft0GrayboxLayout source = Draft0GrayboxCatalog.FirstPlayable;
        Draft0BranchLayout[] branches = source.Branches.ToArray();
        Draft0BranchLayout mixed = branches[1];
        branches[1] = new Draft0BranchLayout(
            mixed.Id,
            mixed.Route,
            mixed.Camp,
            mixed.SampleSpawns.Append(new Draft0SampleSpawn("spawn_blocked", Point(100.0f, 130.0f))));
        Assert.Throws<ArgumentException>(() => CreateLayout(branches: branches));

        var blockedEntry = source.Proxies.Append(new Draft0ProxyBlock(
            "hard_bowl_wall_entry",
            Draft0ProxyRole.CampWall,
            Bounds(144.0f, 95.0f, 146.0f, 99.0f),
            3.0f));
        Assert.Throws<ArgumentException>(() => CreateLayout(proxies: blockedEntry));
    }

    [Fact]
    public void LayoutRejectsOutOfBoundsThickRoutesAndMismatchedDurableBranches()
    {
        Draft0GrayboxLayout source = Draft0GrayboxCatalog.FirstPlayable;
        var edgeRoute = new Draft0RouteCorridor(
            "route_town_exit",
            8.0f,
            [Point(5.0f, 55.0f), source.Junction]);
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateLayout(exitRoute: edgeRoute));

        Draft0BranchLayout[] branches = source.Branches.ToArray();
        Draft0BranchLayout easy = branches[0];
        branches[0] = new Draft0BranchLayout("wrong_branch", easy.Route, easy.Camp, easy.SampleSpawns);
        Assert.Throws<ArgumentException>(() => CreateLayout(branches: branches));
    }

    [Fact]
    public void RouteAndLayoutCollectionsAreDefensivelyCopied()
    {
        var points = new List<GroundPoint> { Point(10.0f, 10.0f), Point(20.0f, 10.0f) };
        var route = new Draft0RouteCorridor("route_copy", 2.0f, points);
        points.Clear();
        Assert.Equal(2, route.Points.Count);

        Draft0GrayboxLayout source = Draft0GrayboxCatalog.FirstPlayable;
        Draft0BranchLayout[] branches = source.Branches.ToArray();
        Draft0GrayboxLayout copy = CreateLayout(branches: branches);
        branches[0] = branches[1];
        Assert.Equal("branch_short", copy.Branches[0].Id);
    }

    private static Draft0GrayboxLayout CreateLayout(
        Draft0RouteCorridor? exitRoute = null,
        IEnumerable<Draft0BranchLayout>? branches = null,
        IEnumerable<Draft0ProxyBlock>? proxies = null)
    {
        Draft0GrayboxLayout source = Draft0GrayboxCatalog.FirstPlayable;
        return new Draft0GrayboxLayout(
            source.Specification,
            source.WalkableBounds,
            source.Town,
            exitRoute ?? source.ExitRoute,
            source.Junction,
            branches ?? source.Branches,
            proxies ?? source.Proxies);
    }

    private static void AssertBranch(
        Draft0BranchLayout branch,
        string id,
        string routeId,
        float lengthMetres,
        string campId,
        GroundPoint entry,
        Draft0CampGeometry geometry,
        GroundBounds bounds)
    {
        Assert.Equal(id, branch.Id);
        Assert.Equal(routeId, branch.Route.Id);
        Assert.Equal(6.0f, branch.Route.WidthMetres);
        Assert.Equal(lengthMetres, branch.Route.LengthMetres, Draft0GrayboxLayout.ValidationToleranceMetres);
        Assert.Equal(campId, branch.Camp.Id);
        Assert.Equal(entry, branch.Camp.EntryAnchor);
        Assert.Equal(geometry, branch.Camp.Geometry);
        Assert.Equal(bounds, branch.Camp.Bounds);
    }

    private static void AssertRoute(
        Draft0RouteCorridor route,
        string id,
        float widthMetres,
        float lengthMetres,
        params GroundPoint[] points)
    {
        Assert.Equal(id, route.Id);
        Assert.Equal(widthMetres, route.WidthMetres);
        Assert.Equal(lengthMetres, route.LengthMetres, Draft0GrayboxLayout.ValidationToleranceMetres);
        Assert.Equal(points, route.Points);
    }

    private static void AssertProxy(
        Draft0ProxyBlock proxy,
        string id,
        Draft0ProxyRole role,
        float minimumX,
        float minimumZ,
        float maximumX,
        float maximumZ,
        float heightMetres)
    {
        Assert.Equal(id, proxy.Id);
        Assert.Equal(role, proxy.Role);
        AssertBounds(proxy.Footprint, minimumX, minimumZ, maximumX, maximumZ);
        Assert.Equal(heightMetres, proxy.HeightMetres);
    }

    private static void AssertSpawns(
        Draft0BranchLayout branch,
        params (string Id, GroundPoint Point)[] expected)
    {
        Assert.Equal(expected.Length, branch.SampleSpawns.Count);
        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(expected[index].Id, branch.SampleSpawns[index].Id);
            Assert.Equal(expected[index].Point, branch.SampleSpawns[index].Point);
        }
    }

    private static void AssertBounds(
        GroundBounds bounds,
        float minimumX,
        float minimumZ,
        float maximumX,
        float maximumZ)
    {
        Assert.Equal(new GroundPoint(minimumX, minimumZ), bounds.Minimum);
        Assert.Equal(new GroundPoint(maximumX, maximumZ), bounds.Maximum);
    }

    private static GroundPoint Point(float xMetres, float zMetres) => new(xMetres, zMetres);

    private static GroundBounds Bounds(
        float minimumX,
        float minimumZ,
        float maximumX,
        float maximumZ) => new(Point(minimumX, minimumZ), Point(maximumX, maximumZ));
}
