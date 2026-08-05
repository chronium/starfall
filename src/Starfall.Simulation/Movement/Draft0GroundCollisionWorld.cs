using System.Numerics;
using ChronoFall.Box3D.Bodies;
using ChronoFall.Box3D.Geometry;
using ChronoFall.Box3D.Worlds;
using Starfall.Content.Zones;

namespace Starfall.Simulation.Movement;

internal sealed class Draft0GroundCollisionWorld : IDisposable
{
    private const ulong EnvironmentCategory = 1UL << 0;
    private const ulong MoverCategory = 1UL << 1;
    private static readonly Box3DFilter EnvironmentFilter =
        new(EnvironmentCategory, MoverCategory);
    private static readonly Box3DQueryFilter MoverQueryFilter =
        new(MoverCategory, EnvironmentCategory);

    private readonly Draft0GrayboxLayout layout;
    private readonly Box3DWorld physicsWorld;
    private bool disposed;

    internal Draft0GroundCollisionWorld(Draft0GrayboxLayout layout)
    {
        this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
        physicsWorld = Box3DWorld.Create(Vector3.Zero);
        try
        {
            CreateStaticCollision();
            physicsWorld.Step(1.0f / Draft0PlayerMovementSimulation.TickRateHz, 1);
        }
        catch
        {
            physicsWorld.Dispose();
            throw;
        }
    }

    internal void Step()
    {
        ThrowIfDisposed();
        physicsWorld.Step(1.0f / Draft0PlayerMovementSimulation.TickRateHz, 1);
    }

    internal float CastCapsuleMover(
        GroundPoint position,
        float radiusMetres,
        float heightMetres,
        Vector2 translation)
    {
        ThrowIfDisposed();
        Vector3 center1 = new(0.0f, radiusMetres, 0.0f);
        Vector3 center2 = new(0.0f, heightMetres - radiusMetres, 0.0f);
        return Cast(position, center1, center2, radiusMetres, translation);
    }

    internal float CastRoundGroundMover(
        GroundPoint position,
        float radiusMetres,
        Vector2 translation)
    {
        ThrowIfDisposed();
        Vector3 center1 = new(0.0f, radiusMetres, 0.0f);
        Vector3 center2 = new(
            0.0f,
            Draft0PlayerMovementSimulation.PlayerHeightMetres - radiusMetres,
            0.0f);
        return Math.Min(
            Cast(position, center1, center2, radiusMetres, translation),
            CastExpandedProxyFootprints(position, radiusMetres, translation));
    }

    internal bool ContainsWalkableCenter(GroundPoint point, float radiusMetres)
    {
        GroundBounds bounds = layout.WalkableBounds;
        return point.XMetres >= bounds.Minimum.XMetres + radiusMetres &&
            point.XMetres <= bounds.Maximum.XMetres - radiusMetres &&
            point.ZMetres >= bounds.Minimum.ZMetres + radiusMetres &&
            point.ZMetres <= bounds.Maximum.ZMetres - radiusMetres;
    }

    internal bool OverlapsProxy(GroundPoint point, float radiusMetres)
    {
        foreach (Draft0ProxyBlock proxy in layout.Proxies)
        {
            float nearestX = Math.Clamp(
                point.XMetres,
                proxy.Footprint.Minimum.XMetres,
                proxy.Footprint.Maximum.XMetres);
            float nearestZ = Math.Clamp(
                point.ZMetres,
                proxy.Footprint.Minimum.ZMetres,
                proxy.Footprint.Maximum.ZMetres);
            float deltaX = point.XMetres - nearestX;
            float deltaZ = point.ZMetres - nearestZ;
            if ((deltaX * deltaX) + (deltaZ * deltaZ) <= radiusMetres * radiusMetres)
                return true;
        }

        return false;
    }

    public void Dispose()
    {
        if (disposed)
            return;

        physicsWorld.Dispose();
        disposed = true;
    }

    private float Cast(
        GroundPoint position,
        Vector3 center1,
        Vector3 center2,
        float radiusMetres,
        Vector2 translation)
    {
        Vector3 spatialTranslation = new(translation.X, 0.0f, translation.Y);
        return Math.Clamp(
            physicsWorld.CastMover(
                position.Metres,
                center1,
                center2,
                radiusMetres,
                spatialTranslation,
                MoverQueryFilter),
            0.0f,
            1.0f);
    }

    private float CastExpandedProxyFootprints(
        GroundPoint position,
        float radiusMetres,
        Vector2 translation)
    {
        float fraction = 1.0f;
        foreach (Draft0ProxyBlock proxy in layout.Proxies)
        {
            float minimumX = proxy.Footprint.Minimum.XMetres - radiusMetres;
            float maximumX = proxy.Footprint.Maximum.XMetres + radiusMetres;
            float minimumZ = proxy.Footprint.Minimum.ZMetres - radiusMetres;
            float maximumZ = proxy.Footprint.Maximum.ZMetres + radiusMetres;
            float entry = 0.0f;
            float exit = fraction;
            if (!ClipAxis(position.XMetres, translation.X, minimumX, maximumX, ref entry, ref exit) ||
                !ClipAxis(position.ZMetres, translation.Y, minimumZ, maximumZ, ref entry, ref exit))
            {
                continue;
            }

            if (exit <= 0.0f)
                continue;
            if (entry >= 0.0f && entry <= fraction)
                fraction = entry;
        }

        return fraction;
    }

    private static bool ClipAxis(
        float origin,
        float translation,
        float minimum,
        float maximum,
        ref float entry,
        ref float exit)
    {
        if (translation == 0.0f)
            return origin >= minimum && origin <= maximum;

        float first = (minimum - origin) / translation;
        float second = (maximum - origin) / translation;
        if (first > second)
            (first, second) = (second, first);
        entry = MathF.Max(entry, first);
        exit = MathF.Min(exit, second);
        return entry <= exit;
    }

    private void CreateStaticCollision()
    {
        GroundBounds zone = layout.Specification.Bounds;
        GroundBounds walkable = layout.WalkableBounds;

        CreateBox(
            new GroundBounds(
                zone.Minimum,
                new GroundPoint(zone.Maximum.XMetres, walkable.Minimum.ZMetres)),
            Draft0PlayerMovementSimulation.PlayerHeightMetres);
        CreateBox(
            new GroundBounds(
                new GroundPoint(zone.Minimum.XMetres, walkable.Maximum.ZMetres),
                zone.Maximum),
            Draft0PlayerMovementSimulation.PlayerHeightMetres);
        CreateBox(
            new GroundBounds(
                new GroundPoint(zone.Minimum.XMetres, walkable.Minimum.ZMetres),
                new GroundPoint(walkable.Minimum.XMetres, walkable.Maximum.ZMetres)),
            Draft0PlayerMovementSimulation.PlayerHeightMetres);
        CreateBox(
            new GroundBounds(
                new GroundPoint(walkable.Maximum.XMetres, walkable.Minimum.ZMetres),
                new GroundPoint(zone.Maximum.XMetres, walkable.Maximum.ZMetres)),
            Draft0PlayerMovementSimulation.PlayerHeightMetres);

        foreach (Draft0ProxyBlock proxy in layout.Proxies)
            CreateBox(proxy.Footprint, proxy.HeightMetres);
    }

    private void CreateBox(GroundBounds footprint, float heightMetres)
    {
        GroundDimensions dimensions = footprint.Dimensions;
        Vector3 position = new(
            (footprint.Minimum.XMetres + footprint.Maximum.XMetres) * 0.5f,
            heightMetres * 0.5f,
            (footprint.Minimum.ZMetres + footprint.Maximum.ZMetres) * 0.5f);
        Vector3 halfExtents = new(
            dimensions.WidthMetres * 0.5f,
            heightMetres * 0.5f,
            dimensions.DepthMetres * 0.5f);
        Box3DBody body = physicsWorld.CreateBody(Box3DBodyKind.Static, position, Quaternion.Identity);
        body.CreateBoxShape(halfExtents, EnvironmentFilter);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);
}
