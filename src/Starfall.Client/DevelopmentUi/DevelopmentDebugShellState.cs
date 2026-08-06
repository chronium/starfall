namespace Starfall.Client.DevelopmentUi;

internal enum DevelopmentDebugWindow
{
    WorldSession,
    PresentationRendering,
}

internal sealed class DevelopmentDebugShellState
{
    internal DevelopmentDebugShellState(bool initiallyVisible)
    {
        IsVisible = initiallyVisible;
    }

    internal bool IsVisible
    {
        get;
        private set;
    }

    internal DevelopmentConsoleState Console { get; } = new();

    internal bool WorldSessionVisible
    {
        get;
        private set;
    } = true;

    internal bool PresentationRenderingVisible
    {
        get;
        private set;
    }

    internal bool ToggleVisibility(bool repeated)
    {
        if (repeated)
            return false;
        IsVisible = !IsVisible;
        if (!IsVisible)
            Console.CloseInput();
        return true;
    }

    internal void Hide()
    {
        IsVisible = false;
        Console.CloseInput();
    }

    internal bool TryOpenConsole(bool repeated, bool keyboardOwned)
    {
        if (repeated || keyboardOwned || !IsVisible || Console.IsInputOpen)
            return false;
        Console.OpenInput();
        return true;
    }

    internal bool IsWindowVisible(DevelopmentDebugWindow window) => window switch
    {
        DevelopmentDebugWindow.WorldSession => WorldSessionVisible,
        DevelopmentDebugWindow.PresentationRendering => PresentationRenderingVisible,
        _ => throw new ArgumentOutOfRangeException(nameof(window), window, "The debug window is not defined."),
    };

    internal void SetWindowVisible(DevelopmentDebugWindow window, bool visible)
    {
        switch (window)
        {
            case DevelopmentDebugWindow.WorldSession:
                WorldSessionVisible = visible;
                break;
            case DevelopmentDebugWindow.PresentationRendering:
                PresentationRenderingVisible = visible;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(window), window, "The debug window is not defined.");
        }
    }
}

internal readonly record struct DevelopmentDebugInputOwnership(
    bool SuppressMouseGameplay,
    bool SuppressKeyboardGameplay)
{
    internal static DevelopmentDebugInputOwnership Resolve(
        bool shellVisible,
        bool consoleInputOpen,
        bool wantsMouse,
        bool wantsKeyboard,
        bool wantsTextInput) =>
        shellVisible
            ? new DevelopmentDebugInputOwnership(
                SuppressMouseGameplay: consoleInputOpen || wantsMouse,
                SuppressKeyboardGameplay: consoleInputOpen || wantsKeyboard || wantsTextInput)
            : default;
}
