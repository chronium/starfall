using ChronoFall.CharacterPresentation.SdlGpu;

namespace Starfall.Client;

internal sealed record Draft0GrayboxCapture(
    int PresetIndex,
    string PresetName,
    string FileName);

internal static class Draft0GrayboxCaptureSuite
{
    internal const int Width = 1920;
    internal const int Height = 1080;
    internal const float AnimationSampleSeconds = 0.5f;

    internal static IReadOnlyList<Draft0GrayboxCapture> Captures
    {
        get;
    } =
    [
        new(0, "player-fixture", "01-player-fixture.png"),
        new(1, "overview", "02-overview.png"),
        new(2, "town", "03-town.png"),
        new(3, "junction", "04-junction.png"),
        new(4, "easy-camp", "05-easy-camp.png"),
        new(5, "mixed-camp", "06-mixed-camp.png"),
        new(6, "hard-camp", "07-hard-camp.png"),
    ];

    internal static IReadOnlyList<ulong> Validate(IReadOnlyList<RgbaImage> images)
    {
        ArgumentNullException.ThrowIfNull(images);
        if (images.Count != Captures.Count)
        {
            throw new ArgumentException(
                $"Draft 0 capture suite requires exactly {Captures.Count} images, received {images.Count}.",
                nameof(images));
        }

        ulong[] fingerprints = new ulong[images.Count];
        for (var index = 0; index < images.Count; index++)
            fingerprints[index] = ValidateImage(images[index], Captures[index].PresetName);
        if (fingerprints.Distinct().Count() != fingerprints.Length)
            throw new InvalidDataException("Draft 0 capture suite contains duplicate rendered images.");
        return fingerprints;
    }

    internal static ulong ValidateImage(RgbaImage image, string diagnosticName)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(diagnosticName);
        if (image.Width != Width || image.Height != Height)
        {
            throw new InvalidDataException(
                $"Draft 0 capture '{diagnosticName}' is {image.Width}x{image.Height}; expected {Width}x{Height}.");
        }

        ReadOnlySpan<byte> pixels = image.Pixels.Span;
        bool differsFromFirst = false;
        byte firstRed = pixels[0];
        byte firstGreen = pixels[1];
        byte firstBlue = pixels[2];
        ulong fingerprint = 14695981039346656037UL;
        for (var offset = 0; offset < pixels.Length; offset += 4)
        {
            if (pixels[offset + 3] != byte.MaxValue)
            {
                throw new InvalidDataException(
                    $"Draft 0 capture '{diagnosticName}' contains a non-opaque pixel at index {offset / 4}.");
            }

            differsFromFirst |= pixels[offset] != firstRed ||
                pixels[offset + 1] != firstGreen ||
                pixels[offset + 2] != firstBlue;
            fingerprint = AddByte(fingerprint, pixels[offset]);
            fingerprint = AddByte(fingerprint, pixels[offset + 1]);
            fingerprint = AddByte(fingerprint, pixels[offset + 2]);
            fingerprint = AddByte(fingerprint, pixels[offset + 3]);
        }

        if (!differsFromFirst)
            throw new InvalidDataException($"Draft 0 capture '{diagnosticName}' is a single flat colour.");
        return fingerprint;
    }

    private static ulong AddByte(ulong hash, byte value) => (hash ^ value) * 1099511628211UL;
}
