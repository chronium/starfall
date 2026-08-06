using Starfall.Client.DevelopmentUi;

namespace Starfall.Client.Tests;

public sealed class DevelopmentDebugShellTests
{
    [Fact]
    public void Launch_options_default_to_visible_and_preserve_interactive_arguments()
    {
        DevelopmentDebugLaunchOptions local = DevelopmentDebugLaunchOptions.Extract([]);
        DevelopmentDebugLaunchOptions connected = DevelopmentDebugLaunchOptions.Extract(
        [
            "--connect-address", "127.0.0.1",
            "--connect-port", "7777",
            "--join-ticket-file", "ticket",
        ]);

        Assert.True(local.InitiallyVisible);
        Assert.Empty(local.RemainingArguments);
        Assert.True(connected.InitiallyVisible);
        Assert.Equal(
        [
            "--connect-address", "127.0.0.1",
            "--connect-port", "7777",
            "--join-ticket-file", "ticket",
        ],
            connected.RemainingArguments);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Hidden_launch_modifier_is_removed_from_connected_arguments(bool first)
    {
        string[] connection =
        [
            "--connect-address", "127.0.0.1",
            "--connect-port", "7777",
            "--join-ticket-file", "ticket",
        ];
        string[] arguments = first
            ? [DevelopmentDebugLaunchOptions.HiddenArgument, .. connection]
            : [.. connection, DevelopmentDebugLaunchOptions.HiddenArgument];

        DevelopmentDebugLaunchOptions options = DevelopmentDebugLaunchOptions.Extract(arguments);

        Assert.False(options.InitiallyVisible);
        Assert.Equal(connection, options.RemainingArguments);
    }

    [Fact]
    public void Hidden_launch_modifier_is_valid_as_the_only_local_argument()
    {
        DevelopmentDebugLaunchOptions options = DevelopmentDebugLaunchOptions.Extract(
            [DevelopmentDebugLaunchOptions.HiddenArgument]);

        Assert.False(options.InitiallyVisible);
        Assert.Empty(options.RemainingArguments);
    }

    [Fact]
    public void Hidden_launch_modifier_rejects_duplicates_and_headless_modes()
    {
        Assert.Throws<ArgumentException>(() => DevelopmentDebugLaunchOptions.Extract(
        [
            DevelopmentDebugLaunchOptions.HiddenArgument,
            DevelopmentDebugLaunchOptions.HiddenArgument,
        ]));
        Assert.Throws<ArgumentException>(() => DevelopmentDebugLaunchOptions.Extract(
        [
            "--validate-character-content",
            DevelopmentDebugLaunchOptions.HiddenArgument,
        ]));
        Assert.Throws<ArgumentException>(() => DevelopmentDebugLaunchOptions.Extract(
        [
            "--capture-graybox-suite", "captures",
            DevelopmentDebugLaunchOptions.HiddenArgument,
        ]));
    }

    [Fact]
    public void Shell_visibility_retains_independent_window_state()
    {
        var state = new DevelopmentDebugShellState(initiallyVisible: true);
        Assert.True(state.IsVisible);
        Assert.True(state.WorldSessionVisible);
        Assert.False(state.PresentationRenderingVisible);

        state.SetWindowVisible(DevelopmentDebugWindow.WorldSession, visible: false);
        state.SetWindowVisible(DevelopmentDebugWindow.PresentationRendering, visible: true);
        Assert.True(state.ToggleVisibility(repeated: false));
        Assert.False(state.IsVisible);
        Assert.False(state.WorldSessionVisible);
        Assert.True(state.PresentationRenderingVisible);

        Assert.False(state.ToggleVisibility(repeated: true));
        Assert.False(state.IsVisible);
        Assert.True(state.ToggleVisibility(repeated: false));
        Assert.True(state.IsVisible);
        Assert.False(state.WorldSessionVisible);
        Assert.True(state.PresentationRenderingVisible);
    }

    [Theory]
    [InlineData(false, true, true, true, true, false, false)]
    [InlineData(true, false, false, false, false, false, false)]
    [InlineData(true, false, true, false, false, true, false)]
    [InlineData(true, false, false, true, false, false, true)]
    [InlineData(true, false, false, false, true, false, true)]
    [InlineData(true, false, true, true, true, true, true)]
    [InlineData(true, true, false, false, false, true, true)]
    public void Input_ownership_suppresses_only_visible_captured_domains(
        bool visible,
        bool consoleInputOpen,
        bool wantsMouse,
        bool wantsKeyboard,
        bool wantsTextInput,
        bool expectedMouse,
        bool expectedKeyboard)
    {
        DevelopmentDebugInputOwnership ownership = DevelopmentDebugInputOwnership.Resolve(
            visible,
            consoleInputOpen,
            wantsMouse,
            wantsKeyboard,
            wantsTextInput);

        Assert.Equal(expectedMouse, ownership.SuppressMouseGameplay);
        Assert.Equal(expectedKeyboard, ownership.SuppressKeyboardGameplay);
    }

    [Fact]
    public void Console_shortcut_respects_master_visibility_capture_and_repeats()
    {
        var state = new DevelopmentDebugShellState(initiallyVisible: true);

        Assert.False(state.TryOpenConsole(repeated: true, keyboardOwned: false));
        Assert.False(state.Console.IsInputOpen);
        Assert.False(state.TryOpenConsole(repeated: false, keyboardOwned: true));
        Assert.False(state.Console.IsInputOpen);
        Assert.True(state.TryOpenConsole(repeated: false, keyboardOwned: false));
        Assert.True(state.Console.IsInputOpen);
        Assert.False(state.TryOpenConsole(repeated: false, keyboardOwned: false));

        state.ToggleVisibility(repeated: false);
        Assert.False(state.IsVisible);
        Assert.False(state.Console.IsInputOpen);
        Assert.False(state.TryOpenConsole(repeated: false, keyboardOwned: false));
    }
}
