using System.Numerics;
using Starfall.Content.Zones;

namespace Starfall.Content.Tests;

public sealed class Draft0ZoneSpecificationTests
{
    [Fact]
    public void FirstPlayableRecordsTheApprovedRegionalTargetsInStableOrder()
    {
        Draft0ZoneSpecification zone = Draft0ZoneCatalog.FirstPlayable;

        Assert.Equal("draft_0_first_playable_zone", zone.Id);
        Assert.Equal(new GroundPoint(0.0f, 0.0f), zone.Bounds.Minimum);
        Assert.Equal(new GroundPoint(200.0f, 200.0f), zone.Bounds.Maximum);
        Assert.Equal(new GroundDimensions(200.0f, 200.0f), zone.Bounds.Dimensions);
        Assert.Equal(new GroundDimensions(50.0f, 50.0f), zone.Town.ApproximateDimensions);
        Assert.Equal(Draft0TownPlacement.NearMapEdge, zone.Town.Placement);
        Assert.True(zone.Town.IsProtected);
        Assert.True(zone.Town.RequiresRespawnAnchor);
        Assert.Equal(2, zone.Town.MinimumLandmarkCount);
        Assert.Equal(3, zone.Town.MaximumLandmarkCount);
        Assert.Equal(1, zone.Town.ExitCount);
        Assert.True(zone.RequiresExitJunction);
        Assert.Collection(
            zone.Branches,
            branch => AssertBranch(branch, "branch_short", 25.0f, Draft0CampGeometry.BroadOpenCircle),
            branch => AssertBranch(branch, "branch_medium", 45.0f, Draft0CampGeometry.ElongatedOrDivided),
            branch => AssertBranch(branch, "branch_long", 70.0f, Draft0CampGeometry.TightBowlOrConstrainedApproach));
        Assert.Equal("surface_grass", zone.Environment.DefaultSurfaceId);
        Assert.Equal("surface_dirt_path", zone.Environment.RouteSurfaceId);
        Assert.Equal("boundary_rocks_boulders", zone.Environment.BoundaryPresentationId);
    }

    [Fact]
    public void GroundValueTypesRejectNonFiniteOffPlaneAndInvalidDimensions()
    {
        Assert.Throws<ArgumentException>(() => new GroundPoint(float.NaN, 0.0f));
        Assert.Throws<ArgumentException>(() => new GroundPoint(new Vector3(0.0f, 1.0f, 0.0f)));
        Assert.Throws<ArgumentException>(() => new GroundDimensions(float.PositiveInfinity, 1.0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GroundDimensions(0.0f, 1.0f));
        Assert.Throws<ArgumentException>(() => new GroundBounds(new GroundPoint(10.0f, 0.0f), new GroundPoint(5.0f, 1.0f)));
    }

    [Fact]
    public void BoundsContainEdgesAndRejectOutOfZonePoints()
    {
        GroundBounds bounds = Draft0ZoneCatalog.FirstPlayable.Bounds;

        Assert.True(bounds.Contains(new GroundPoint(0.0f, 0.0f)));
        Assert.True(bounds.Contains(new GroundPoint(200.0f, 200.0f)));
        Assert.True(bounds.Contains(new GroundPoint(100.0f, 100.0f)));
        Assert.False(bounds.Contains(new GroundPoint(-0.01f, 50.0f)));
        Assert.False(bounds.Contains(new GroundPoint(50.0f, 200.01f)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            bounds.RequireContains(new GroundPoint(201.0f, 100.0f), "spawnAnchor"));
    }

    [Fact]
    public void ZoneCopiesBranchesAndRejectsDuplicateOrInvalidContent()
    {
        Draft0BranchSpecification first = new("branch_one", 10.0f, Draft0CampGeometry.BroadOpenCircle);
        var branches = new List<Draft0BranchSpecification> { first };
        Draft0ZoneSpecification zone = CreateZone(branches);
        branches.Clear();

        Assert.Single(zone.Branches);
        Assert.Same(first, zone.Branches[0]);
        Assert.Throws<ArgumentException>(() => CreateZone([first, first]));
        Assert.Throws<ArgumentException>(() =>
            new Draft0BranchSpecification("Branch-One", 10.0f, Draft0CampGeometry.BroadOpenCircle));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Draft0BranchSpecification("branch_one", float.PositiveInfinity, Draft0CampGeometry.BroadOpenCircle));
        Assert.Throws<ArgumentException>(() =>
            new Draft0EnvironmentSpecification("same", "same", "different"));
    }

    [Fact]
    public void TownTargetCannotExceedZoneBounds()
    {
        Assert.Throws<ArgumentException>(() => CreateZone(
            [new Draft0BranchSpecification("branch_one", 10.0f, Draft0CampGeometry.BroadOpenCircle)],
            townDimensions: new GroundDimensions(201.0f, 50.0f)));
    }

    private static Draft0ZoneSpecification CreateZone(
        IEnumerable<Draft0BranchSpecification> branches,
        GroundDimensions? townDimensions = null) => new(
            "test_zone",
            new GroundBounds(new GroundPoint(0.0f, 0.0f), new GroundPoint(200.0f, 200.0f)),
            new Draft0TownSpecification(
                townDimensions ?? new GroundDimensions(50.0f, 50.0f),
                Draft0TownPlacement.NearMapEdge,
                true,
                true,
                2,
                3,
                1),
            true,
            branches,
            new Draft0EnvironmentSpecification("surface_one", "surface_two", "boundary_three"));

    private static void AssertBranch(
        Draft0BranchSpecification branch,
        string id,
        float distance,
        Draft0CampGeometry geometry)
    {
        Assert.Equal(id, branch.Id);
        Assert.Equal(distance, branch.ApproximateTravelDistanceMetres);
        Assert.Equal(geometry, branch.CampGeometry);
    }
}
