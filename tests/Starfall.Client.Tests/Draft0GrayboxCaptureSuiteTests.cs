using ChronoFall.CharacterPresentation.SdlGpu;
using Starfall.Client;

namespace Starfall.Client.Tests;

public sealed class Draft0GrayboxCaptureSuiteTests
{
    [Fact]
    public void CaptureRecipeFreezesF1ThroughF7OrderNamesAndSample()
    {
        Assert.Equal(1920, Draft0GrayboxCaptureSuite.Width);
        Assert.Equal(1080, Draft0GrayboxCaptureSuite.Height);
        Assert.Equal(0.5f, Draft0GrayboxCaptureSuite.AnimationSampleSeconds);
        Assert.Equal(
        [
            (0, "player-fixture", "01-player-fixture.png"),
            (1, "overview", "02-overview.png"),
            (2, "town", "03-town.png"),
            (3, "junction", "04-junction.png"),
            (4, "easy-camp", "05-easy-camp.png"),
            (5, "mixed-camp", "06-mixed-camp.png"),
            (6, "hard-camp", "07-hard-camp.png"),
        ],
        Draft0GrayboxCaptureSuite.Captures.Select(
            static capture => (capture.PresetIndex, capture.PresetName, capture.FileName)));
        Assert.Equal(
            Draft0GrayboxCameraController.All.Select(static preset => preset.Name),
            Draft0GrayboxCaptureSuite.Captures.Select(static capture => capture.PresetName));
    }

    [Fact]
    public void CameraControllerSupportsDeterministicPresetSelection()
    {
        var controller = new Draft0GrayboxCameraController();

        controller.SelectPreset(6);

        Assert.Equal("hard-camp", controller.CurrentPreset.Name);
        Assert.Throws<ArgumentOutOfRangeException>(() => controller.SelectPreset(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => controller.SelectPreset(7));
    }

    [Fact]
    public void CaptureValidationRequiresExactOpaqueNonFlatImage()
    {
        byte[] pixels = CreateOpaquePixels();
        pixels[^4] = 1;
        var valid = new RgbaImage(
            Draft0GrayboxCaptureSuite.Width,
            Draft0GrayboxCaptureSuite.Height,
            pixels);

        ulong fingerprint = Draft0GrayboxCaptureSuite.ValidateImage(valid, "valid");

        Assert.NotEqual(0UL, fingerprint);

        var wrongSize = new RgbaImage(1, 1, [0, 0, 0, 255]);
        Assert.Throws<InvalidDataException>(() =>
            Draft0GrayboxCaptureSuite.ValidateImage(wrongSize, "wrong-size"));

        pixels[^1] = 0;
        var transparent = new RgbaImage(
            Draft0GrayboxCaptureSuite.Width,
            Draft0GrayboxCaptureSuite.Height,
            pixels);
        Assert.Throws<InvalidDataException>(() =>
            Draft0GrayboxCaptureSuite.ValidateImage(transparent, "transparent"));

        var flat = new RgbaImage(
            Draft0GrayboxCaptureSuite.Width,
            Draft0GrayboxCaptureSuite.Height,
            CreateOpaquePixels());
        Assert.Throws<InvalidDataException>(() =>
            Draft0GrayboxCaptureSuite.ValidateImage(flat, "flat"));
    }

    [Fact]
    public void CaptureSuiteRejectsWrongCountAndDuplicateFrames()
    {
        var image = new RgbaImage(
            Draft0GrayboxCaptureSuite.Width,
            Draft0GrayboxCaptureSuite.Height,
            CreateNonFlatOpaquePixels());

        Assert.Throws<ArgumentException>(() => Draft0GrayboxCaptureSuite.Validate([]));
        Assert.Throws<InvalidDataException>(() => Draft0GrayboxCaptureSuite.Validate(
            Enumerable.Repeat(image, Draft0GrayboxCaptureSuite.Captures.Count).ToArray()));
    }

    private static byte[] CreateOpaquePixels()
    {
        byte[] pixels = new byte[Draft0GrayboxCaptureSuite.Width * Draft0GrayboxCaptureSuite.Height * 4];
        for (var offset = 3; offset < pixels.Length; offset += 4)
            pixels[offset] = byte.MaxValue;
        return pixels;
    }

    private static byte[] CreateNonFlatOpaquePixels()
    {
        byte[] pixels = CreateOpaquePixels();
        pixels[^4] = 1;
        return pixels;
    }
}
