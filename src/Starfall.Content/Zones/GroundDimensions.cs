using System.Numerics;

namespace Starfall.Content.Zones;

public readonly record struct GroundDimensions
{
    public GroundDimensions(float widthMetres, float depthMetres)
        : this(new Vector3(widthMetres, 0.0f, depthMetres))
    {
    }

    public GroundDimensions(Vector3 metres)
    {
        if (!float.IsFinite(metres.X) || !float.IsFinite(metres.Y) || !float.IsFinite(metres.Z))
            throw new ArgumentException("Ground dimensions must contain finite metre values.", nameof(metres));
        if (metres.X <= 0.0f || metres.Z <= 0.0f || metres.Y != 0.0f)
            throw new ArgumentOutOfRangeException(nameof(metres), "Ground width and depth must be positive and Y must be zero.");

        Metres = metres;
    }

    public Vector3 Metres
    {
        get;
    }

    public float WidthMetres => Metres.X;

    public float DepthMetres => Metres.Z;
}
