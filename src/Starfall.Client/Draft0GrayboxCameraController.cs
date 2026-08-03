using SDL;
using Starfall.Content.Zones;

namespace Starfall.Client;

internal sealed record Draft0GrayboxCameraPreset(
    string Name,
    GroundPoint Focus,
    PerspectiveIsometricCameraSettings Settings);

internal sealed class Draft0GrayboxCameraController
{
    private static readonly Draft0GrayboxCameraPreset[] Presets =
    [
        Preset("player-fixture", 100.0f, 100.0f, 22.5f, 1.0f, 300.0f),
        Preset("overview", 100.0f, 100.0f, 560.0f, 100.0f, 800.0f),
        Preset("town", 100.0f, 30.0f, 85.0f, 1.0f, 300.0f),
        Preset("junction", 100.0f, 70.0f, 80.0f, 1.0f, 300.0f),
        Preset("easy-camp", 60.0f, 70.0f, 55.0f, 1.0f, 300.0f),
        Preset("mixed-camp", 100.0f, 132.5f, 65.0f, 1.0f, 300.0f),
        Preset("hard-camp", 145.0f, 110.0f, 55.0f, 1.0f, 300.0f),
    ];

    private int selectedIndex;

    internal Draft0GrayboxCameraPreset CurrentPreset => Presets[selectedIndex];

    internal PerspectiveIsometricCamera Camera => new(CurrentPreset.Focus, CurrentPreset.Settings);

    internal int SelectedIndex => selectedIndex;

    internal static IReadOnlyList<Draft0GrayboxCameraPreset> All => Presets;

    internal bool HandleKey(SDL_Keycode key, bool repeated)
    {
        if (repeated)
            return false;

        int? directIndex = key switch
        {
            SDL_Keycode.SDLK_F1 => 0,
            SDL_Keycode.SDLK_F2 => 1,
            SDL_Keycode.SDLK_F3 => 2,
            SDL_Keycode.SDLK_F4 => 3,
            SDL_Keycode.SDLK_F5 => 4,
            SDL_Keycode.SDLK_F6 => 5,
            SDL_Keycode.SDLK_F7 => 6,
            _ => null,
        };
        if (directIndex.HasValue)
        {
            selectedIndex = directIndex.Value;
            return true;
        }

        if (key != SDL_Keycode.SDLK_TAB)
            return false;

        selectedIndex = (selectedIndex + 1) % Presets.Length;
        return true;
    }

    private static Draft0GrayboxCameraPreset Preset(
        string name,
        float focusX,
        float focusZ,
        float distance,
        float nearPlane,
        float farPlane) => new(
        name,
        new GroundPoint(focusX, focusZ),
        new PerspectiveIsometricCameraSettings(
            verticalFieldOfViewDegrees: 28.0f,
            downwardPitchDegrees: 42.0f,
            yawDegrees: 45.0f,
            focusDistanceMetres: distance,
            nearPlaneMetres: nearPlane,
            farPlaneMetres: farPlane));
}
