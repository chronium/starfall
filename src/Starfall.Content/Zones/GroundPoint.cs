using System.Numerics;

namespace Starfall.Content.Zones;

public readonly record struct GroundPoint
{
    public GroundPoint(float xMetres, float zMetres)
        : this(new Vector3(xMetres, 0.0f, zMetres))
    {
    }

    public GroundPoint(Vector3 metres)
    {
        if (!IsFinite(metres))
            throw new ArgumentException("Ground points must contain finite metre values.", nameof(metres));
        if (metres.Y != 0.0f)
            throw new ArgumentException("Draft 0 ground points must lie on Y = 0 metres.", nameof(metres));

        Metres = metres;
    }

    public Vector3 Metres
    {
        get;
    }

    public float XMetres => Metres.X;

    public float ZMetres => Metres.Z;

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
