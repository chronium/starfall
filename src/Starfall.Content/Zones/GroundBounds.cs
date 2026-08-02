namespace Starfall.Content.Zones;

public readonly record struct GroundBounds
{
    public GroundBounds(GroundPoint minimum, GroundPoint maximum)
    {
        if (maximum.XMetres <= minimum.XMetres || maximum.ZMetres <= minimum.ZMetres)
            throw new ArgumentException("Ground bounds must have positive width and depth.", nameof(maximum));

        Minimum = minimum;
        Maximum = maximum;
    }

    public GroundPoint Minimum
    {
        get;
    }

    public GroundPoint Maximum
    {
        get;
    }

    public GroundDimensions Dimensions => new(
        Maximum.XMetres - Minimum.XMetres,
        Maximum.ZMetres - Minimum.ZMetres);

    public bool Contains(GroundPoint point) =>
        point.XMetres >= Minimum.XMetres &&
        point.XMetres <= Maximum.XMetres &&
        point.ZMetres >= Minimum.ZMetres &&
        point.ZMetres <= Maximum.ZMetres;

    public void RequireContains(GroundPoint point, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
        if (!Contains(point))
            throw new ArgumentOutOfRangeException(parameterName, point, "Ground point lies outside the zone bounds.");
    }
}
