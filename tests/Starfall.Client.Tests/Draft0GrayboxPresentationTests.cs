using System.Numerics;
using ChronoFall.CharacterPresentation;
using SDL;
using Starfall.Client;
using Starfall.Content.Zones;

namespace Starfall.Client.Tests;

public sealed class Draft0GrayboxPresentationTests
{
    private static readonly string[] ExpectedSections =
    [
        "surface_grass",
        "boundary_rocks_boulders_south",
        "boundary_rocks_boulders_north",
        "boundary_rocks_boulders_west",
        "boundary_rocks_boulders_east",
        "town_safe",
        "route_town_exit",
        "route_branch_short",
        "route_branch_medium",
        "route_branch_long",
        "camp_easy",
        "camp_mixed",
        "camp_hard",
        "landmark_west_south",
        "landmark_east_south",
        "landmark_west_north",
        "mixed_divider",
        "hard_bowl_wall_west",
        "hard_bowl_wall_east",
        "hard_bowl_wall_north",
        "anchor_town_safe_respawn",
        "anchor_town_safe_exit",
        "anchor_route_junction",
        "anchor_camp_easy_entry",
        "anchor_camp_mixed_entry",
        "anchor_camp_hard_entry",
        "spawn_easy_01",
        "spawn_easy_02",
        "spawn_easy_03",
        "spawn_mixed_01",
        "spawn_mixed_02",
        "spawn_mixed_03",
        "spawn_mixed_04",
        "spawn_hard_01",
        "spawn_hard_02",
        "spawn_hard_03",
    ];

    [Fact]
    public void FirstPlayablePresentationFreezesSectionOrderAndGeometryCounts()
    {
        Draft0GrayboxPresentation presentation = CreatePresentation();

        Assert.Equal(Draft0GrayboxPresentation.ExpectedSectionCount, presentation.Mesh.Sections.Count);
        Assert.Equal(Draft0GrayboxPresentation.ExpectedVertexCount, presentation.Mesh.Vertices.Count);
        Assert.Equal(Draft0GrayboxPresentation.ExpectedIndexCount, presentation.Mesh.Indices.Count);
        Assert.Equal(ExpectedSections, presentation.Mesh.Sections.Select(static section => section.MaterialName));
        Assert.Equal(ExpectedSections.Length, presentation.SectionColors.Count);

        AssertSectionCounts(presentation.Mesh, "surface_grass", 4, 6);
        AssertSectionCounts(presentation.Mesh, "boundary_rocks_boulders_south", 24, 36);
        AssertSectionCounts(presentation.Mesh, "route_town_exit", 38, 102);
        AssertSectionCounts(presentation.Mesh, "route_branch_short", 38, 102);
        AssertSectionCounts(presentation.Mesh, "route_branch_medium", 38, 102);
        AssertSectionCounts(presentation.Mesh, "route_branch_long", 59, 156);
        AssertSectionCounts(presentation.Mesh, "camp_easy", 33, 96);
        AssertSectionCounts(presentation.Mesh, "camp_mixed", 4, 6);
        AssertSectionCounts(presentation.Mesh, "camp_hard", 4, 6);
        AssertSectionCounts(presentation.Mesh, "landmark_west_south", 24, 36);
        AssertSectionCounts(presentation.Mesh, "anchor_town_safe_respawn", 24, 36);
        AssertSectionCounts(presentation.Mesh, "spawn_easy_01", 24, 36);
    }

    [Fact]
    public void PresentationConstructionIsDeterministic()
    {
        Draft0GrayboxPresentation first = CreatePresentation();
        Draft0GrayboxPresentation second = CreatePresentation();

        Assert.Equal(first.Mesh.Name, second.Mesh.Name);
        Assert.Equal(first.Mesh.Vertices, second.Mesh.Vertices);
        Assert.Equal(first.Mesh.Indices, second.Mesh.Indices);
        Assert.Equal(
            first.Mesh.Sections.Select(static section => (section.MaterialName, section.StartIndex, section.IndexCount)),
            second.Mesh.Sections.Select(static section => (section.MaterialName, section.StartIndex, section.IndexCount)));
        Assert.Equal(first.SectionColors, second.SectionColors);
    }

    [Fact]
    public void PresentationLayersDoNotMutateGroundPlaneContent()
    {
        Draft0GrayboxPresentation presentation = CreatePresentation();

        AssertSectionY(presentation.Mesh, "surface_grass", 0.0f, 0.0f);
        AssertSectionY(presentation.Mesh, "town_safe", 0.01f, 0.01f);
        AssertSectionY(presentation.Mesh, "camp_easy", 0.01f, 0.01f);
        AssertSectionY(presentation.Mesh, "camp_mixed", 0.01f, 0.01f);
        AssertSectionY(presentation.Mesh, "camp_hard", 0.01f, 0.01f);
        AssertSectionY(presentation.Mesh, "route_town_exit", 0.02f, 0.02f);
        AssertSectionY(presentation.Mesh, "route_branch_long", 0.02f, 0.02f);
        AssertSectionY(presentation.Mesh, "anchor_town_safe_respawn", 0.03f, 1.53f);
        AssertSectionY(presentation.Mesh, "anchor_town_safe_exit", 0.03f, 1.03f);
        AssertSectionY(presentation.Mesh, "spawn_easy_01", 0.03f, 0.78f);

        Draft0GrayboxLayout layout = Draft0GrayboxCatalog.FirstPlayable;
        Assert.Equal(0.0f, layout.Town.RespawnAnchor.Metres.Y);
        Assert.All(layout.Branches, branch =>
            Assert.All(branch.SampleSpawns, spawn => Assert.Equal(0.0f, spawn.Point.Metres.Y)));
    }

    [Fact]
    public void BoundariesAndProxiesRetainExactFootprintsAndHeights()
    {
        Draft0GrayboxPresentation presentation = CreatePresentation();

        AssertBox(
            presentation.Mesh,
            "boundary_rocks_boulders_south",
            minimum: new Vector3(0.0f, 0.0f, 0.0f),
            maximum: new Vector3(200.0f, 0.5f, 5.0f));
        AssertBox(
            presentation.Mesh,
            "boundary_rocks_boulders_north",
            minimum: new Vector3(0.0f, 0.0f, 195.0f),
            maximum: new Vector3(200.0f, 0.5f, 200.0f));
        AssertBox(
            presentation.Mesh,
            "boundary_rocks_boulders_west",
            minimum: new Vector3(0.0f, 0.0f, 5.0f),
            maximum: new Vector3(5.0f, 0.5f, 195.0f));
        AssertBox(
            presentation.Mesh,
            "boundary_rocks_boulders_east",
            minimum: new Vector3(195.0f, 0.0f, 5.0f),
            maximum: new Vector3(200.0f, 0.5f, 195.0f));

        foreach (Draft0ProxyBlock proxy in Draft0GrayboxCatalog.FirstPlayable.Proxies)
        {
            AssertBox(
                presentation.Mesh,
                proxy.Id,
                new Vector3(proxy.Footprint.Minimum.XMetres, 0.0f, proxy.Footprint.Minimum.ZMetres),
                new Vector3(proxy.Footprint.Maximum.XMetres, proxy.HeightMetres, proxy.Footprint.Maximum.ZMetres));
        }
    }

    [Fact]
    public void RouteCapsAndCornerJoinUseTheFrozenSweptCorridorConstruction()
    {
        Draft0GrayboxPresentation presentation = CreatePresentation();
        Vector3[] shortRoute = GetSectionVertices(presentation.Mesh, "route_branch_short");
        Vector3[] longRoute = GetSectionVertices(presentation.Mesh, "route_branch_long");

        Assert.Equal(38, shortRoute.Length);
        Assert.Contains(shortRoute, vertex => NearlyEqual(vertex.X, 72.0f) && NearlyEqual(vertex.Z, 70.0f));
        Assert.True(Draft0GrayboxCatalog.FirstPlayable.Branches[0].Camp.Contains(new GroundPoint(72.0f, 70.0f)));

        Assert.Equal(59, longRoute.Length);
        Assert.Contains(longRoute, vertex => NearlyEqual(vertex.X, 148.0f) && NearlyEqual(vertex.Z, 70.0f));
        Assert.Contains(longRoute, vertex => NearlyEqual(vertex.X, 145.0f) && NearlyEqual(vertex.Z, 67.0f));
        Assert.Single(longRoute, vertex => NearlyEqual(vertex.X, 145.0f) && NearlyEqual(vertex.Z, 70.0f));
    }

    [Fact]
    public void PaletteIsFrozenBySemanticSection()
    {
        Draft0GrayboxPresentation presentation = CreatePresentation();

        AssertColor(presentation, "surface_grass", Draft0GrayboxPresentation.GrassColor);
        AssertColor(presentation, "town_safe", Draft0GrayboxPresentation.TownColor);
        AssertColor(presentation, "route_branch_long", Draft0GrayboxPresentation.RouteColor);
        AssertColor(presentation, "camp_easy", Draft0GrayboxPresentation.EasyCampColor);
        AssertColor(presentation, "camp_mixed", Draft0GrayboxPresentation.MixedCampColor);
        AssertColor(presentation, "camp_hard", Draft0GrayboxPresentation.HardCampColor);
        AssertColor(presentation, "landmark_west_south", Draft0GrayboxPresentation.TownLandmarkColor);
        AssertColor(presentation, "mixed_divider", Draft0GrayboxPresentation.CampDividerColor);
        AssertColor(presentation, "hard_bowl_wall_west", Draft0GrayboxPresentation.CampWallColor);
        AssertColor(presentation, "anchor_town_safe_respawn", Draft0GrayboxPresentation.RespawnColor);
        AssertColor(presentation, "anchor_route_junction", Draft0GrayboxPresentation.AnchorColor);
        AssertColor(presentation, "spawn_easy_01", Draft0GrayboxPresentation.EasySpawnColor);
        AssertColor(presentation, "spawn_mixed_01", Draft0GrayboxPresentation.MixedSpawnColor);
        AssertColor(presentation, "spawn_hard_01", Draft0GrayboxPresentation.HardSpawnColor);
    }

    [Fact]
    public void AllPresentationGeometryIsFiniteAndInsideTheDraft0Zone()
    {
        Draft0GrayboxPresentation presentation = CreatePresentation();

        Assert.All(presentation.Mesh.Vertices, vertex =>
        {
            Assert.True(float.IsFinite(vertex.Position.X));
            Assert.True(float.IsFinite(vertex.Position.Y));
            Assert.True(float.IsFinite(vertex.Position.Z));
            Assert.InRange(vertex.Position.X, 0.0f, 200.0f);
            Assert.InRange(vertex.Position.Z, 0.0f, 200.0f);
            Assert.InRange(vertex.Position.Y, 0.0f, 8.0f);
        });
    }

    [Fact]
    public void CameraPresetsUseFunctionKeysAndTabWithoutConsumingActionNumbers()
    {
        var controller = new Draft0GrayboxCameraController();

        Assert.Equal("player-fixture", controller.CurrentPreset.Name);
        Assert.True(controller.HandleKey(SDL_Keycode.SDLK_F7, repeated: false));
        Assert.Equal("hard-camp", controller.CurrentPreset.Name);
        Assert.True(controller.HandleKey(SDL_Keycode.SDLK_TAB, repeated: false));
        Assert.Equal("player-fixture", controller.CurrentPreset.Name);
        Assert.True(controller.HandleKey(SDL_Keycode.SDLK_F2, repeated: false));
        Assert.Equal("overview", controller.CurrentPreset.Name);
        Assert.False(controller.HandleKey(SDL_Keycode.SDLK_F5, repeated: true));
        Assert.Equal("overview", controller.CurrentPreset.Name);
        Assert.False(controller.HandleKey(SDL_Keycode.SDLK_1, repeated: false));
        Assert.False(controller.HandleKey(SDL_Keycode.SDLK_2, repeated: false));
        Assert.Equal("overview", controller.CurrentPreset.Name);
    }

    [Fact]
    public void PlayerViewFollowsThePlayerAndTunesDistanceWithoutChangingFixedViews()
    {
        var controller = new Draft0GrayboxCameraController();
        var player = new GroundPoint(100.0f, 25.0f);

        PerspectiveIsometricCamera initial = controller.CreateCamera(player);
        Assert.Equal(player, initial.Focus);
        Assert.Equal(22.5f, controller.CurrentDistanceMetres);

        Assert.False(controller.HandleKey(SDL_Keycode.SDLK_UP, repeated: true));
        Assert.True(controller.HandleKey(SDL_Keycode.SDLK_UP, repeated: false));
        Assert.Equal(22.0f, controller.CurrentDistanceMetres);
        Assert.True(controller.HandleKey(SDL_Keycode.SDLK_DOWN, repeated: false));
        Assert.Equal(22.5f, controller.CurrentDistanceMetres);

        controller.SelectPreset(1);
        Assert.False(controller.HandleKey(SDL_Keycode.SDLK_UP, repeated: false));
        Assert.Equal(560.0f, controller.CurrentDistanceMetres);
        Assert.Equal(new GroundPoint(100.0f, 100.0f), controller.CreateCamera(player).Focus);

        controller.SelectPreset(0);
        Assert.Equal(22.5f, controller.CurrentDistanceMetres);
        Assert.Equal(player, controller.CreateCamera(player).Focus);
    }

    [Fact]
    public void PlayerViewDistanceClampsToTheApprovedRange()
    {
        var controller = new Draft0GrayboxCameraController();

        for (var index = 0; index < 100; index++)
            _ = controller.HandleKey(SDL_Keycode.SDLK_UP, repeated: false);
        Assert.Equal(10.0f, controller.CurrentDistanceMetres);
        Assert.False(controller.HandleKey(SDL_Keycode.SDLK_UP, repeated: false));

        for (var index = 0; index < 200; index++)
            _ = controller.HandleKey(SDL_Keycode.SDLK_DOWN, repeated: false);
        Assert.Equal(60.0f, controller.CurrentDistanceMetres);
        Assert.False(controller.HandleKey(SDL_Keycode.SDLK_DOWN, repeated: false));
    }

    [Fact]
    public void CameraPresetsFreezeApprovedFocusAndProjectionInputs()
    {
        string[] names = Draft0GrayboxCameraController.All.Select(static preset => preset.Name).ToArray();
        Assert.Equal(
            ["player-fixture", "overview", "town", "junction", "easy-camp", "mixed-camp", "hard-camp"],
            names);

        AssertPreset(0, 100.0f, 100.0f, 22.5f, 1.0f, 300.0f);
        AssertPreset(1, 100.0f, 100.0f, 560.0f, 100.0f, 800.0f);
        AssertPreset(2, 100.0f, 30.0f, 85.0f, 1.0f, 300.0f);
        AssertPreset(3, 100.0f, 70.0f, 80.0f, 1.0f, 300.0f);
        AssertPreset(4, 60.0f, 70.0f, 55.0f, 1.0f, 300.0f);
        AssertPreset(5, 100.0f, 132.5f, 65.0f, 1.0f, 300.0f);
        AssertPreset(6, 145.0f, 110.0f, 55.0f, 1.0f, 300.0f);
    }

    [Fact]
    public void OverviewContainsAllZoneCornersAndEveryViewPicksItsFocus()
    {
        GroundBounds zone = Draft0GrayboxCatalog.FirstPlayable.Specification.Bounds;
        Draft0GrayboxCameraPreset overview = Draft0GrayboxCameraController.All[1];
        var overviewCamera = new PerspectiveIsometricCamera(overview.Focus, overview.Settings);

        GroundPoint[] corners =
        [
            zone.Minimum,
            new GroundPoint(zone.Maximum.XMetres, zone.Minimum.ZMetres),
            zone.Maximum,
            new GroundPoint(zone.Minimum.XMetres, zone.Maximum.ZMetres),
        ];
        Assert.All(corners, corner =>
        {
            Vector2 projected = ProjectGround(overviewCamera, corner, 1920, 1080);
            Assert.InRange(projected.X, 0.0f, 1.0f);
            Assert.InRange(projected.Y, 0.0f, 1.0f);
        });

        foreach (Draft0GrayboxCameraPreset preset in Draft0GrayboxCameraController.All.Where(
                     static preset => !string.Equals(preset.Name, "overview", StringComparison.Ordinal)))
        {
            var camera = new PerspectiveIsometricCamera(preset.Focus, preset.Settings);
            Assert.True(camera.TryPickGround(new Vector2(0.5f, 0.5f), 1920, 1080, zone, out GroundPoint picked));
            Assert.True(
                NearlyEqual(preset.Focus.XMetres, picked.XMetres),
                $"{preset.Name} X focus {preset.Focus.XMetres} picked {picked.XMetres}.");
            Assert.True(
                NearlyEqual(preset.Focus.ZMetres, picked.ZMetres),
                $"{preset.Name} Z focus {preset.Focus.ZMetres} picked {picked.ZMetres}.");
        }
    }

    private static Draft0GrayboxPresentation CreatePresentation() =>
        Draft0GrayboxPresentation.Create(Draft0GrayboxCatalog.FirstPlayable);

    private static void AssertSectionCounts(
        StaticMeshDefinition mesh,
        string sectionName,
        int expectedVertices,
        int expectedIndices)
    {
        StaticMeshSection section = RequireSection(mesh, sectionName);
        Assert.Equal(expectedIndices, section.IndexCount);
        Assert.Equal(expectedVertices, GetSectionVertices(mesh, sectionName).Length);
    }

    private static void AssertSectionY(
        StaticMeshDefinition mesh,
        string sectionName,
        float minimum,
        float maximum)
    {
        Vector3[] vertices = GetSectionVertices(mesh, sectionName);
        Assert.Equal(minimum, vertices.Min(static vertex => vertex.Y), precision: 5);
        Assert.Equal(maximum, vertices.Max(static vertex => vertex.Y), precision: 5);
    }

    private static void AssertBox(
        StaticMeshDefinition mesh,
        string sectionName,
        Vector3 minimum,
        Vector3 maximum)
    {
        Vector3[] vertices = GetSectionVertices(mesh, sectionName);
        Assert.Equal(minimum.X, vertices.Min(static vertex => vertex.X), precision: 5);
        Assert.Equal(minimum.Y, vertices.Min(static vertex => vertex.Y), precision: 5);
        Assert.Equal(minimum.Z, vertices.Min(static vertex => vertex.Z), precision: 5);
        Assert.Equal(maximum.X, vertices.Max(static vertex => vertex.X), precision: 5);
        Assert.Equal(maximum.Y, vertices.Max(static vertex => vertex.Y), precision: 5);
        Assert.Equal(maximum.Z, vertices.Max(static vertex => vertex.Z), precision: 5);
    }

    private static void AssertColor(
        Draft0GrayboxPresentation presentation,
        string sectionName,
        Vector3 expected)
    {
        int sectionIndex = presentation.Mesh.Sections
            .Select(static (section, index) => (section, index))
            .Single(item => string.Equals(item.section.MaterialName, sectionName, StringComparison.Ordinal))
            .index;
        Assert.Equal(expected, presentation.SectionColors[sectionIndex]);
    }

    private static StaticMeshSection RequireSection(StaticMeshDefinition mesh, string name) =>
        mesh.Sections.Single(section => string.Equals(section.MaterialName, name, StringComparison.Ordinal));

    private static Vector3[] GetSectionVertices(StaticMeshDefinition mesh, string sectionName)
    {
        StaticMeshSection section = RequireSection(mesh, sectionName);
        return mesh.Indices
            .Skip(section.StartIndex)
            .Take(section.IndexCount)
            .Distinct()
            .Select(index => mesh.Vertices[checked((int)index)].Position)
            .ToArray();
    }

    private static void AssertPreset(
        int index,
        float focusX,
        float focusZ,
        float distance,
        float nearPlane,
        float farPlane)
    {
        Draft0GrayboxCameraPreset preset = Draft0GrayboxCameraController.All[index];
        Assert.Equal(focusX, preset.Focus.XMetres);
        Assert.Equal(focusZ, preset.Focus.ZMetres);
        Assert.Equal(28.0f, preset.Settings.VerticalFieldOfViewDegrees);
        Assert.Equal(42.0f, preset.Settings.DownwardPitchDegrees);
        Assert.Equal(45.0f, preset.Settings.YawDegrees);
        Assert.Equal(distance, preset.Settings.FocusDistanceMetres);
        Assert.Equal(nearPlane, preset.Settings.NearPlaneMetres);
        Assert.Equal(farPlane, preset.Settings.FarPlaneMetres);
    }

    private static Vector2 ProjectGround(
        PerspectiveIsometricCamera camera,
        GroundPoint point,
        uint width,
        uint height)
    {
        Vector4 clip = Vector4.Transform(new Vector4(point.Metres, 1.0f), camera.CreateViewProjection(width, height));
        Assert.True(clip.W > 0.0f);
        Vector3 ndc = new(clip.X / clip.W, clip.Y / clip.W, clip.Z / clip.W);
        return new Vector2((ndc.X + 1.0f) * 0.5f, (1.0f - ndc.Y) * 0.5f);
    }

    private static bool NearlyEqual(float left, float right) => MathF.Abs(left - right) <= 0.02f;
}
