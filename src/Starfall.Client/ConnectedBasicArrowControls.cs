using System.Numerics;
using Starfall.Protocol.Monsters;
using Starfall.Protocol.Movement;

namespace Starfall.Client;

internal enum Draft0PointerButton
{
    Primary,
    Secondary,
}

internal enum Draft0PointerAction
{
    None,
    Move,
    BasicArrow,
}

internal static class Draft0PointerControls
{
    internal static Draft0PointerAction Resolve(Draft0PointerButton button, bool connected) =>
        button switch
        {
            Draft0PointerButton.Primary when connected => Draft0PointerAction.BasicArrow,
            Draft0PointerButton.Primary => Draft0PointerAction.None,
            Draft0PointerButton.Secondary => Draft0PointerAction.Move,
            _ => throw new ArgumentOutOfRangeException(nameof(button)),
        };
}

internal static class ConnectedBasicArrowTargeting
{
    internal const float PositiveHitToleranceMetres = 1e-4f;
    internal const float EqualHitToleranceMetres = 1e-4f;

    internal static bool TryPick(
        PerspectiveWorldRay ray,
        BoundedMonsterSnapshot snapshot,
        out WorldEntityId targetEntityId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        targetEntityId = default;

        float nearestDistance = float.PositiveInfinity;
        foreach (LiveMonsterSnapshot monster in snapshot.LiveMonsters)
        {
            float height = GetPickHeightMetres(monster.ArchetypeId.Value);
            if (!TryIntersectVerticalCylinder(
                    ray,
                    monster.Position.XMetres,
                    monster.Position.ZMetres,
                    monster.CollisionRadiusMetres,
                    height,
                    out float distance))
            {
                continue;
            }

            bool nearer = distance < nearestDistance - EqualHitToleranceMetres;
            bool equalAndLowerIdentity = MathF.Abs(distance - nearestDistance) <= EqualHitToleranceMetres &&
                (targetEntityId.Value == 0 || monster.EntityId.Value < targetEntityId.Value);
            if (nearer || equalAndLowerIdentity)
            {
                nearestDistance = distance;
                targetEntityId = monster.EntityId;
            }
        }

        return targetEntityId.Value != 0;
    }

    private static float GetPickHeightMetres(string archetypeId) =>
        Draft0MonsterPresentationAdapter.GroundClearanceMetres +
        Draft0MonsterPresentationAdapter.GetUniformScaleMetres(archetypeId) +
        Draft0MonsterPresentationAdapter.HoverAmplitudeMetres;

    private static bool TryIntersectVerticalCylinder(
        PerspectiveWorldRay ray,
        float centreX,
        float centreZ,
        float radius,
        float height,
        out float nearestDistance)
    {
        float candidateDistance = float.PositiveInfinity;
        float offsetX = ray.Origin.X - centreX;
        float offsetZ = ray.Origin.Z - centreZ;
        float horizontalDirectionLengthSquared =
            ray.Direction.X * ray.Direction.X + ray.Direction.Z * ray.Direction.Z;

        if (horizontalDirectionLengthSquared > PositiveHitToleranceMetres * PositiveHitToleranceMetres)
        {
            float b = 2.0f * (offsetX * ray.Direction.X + offsetZ * ray.Direction.Z);
            float c = offsetX * offsetX + offsetZ * offsetZ - radius * radius;
            float discriminant = b * b - 4.0f * horizontalDirectionLengthSquared * c;
            if (discriminant >= 0.0f)
            {
                float root = MathF.Sqrt(discriminant);
                float denominator = 2.0f * horizontalDirectionLengthSquared;
                ConsiderSide((-b - root) / denominator);
                ConsiderSide((-b + root) / denominator);
            }
        }

        if (MathF.Abs(ray.Direction.Y) > PositiveHitToleranceMetres)
        {
            ConsiderCap(-ray.Origin.Y / ray.Direction.Y);
            ConsiderCap((height - ray.Origin.Y) / ray.Direction.Y);
        }

        nearestDistance = candidateDistance;
        return float.IsFinite(candidateDistance);

        void ConsiderSide(float distance)
        {
            if (!IsPositiveFinite(distance))
                return;
            float y = ray.Origin.Y + ray.Direction.Y * distance;
            if (y >= 0.0f && y <= height)
                candidateDistance = MathF.Min(candidateDistance, distance);
        }

        void ConsiderCap(float distance)
        {
            if (!IsPositiveFinite(distance))
                return;
            float x = offsetX + ray.Direction.X * distance;
            float z = offsetZ + ray.Direction.Z * distance;
            if (x * x + z * z <= radius * radius)
                candidateDistance = MathF.Min(candidateDistance, distance);
        }

        static bool IsPositiveFinite(float value) =>
            float.IsFinite(value) && value > PositiveHitToleranceMetres;
    }
}

internal sealed class ConnectedBasicArrowSelection
{
    internal WorldEntityId? SelectedTarget
    {
        get; private set;
    }

    internal bool SelectOrClear(PerspectiveWorldRay ray, BoundedMonsterSnapshot snapshot)
    {
        if (!ConnectedBasicArrowTargeting.TryPick(ray, snapshot, out WorldEntityId selected))
        {
            SelectedTarget = null;
            return false;
        }

        SelectedTarget = selected;
        return true;
    }

    internal bool Reconcile(BoundedMonsterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (SelectedTarget is not { } selected)
            return false;
        if (snapshot.LiveMonsters.Any(monster => monster.EntityId == selected))
            return false;

        SelectedTarget = null;
        return true;
    }

    internal bool Clear()
    {
        if (SelectedTarget is null)
            return false;
        SelectedTarget = null;
        return true;
    }
}
