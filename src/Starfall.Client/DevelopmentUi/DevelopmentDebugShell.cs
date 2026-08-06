using System.Globalization;
using Evergine.Bindings.Imgui;
using ImGuiVector2 = Evergine.Mathematics.Vector2;
using ImGuiVector4 = Evergine.Mathematics.Vector4;

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
    private const float ConsoleMargin = 16.0f;
    private const float ConsoleWidthFraction = 0.42f;
    private const float ConsoleMinimumWidth = 480.0f;
    private const float ConsoleMaximumWidth = 900.0f;
    private const float ConsoleOpenHeight = 280.0f;
    private const float ConsoleOpenBackgroundAlpha = 0.72f;
    private readonly DevelopmentDebugShellState state;
    private ulong renderedTranscriptRevision;

    internal DevelopmentDebugShell(DevelopmentDebugShellState state)
    {
        this.state = state ?? throw new ArgumentNullException(nameof(state));
    }

    internal void Draw(DevelopmentDebugSnapshot snapshot, double nowSeconds)
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
        DrawConsole(nowSeconds);
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
                if (ImguiNative.igMenuItem_Bool("Command Console", "T", selected: false, enabled: true))
                    state.Console.OpenInput();
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

    private void DrawConsole(double nowSeconds)
    {
        IReadOnlyList<DevelopmentConsoleVisibleEntry> entries = state.Console.GetVisibleEntries(nowSeconds);
        if (!state.Console.IsInputOpen && entries.Count == 0)
            return;

        ImGuiIO* io = ImguiNative.igGetIO_Nil();
        if (io is null)
            throw new InvalidOperationException("ImGui IO is unavailable while drawing the development console.");

        float availableWidth = Math.Max(1.0f, io->DisplaySize.X - (ConsoleMargin * 2.0f));
        float width = Math.Min(
            availableWidth,
            Math.Clamp(
                io->DisplaySize.X * ConsoleWidthFraction,
                ConsoleMinimumWidth,
                ConsoleMaximumWidth));
        float lineHeight = ImguiNative.igGetTextLineHeightWithSpacing();
        float height = state.Console.IsInputOpen
            ? Math.Min(ConsoleOpenHeight, Math.Max(lineHeight * 4.0f, io->DisplaySize.Y - (ConsoleMargin * 2.0f)))
            : Math.Min(
                Math.Max(lineHeight + 12.0f, entries.Count * lineHeight + 12.0f),
                Math.Max(lineHeight + 12.0f, io->DisplaySize.Y - (ConsoleMargin * 2.0f)));

        ImguiNative.igSetNextWindowPos(
            new ImGuiVector2(ConsoleMargin, io->DisplaySize.Y - ConsoleMargin),
            ImGuiCond.Always,
            new ImGuiVector2(0.0f, 1.0f));
        ImguiNative.igSetNextWindowSize(new ImGuiVector2(width, height), ImGuiCond.Always);
        ImguiNative.igSetNextWindowBgAlpha(state.Console.IsInputOpen ? ConsoleOpenBackgroundAlpha : 0.0f);

        ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoDocking;
        if (!state.Console.IsInputOpen)
        {
            flags |=
                ImGuiWindowFlags.NoBackground |
                ImGuiWindowFlags.NoInputs |
                ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoBringToFrontOnFocus;
        }

        bool expanded = ImguiNative.igBegin("Command Console###starfall-command-console", null, flags);
        try
        {
            if (!expanded)
                return;

            bool focusRequested = state.Console.IsInputOpen && state.Console.ConsumeFocusRequest();
            if (state.Console.IsInputOpen)
            {
                ImGuiVector2 available = ImguiNative.igGetContentRegionAvail();
                float inputHeight = ImguiNative.igGetTextLineHeightWithSpacing() + 8.0f;
                bool childVisible = ImguiNative.igBeginChild_Str(
                    "##development-console-transcript",
                    new ImGuiVector2(available.X, Math.Max(lineHeight, available.Y - inputHeight)),
                    ImGuiChildFlags.None,
                    ImGuiWindowFlags.None);
                try
                {
                    float scrollY = ImguiNative.igGetScrollY();
                    float scrollMaximum = ImguiNative.igGetScrollMaxY();
                    bool wasAtBottom = scrollMaximum <= 0.0f || scrollY >= scrollMaximum - 1.0f;
                    if (childVisible)
                    {
                        foreach (DevelopmentConsoleVisibleEntry entry in entries)
                            DrawConsoleEntry(entry);
                    }
                    if (focusRequested ||
                        (state.Console.TranscriptRevision != renderedTranscriptRevision && wasAtBottom))
                    {
                        ImguiNative.igSetScrollHereY(1.0f);
                    }
                }
                finally
                {
                    ImguiNative.igEndChild();
                }

                renderedTranscriptRevision = state.Console.TranscriptRevision;
                ImguiNative.igSetNextItemWidth(-1.0f);
                if (focusRequested)
                    ImguiNative.igSetKeyboardFocusHere(0);
                fixed (byte* buffer = state.Console.InputBuffer)
                {
                    bool submitted = ImguiNative.igInputTextWithHint(
                        "##development-command-input",
                        "> command",
                        buffer,
                        (uint)state.Console.InputBuffer.Length,
                        ImGuiInputTextFlags.EnterReturnsTrue,
                        null,
                        null);
                    if (ImguiNative.igIsItemEdited())
                        state.Console.MarkInputEdited();
                    if (submitted)
                        state.Console.Submit(nowSeconds);
                }
            }
            else
            {
                foreach (DevelopmentConsoleVisibleEntry entry in entries)
                    DrawConsoleEntry(entry);
                renderedTranscriptRevision = state.Console.TranscriptRevision;
            }
        }
        finally
        {
            ImguiNative.igEnd();
        }
    }

    private static void DrawConsoleEntry(DevelopmentConsoleVisibleEntry entry)
    {
        ImGuiVector4 color = entry.Kind switch
        {
            DevelopmentConsoleEntryKind.Command => new ImGuiVector4(0.78f, 0.80f, 0.84f, entry.Alpha),
            DevelopmentConsoleEntryKind.Success => new ImGuiVector4(0.74f, 0.90f, 0.72f, entry.Alpha),
            DevelopmentConsoleEntryKind.Rejection => new ImGuiVector4(0.96f, 0.74f, 0.42f, entry.Alpha),
            DevelopmentConsoleEntryKind.Error => new ImGuiVector4(0.96f, 0.50f, 0.45f, entry.Alpha),
            _ => throw new ArgumentOutOfRangeException(nameof(entry)),
        };
        ImGuiVector2 cursor = ImguiNative.igGetCursorScreenPos();
        float wrapWidth = Math.Max(1.0f, ImguiNative.igGetContentRegionAvail().X);
        ImDrawList* drawList = ImguiNative.igGetWindowDrawList();
        ImguiNative.ImDrawList_AddText_FontPtr(
            drawList,
            ImguiNative.igGetFont(),
            ImguiNative.igGetFontSize(),
            new ImGuiVector2(cursor.X + 1.0f, cursor.Y + 1.0f),
            ImguiNative.igColorConvertFloat4ToU32(new ImGuiVector4(0.0f, 0.0f, 0.0f, entry.Alpha * 0.85f)),
            entry.Text,
            null,
            wrapWidth,
            null);
        ImguiNative.igPushStyleColor_Vec4(ImGuiCol.Text, color);
        ImguiNative.igPushTextWrapPos(0.0f);
        try
        {
            ImguiNative.igTextUnformatted(entry.Text, null);
        }
        finally
        {
            ImguiNative.igPopTextWrapPos();
            ImguiNative.igPopStyleColor(1);
        }
    }
}
