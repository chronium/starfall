using System.Collections.ObjectModel;
using System.Numerics;
using ChronoFall.CharacterPresentation;
using Starfall.Content.Zones;

namespace Starfall.Client;

internal sealed class Draft0GrayboxPresentation
{
    internal const int ExpectedSectionCount = 36;
    internal const int ExpectedVertexCount = 870;
    internal const int ExpectedIndexCount = 1554;
    internal const float GroundLayerY = 0.0f;
    internal const float RegionLayerY = 0.01f;
    internal const float RouteLayerY = 0.02f;
    internal const float MarkerBaseY = 0.03f;
    internal const float BoundaryHeightMetres = 0.5f;
    internal const float RespawnMarkerSizeMetres = 1.5f;
    internal const float AnchorMarkerSizeMetres = 1.0f;
    internal const float SpawnMarkerSizeMetres = 0.75f;

    internal static readonly Vector3 GrassColor = new(0.18f, 0.32f, 0.16f);
    internal static readonly Vector3 TownColor = new(0.10f, 0.38f, 0.48f);
    internal static readonly Vector3 RouteColor = new(0.50f, 0.32f, 0.14f);
    internal static readonly Vector3 EasyCampColor = new(0.24f, 0.52f, 0.24f);
    internal static readonly Vector3 MixedCampColor = new(0.62f, 0.43f, 0.12f);
    internal static readonly Vector3 HardCampColor = new(0.58f, 0.18f, 0.16f);
    internal static readonly Vector3 BoundaryColor = new(0.34f, 0.36f, 0.40f);
    internal static readonly Vector3 TownLandmarkColor = new(0.55f, 0.43f, 0.26f);
    internal static readonly Vector3 CampDividerColor = new(0.78f, 0.36f, 0.08f);
    internal static readonly Vector3 CampWallColor = new(0.42f, 0.42f, 0.46f);
    internal static readonly Vector3 RespawnColor = new(0.10f, 0.80f, 0.85f);
    internal static readonly Vector3 AnchorColor = new(0.92f, 0.82f, 0.18f);
    internal static readonly Vector3 EasySpawnColor = new(0.40f, 0.95f, 0.35f);
    internal static readonly Vector3 MixedSpawnColor = new(1.00f, 0.65f, 0.15f);
    internal static readonly Vector3 HardSpawnColor = new(1.00f, 0.25f, 0.20f);

    private Draft0GrayboxPresentation(
        StaticMeshDefinition mesh,
        IReadOnlyList<Vector3> sectionColors)
    {
        Mesh = mesh;
        SectionColors = sectionColors;
    }

    internal StaticMeshDefinition Mesh
    {
        get;
    }

    internal IReadOnlyList<Vector3> SectionColors
    {
        get;
    }

    internal static Draft0GrayboxPresentation Create(Draft0GrayboxLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var builder = new PresentationBuilder();

        builder.AddHorizontalQuad(
            layout.Specification.Environment.DefaultSurfaceId,
            GrassColor,
            layout.WalkableBounds,
            GroundLayerY);

        GroundBounds zone = layout.Specification.Bounds;
        GroundBounds walkable = layout.WalkableBounds;
        builder.AddBox(
            layout.Specification.Environment.BoundaryPresentationId + "_south",
            BoundaryColor,
            Bounds(zone.Minimum.XMetres, zone.Minimum.ZMetres, zone.Maximum.XMetres, walkable.Minimum.ZMetres),
            0.0f,
            BoundaryHeightMetres);
        builder.AddBox(
            layout.Specification.Environment.BoundaryPresentationId + "_north",
            BoundaryColor,
            Bounds(zone.Minimum.XMetres, walkable.Maximum.ZMetres, zone.Maximum.XMetres, zone.Maximum.ZMetres),
            0.0f,
            BoundaryHeightMetres);
        builder.AddBox(
            layout.Specification.Environment.BoundaryPresentationId + "_west",
            BoundaryColor,
            Bounds(zone.Minimum.XMetres, walkable.Minimum.ZMetres, walkable.Minimum.XMetres, walkable.Maximum.ZMetres),
            0.0f,
            BoundaryHeightMetres);
        builder.AddBox(
            layout.Specification.Environment.BoundaryPresentationId + "_east",
            BoundaryColor,
            Bounds(walkable.Maximum.XMetres, walkable.Minimum.ZMetres, zone.Maximum.XMetres, walkable.Maximum.ZMetres),
            0.0f,
            BoundaryHeightMetres);

        builder.AddHorizontalQuad(layout.Town.Id, TownColor, layout.Town.Bounds, RegionLayerY);
        builder.AddRoute(layout.ExitRoute, RouteColor);
        foreach (Draft0BranchLayout branch in layout.Branches)
            builder.AddRoute(branch.Route, RouteColor);

        for (var branchIndex = 0; branchIndex < layout.Branches.Count; branchIndex++)
        {
            Draft0CampLayout camp = layout.Branches[branchIndex].Camp;
            Vector3 color = GetCampColor(branchIndex);
            if (camp.Geometry == Draft0CampGeometry.BroadOpenCircle)
            {
                builder.AddDisc(
                    camp.Id,
                    color,
                    camp.Center,
                    camp.RadiusMetres,
                    RegionLayerY,
                    wedgeCount: 32);
            }
            else
            {
                builder.AddHorizontalQuad(camp.Id, color, camp.Bounds, RegionLayerY);
            }
        }

        foreach (Draft0ProxyBlock proxy in layout.Proxies)
        {
            builder.AddBox(
                proxy.Id,
                GetProxyColor(proxy.Role),
                proxy.Footprint,
                minimumY: 0.0f,
                maximumY: proxy.HeightMetres);
        }

        builder.AddMarker(
            "anchor_town_safe_respawn",
            RespawnColor,
            layout.Town.RespawnAnchor,
            RespawnMarkerSizeMetres);
        builder.AddMarker(
            "anchor_town_safe_exit",
            AnchorColor,
            layout.Town.ExitAnchor,
            AnchorMarkerSizeMetres);
        builder.AddMarker(
            "anchor_route_junction",
            AnchorColor,
            layout.Junction,
            AnchorMarkerSizeMetres);
        foreach (Draft0BranchLayout branch in layout.Branches)
        {
            builder.AddMarker(
                $"anchor_{branch.Camp.Id}_entry",
                AnchorColor,
                branch.Camp.EntryAnchor,
                AnchorMarkerSizeMetres);
        }

        for (var branchIndex = 0; branchIndex < layout.Branches.Count; branchIndex++)
        {
            foreach (Draft0SampleSpawn spawn in layout.Branches[branchIndex].SampleSpawns)
            {
                builder.AddMarker(
                    spawn.Id,
                    GetSpawnColor(branchIndex),
                    spawn.Point,
                    SpawnMarkerSizeMetres);
            }
        }

        Draft0GrayboxPresentation presentation = builder.Build();
        if (presentation.Mesh.Sections.Count != ExpectedSectionCount ||
            presentation.Mesh.Vertices.Count != ExpectedVertexCount ||
            presentation.Mesh.Indices.Count != ExpectedIndexCount)
        {
            throw new InvalidOperationException(
                $"Draft 0 presentation geometry changed unexpectedly: " +
                $"sections={presentation.Mesh.Sections.Count}/{ExpectedSectionCount}, " +
                $"vertices={presentation.Mesh.Vertices.Count}/{ExpectedVertexCount}, " +
                $"indices={presentation.Mesh.Indices.Count}/{ExpectedIndexCount}.");
        }

        return presentation;
    }

    private static Vector3 GetCampColor(int branchIndex) => branchIndex switch
    {
        0 => EasyCampColor,
        1 => MixedCampColor,
        2 => HardCampColor,
        _ => throw new ArgumentOutOfRangeException(nameof(branchIndex)),
    };

    private static Vector3 GetSpawnColor(int branchIndex) => branchIndex switch
    {
        0 => EasySpawnColor,
        1 => MixedSpawnColor,
        2 => HardSpawnColor,
        _ => throw new ArgumentOutOfRangeException(nameof(branchIndex)),
    };

    private static Vector3 GetProxyColor(Draft0ProxyRole role) => role switch
    {
        Draft0ProxyRole.TownLandmark => TownLandmarkColor,
        Draft0ProxyRole.CampDivider => CampDividerColor,
        Draft0ProxyRole.CampWall => CampWallColor,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    private static GroundBounds Bounds(
        float minimumX,
        float minimumZ,
        float maximumX,
        float maximumZ) => new(
        new GroundPoint(minimumX, minimumZ),
        new GroundPoint(maximumX, maximumZ));

    private sealed class PresentationBuilder
    {
        private readonly List<StaticVertex> vertices = [];
        private readonly List<uint> indices = [];
        private readonly List<StaticMeshSection> sections = [];
        private readonly List<Vector3> sectionColors = [];

        internal void AddHorizontalQuad(
            string name,
            Vector3 color,
            GroundBounds bounds,
            float y) => AddSection(name, color, () => AddHorizontalQuad(bounds, y));

        internal void AddBox(
            string name,
            Vector3 color,
            GroundBounds footprint,
            float minimumY,
            float maximumY) => AddSection(
            name,
            color,
            () => AddBox(footprint, minimumY, maximumY));

        internal void AddDisc(
            string name,
            Vector3 color,
            GroundPoint center,
            float radius,
            float y,
            int wedgeCount) => AddSection(
            name,
            color,
            () => AddDisc(center, radius, y, wedgeCount));

        internal void AddRoute(Draft0RouteCorridor route, Vector3 color)
        {
            AddSection(route.Id, color, () =>
            {
                float radius = route.HalfWidthMetres;
                for (var index = 1; index < route.Points.Count; index++)
                    AddRouteSegment(route.Points[index - 1], route.Points[index], radius);
                foreach (GroundPoint point in route.Points)
                    AddDisc(point, radius, RouteLayerY, wedgeCount: 16);
            });
        }

        internal void AddMarker(
            string name,
            Vector3 color,
            GroundPoint center,
            float size)
        {
            float halfSize = size * 0.5f;
            var footprint = Bounds(
                center.XMetres - halfSize,
                center.ZMetres - halfSize,
                center.XMetres + halfSize,
                center.ZMetres + halfSize);
            AddBox(name, color, footprint, MarkerBaseY, MarkerBaseY + size);
        }

        internal Draft0GrayboxPresentation Build()
        {
            var mesh = new StaticMeshDefinition("starfall-draft-0-graybox", vertices, indices, sections);
            return new Draft0GrayboxPresentation(
                mesh,
                new ReadOnlyCollection<Vector3>(sectionColors.ToArray()));
        }

        private void AddSection(string name, Vector3 color, Action appendGeometry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(appendGeometry);
            int startIndex = indices.Count;
            appendGeometry();
            int indexCount = indices.Count - startIndex;
            sections.Add(new StaticMeshSection(name, startIndex, indexCount));
            sectionColors.Add(color);
        }

        private void AddHorizontalQuad(GroundBounds bounds, float y) => AddQuad(
            new Vector3(bounds.Minimum.XMetres, y, bounds.Maximum.ZMetres),
            new Vector3(bounds.Maximum.XMetres, y, bounds.Maximum.ZMetres),
            new Vector3(bounds.Maximum.XMetres, y, bounds.Minimum.ZMetres),
            new Vector3(bounds.Minimum.XMetres, y, bounds.Minimum.ZMetres),
            Vector3.UnitY);

        private void AddRouteSegment(GroundPoint start, GroundPoint end, float radius)
        {
            Vector2 startPlane = new(start.XMetres, start.ZMetres);
            Vector2 endPlane = new(end.XMetres, end.ZMetres);
            Vector2 direction = Vector2.Normalize(endPlane - startPlane);
            Vector2 lateral = new(-direction.Y, direction.X);
            Vector2 startLeft = startPlane + (lateral * radius);
            Vector2 endLeft = endPlane + (lateral * radius);
            Vector2 endRight = endPlane - (lateral * radius);
            Vector2 startRight = startPlane - (lateral * radius);
            AddQuad(
                ToWorld(startLeft, RouteLayerY),
                ToWorld(endLeft, RouteLayerY),
                ToWorld(endRight, RouteLayerY),
                ToWorld(startRight, RouteLayerY),
                Vector3.UnitY);
        }

        private void AddDisc(GroundPoint center, float radius, float y, int wedgeCount)
        {
            uint centreIndex = checked((uint)vertices.Count);
            vertices.Add(new StaticVertex(new Vector3(center.XMetres, y, center.ZMetres), Vector3.UnitY));
            uint perimeterStart = checked((uint)vertices.Count);
            for (var wedge = 0; wedge < wedgeCount; wedge++)
            {
                float angle = -MathF.Tau * wedge / wedgeCount;
                vertices.Add(new StaticVertex(
                    new Vector3(
                        center.XMetres + MathF.Cos(angle) * radius,
                        y,
                        center.ZMetres + MathF.Sin(angle) * radius),
                    Vector3.UnitY));
            }

            for (var wedge = 0; wedge < wedgeCount; wedge++)
            {
                uint current = perimeterStart + (uint)wedge;
                uint next = perimeterStart + (uint)((wedge + 1) % wedgeCount);
                indices.AddRange([centreIndex, current, next]);
            }
        }

        private void AddBox(GroundBounds footprint, float minimumY, float maximumY)
        {
            Vector3 minimum = new(footprint.Minimum.XMetres, minimumY, footprint.Minimum.ZMetres);
            Vector3 maximum = new(footprint.Maximum.XMetres, maximumY, footprint.Maximum.ZMetres);
            AddQuad(new(minimum.X, minimum.Y, maximum.Z), new(maximum.X, minimum.Y, maximum.Z), new(maximum.X, maximum.Y, maximum.Z), new(minimum.X, maximum.Y, maximum.Z), Vector3.UnitZ);
            AddQuad(new(maximum.X, minimum.Y, minimum.Z), new(minimum.X, minimum.Y, minimum.Z), new(minimum.X, maximum.Y, minimum.Z), new(maximum.X, maximum.Y, minimum.Z), -Vector3.UnitZ);
            AddQuad(new(maximum.X, minimum.Y, minimum.Z), new(maximum.X, maximum.Y, minimum.Z), new(maximum.X, maximum.Y, maximum.Z), new(maximum.X, minimum.Y, maximum.Z), Vector3.UnitX);
            AddQuad(new(minimum.X, minimum.Y, maximum.Z), new(minimum.X, maximum.Y, maximum.Z), new(minimum.X, maximum.Y, minimum.Z), new(minimum.X, minimum.Y, minimum.Z), -Vector3.UnitX);
            AddQuad(new(minimum.X, maximum.Y, maximum.Z), new(maximum.X, maximum.Y, maximum.Z), new(maximum.X, maximum.Y, minimum.Z), new(minimum.X, maximum.Y, minimum.Z), Vector3.UnitY);
            AddQuad(new(minimum.X, minimum.Y, minimum.Z), new(maximum.X, minimum.Y, minimum.Z), new(maximum.X, minimum.Y, maximum.Z), new(minimum.X, minimum.Y, maximum.Z), -Vector3.UnitY);
        }

        private void AddQuad(
            Vector3 first,
            Vector3 second,
            Vector3 third,
            Vector3 fourth,
            Vector3 normal)
        {
            uint start = checked((uint)vertices.Count);
            vertices.Add(new StaticVertex(first, normal));
            vertices.Add(new StaticVertex(second, normal));
            vertices.Add(new StaticVertex(third, normal));
            vertices.Add(new StaticVertex(fourth, normal));
            indices.AddRange([start, start + 1, start + 2, start, start + 2, start + 3]);
        }

        private static Vector3 ToWorld(Vector2 point, float y) => new(point.X, y, point.Y);
    }
}
