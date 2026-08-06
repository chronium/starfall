using System.Globalization;
using Evergine.Bindings.Imgui;
using ImGuiVector2 = Evergine.Mathematics.Vector2;

namespace Starfall.Client.DevelopmentUi;

internal readonly record struct DevelopmentDebugSnapshot(
    string Mode,
    string SessionStatus,
    string SessionIdentity,
    string EntityIdentity,
    ulong Tick,
    string CameraPreset,
    float CameraDistanceMetres,
    float? LocalSpeedMetresPerSecond);

internal sealed unsafe class DevelopmentDebugShell
{
    private readonly DevelopmentDebugShellState state;

    internal DevelopmentDebugShell(DevelopmentDebugShellState state)
    {
        this.state = state ?? throw new ArgumentNullException(nameof(state));
    }

    internal void Draw(DevelopmentDebugSnapshot snapshot)
    {
        if (!state.IsVisible)
            return;

        DrawMenuBar();
        if (!state.IsVisible)
            return;

        if (state.WorldSessionVisible)
            DrawWorldSession(snapshot);
        if (state.PresentationRenderingVisible)
            DrawPresentationRendering(snapshot);
    }

    private void DrawMenuBar()
    {
        if (!ImguiNative.igBeginMainMenuBar())
            return;

        try
        {
            if (!ImguiNative.igBeginMenu("Debug", enabled: true))
                return;

            try
            {
                DrawWindowMenuItem("World / Session", DevelopmentDebugWindow.WorldSession);
                DrawWindowMenuItem("Presentation / Rendering", DevelopmentDebugWindow.PresentationRendering);
                ImguiNative.igSeparator();
                if (ImguiNative.igMenuItem_Bool("Hide debug UI", "F12", selected: false, enabled: true))
                    state.Hide();
            }
            finally
            {
                ImguiNative.igEndMenu();
            }
        }
        finally
        {
            ImguiNative.igEndMainMenuBar();
        }
    }

    private void DrawWindowMenuItem(string label, DevelopmentDebugWindow window)
    {
        byte selected = state.IsWindowVisible(window) ? (byte)1 : (byte)0;
        if (ImguiNative.igMenuItem_BoolPtr(label, string.Empty, &selected, enabled: true))
            state.SetWindowVisible(window, selected != 0);
    }

    private void DrawWorldSession(DevelopmentDebugSnapshot snapshot)
    {
        ImguiNative.igSetNextWindowSize(new ImGuiVector2(340, 190), ImGuiCond.FirstUseEver);
        byte open = 1;
        bool expanded = ImguiNative.igBegin(
            "World / Session###starfall-world-session",
            &open,
            ImGuiWindowFlags.NoSavedSettings);
        try
        {
            if (expanded)
            {
                ImguiNative.igText($"Mode: {snapshot.Mode}");
                ImguiNative.igText($"Status: {snapshot.SessionStatus}");
                ImguiNative.igText($"Session: {snapshot.SessionIdentity}");
                ImguiNative.igText($"Entity: {snapshot.EntityIdentity}");
                ImguiNative.igText($"Tick: {snapshot.Tick.ToString(CultureInfo.InvariantCulture)}");
            }
        }
        finally
        {
            ImguiNative.igEnd();
            state.SetWindowVisible(DevelopmentDebugWindow.WorldSession, open != 0);
        }
    }

    private void DrawPresentationRendering(DevelopmentDebugSnapshot snapshot)
    {
        ImguiNative.igSetNextWindowSize(new ImGuiVector2(340, 150), ImGuiCond.FirstUseEver);
        byte open = 1;
        bool expanded = ImguiNative.igBegin(
            "Presentation / Rendering###starfall-presentation-rendering",
            &open,
            ImGuiWindowFlags.NoSavedSettings);
        try
        {
            if (expanded)
            {
                ImguiNative.igText($"Camera: {snapshot.CameraPreset}");
                ImguiNative.igText(string.Create(
                    CultureInfo.InvariantCulture,
                    $"Distance: {snapshot.CameraDistanceMetres:F1} m"));
                ImguiNative.igText(snapshot.LocalSpeedMetresPerSecond.HasValue
                    ? string.Create(
                        CultureInfo.InvariantCulture,
                        $"Local fixture speed: {snapshot.LocalSpeedMetresPerSecond.Value:F1} m/s")
                    : "Local fixture speed: unavailable in connected mode");
            }
        }
        finally
        {
            ImguiNative.igEnd();
            state.SetWindowVisible(DevelopmentDebugWindow.PresentationRendering, open != 0);
        }
    }
}
