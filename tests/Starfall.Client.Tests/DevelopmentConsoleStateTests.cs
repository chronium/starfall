using System.Collections.Immutable;
using Starfall.Client.DevelopmentUi;
using Starfall.Client.Networking;
using Starfall.Protocol.Development;

namespace Starfall.Client.Tests;

public sealed class DevelopmentConsoleStateTests
{
    [Fact]
    public void Parser_normalizes_spaces_and_preserves_canonical_argument_order()
    {
        Assert.True(DevelopmentConsoleCommandParser.TryParse(
            "  ping   first second  ",
            out DevelopmentConsoleInvocation? invocation,
            out string error));

        Assert.Empty(error);
        Assert.Equal(DevelopmentCommandIds.Ping, invocation!.CommandId);
        Assert.Equal<string>(["first", "second"], invocation.Arguments);
        Assert.Equal("ping first second", invocation.CanonicalText);
    }

    [Theory]
    [InlineData("Ping", "identity")]
    [InlineData("ping\tvalue", "printable ASCII")]
    [InlineData("ping café", "printable ASCII")]
    [InlineData("ping a b c d e f g h i", "eight")]
    public void Parser_rejects_noncanonical_input(string input, string expectedDiagnostic)
    {
        Assert.False(DevelopmentConsoleCommandParser.TryParse(
            input,
            out DevelopmentConsoleInvocation? invocation,
            out string error));

        Assert.Null(invocation);
        Assert.Contains(expectedDiagnostic, error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parser_enforces_command_line_and_argument_byte_limits()
    {
        Assert.False(DevelopmentConsoleCommandParser.TryParse(
            "p" + new string('x', DevelopmentConsoleState.MaximumInputByteLength),
            out _,
            out string lineError));
        Assert.Contains(DevelopmentConsoleState.MaximumInputByteLength.ToString(), lineError, StringComparison.Ordinal);

        Assert.False(DevelopmentConsoleCommandParser.TryParse(
            "ping " + new string('x', DevelopmentCommandRequest.MaximumArgumentByteLength + 1),
            out _,
            out string argumentError));
        Assert.Contains("64", argumentError, StringComparison.Ordinal);
    }

    [Fact]
    public void Submit_closes_input_records_history_and_queues_only_valid_nonempty_commands()
    {
        var state = new DevelopmentConsoleState();
        state.OpenInput();
        state.SetInput("ping");

        state.Submit(1.0);

        Assert.False(state.IsInputOpen);
        Assert.Equal(1, state.HistoryCount);
        Assert.True(state.TryDequeueSubmission(out DevelopmentConsoleInvocation? invocation));
        Assert.Equal("ping", invocation!.CanonicalText);

        state.OpenInput();
        state.SetInput("   ");
        state.Submit(2.0);
        Assert.Equal(1, state.HistoryCount);
        Assert.False(state.TryDequeueSubmission(out _));

        state.OpenInput();
        state.SetInput("Ping");
        state.Submit(3.0);
        Assert.Equal(2, state.HistoryCount);
        Assert.False(state.TryDequeueSubmission(out _));
        DevelopmentConsoleVisibleEntry error = Assert.Single(state.GetVisibleEntries(3.0));
        Assert.Equal(DevelopmentConsoleEntryKind.Error, error.Kind);
    }

    [Fact]
    public void History_is_bounded_and_navigation_restores_the_draft()
    {
        var state = new DevelopmentConsoleState();
        for (int index = 0; index < DevelopmentConsoleState.HistoryCapacity + 3; index++)
        {
            state.OpenInput();
            state.SetInput($"ping value_{index}");
            state.Submit(index);
            Assert.True(state.TryDequeueSubmission(out _));
        }
        Assert.Equal(DevelopmentConsoleState.HistoryCapacity, state.HistoryCount);

        state.OpenInput();
        state.SetInput("ping draft");
        state.NavigateHistory(-1);
        Assert.Equal($"ping value_{DevelopmentConsoleState.HistoryCapacity + 2}", state.InputValue);
        state.NavigateHistory(1);
        Assert.Equal("ping draft", state.InputValue);
    }

    [Fact]
    public void Transcript_is_bounded_and_closed_view_holds_then_fades_six_latest_entries()
    {
        var state = new DevelopmentConsoleState();
        var invocation = new DevelopmentConsoleInvocation(
            DevelopmentCommandIds.Ping,
            ImmutableArray<string>.Empty,
            "ping");
        for (int index = 0; index < DevelopmentConsoleState.TranscriptCapacity + 2; index++)
            state.RecordCommandSent(new DevelopmentCommandSequence((ulong)index + 1), invocation, 0.0);

        Assert.Equal(DevelopmentConsoleState.TranscriptCapacity, state.TranscriptCount);
        IReadOnlyList<DevelopmentConsoleVisibleEntry> held = state.GetVisibleEntries(10.0);
        Assert.Equal(DevelopmentConsoleState.ClosedEntryCount, held.Count);
        Assert.All(held, static entry => Assert.Equal(1.0f, entry.Alpha));

        IReadOnlyList<DevelopmentConsoleVisibleEntry> fading = state.GetVisibleEntries(11.0);
        Assert.Equal(DevelopmentConsoleState.ClosedEntryCount, fading.Count);
        Assert.All(fading, static entry => Assert.Equal(0.5f, entry.Alpha));
        Assert.Empty(state.GetVisibleEntries(12.0));

        state.OpenInput();
        Assert.Equal(DevelopmentConsoleState.TranscriptCapacity, state.GetVisibleEntries(100.0).Count);
    }

    [Fact]
    public void Transcript_distinguishes_local_failures_successes_and_rejections()
    {
        var state = new DevelopmentConsoleState();
        var invocation = new DevelopmentConsoleInvocation(
            DevelopmentCommandIds.Ping,
            ImmutableArray<string>.Empty,
            "ping");
        state.RecordLocalFailure(invocation, "not connected", 1.0);
        state.RecordResult(
            ConnectedDevelopmentCommandResult.Succeeded(new DevelopmentCommandSucceeded(
                new DevelopmentCommandSequence(1), DevelopmentCommandIds.Ping, "pong")),
            2.0);
        state.RecordResult(
            ConnectedDevelopmentCommandResult.Rejected(new DevelopmentCommandRejected(
                new DevelopmentCommandSequence(2),
                DevelopmentCommandIds.Ping,
                DevelopmentCommandRejectionReason.InvalidArguments,
                "ping accepts no arguments")),
            3.0);

        state.OpenInput();
        DevelopmentConsoleVisibleEntry[] entries = state.GetVisibleEntries(4.0).ToArray();
        Assert.Equal(
            [
                DevelopmentConsoleEntryKind.Command,
                DevelopmentConsoleEntryKind.Error,
                DevelopmentConsoleEntryKind.Success,
                DevelopmentConsoleEntryKind.Rejection,
            ],
            entries.Select(static entry => entry.Kind));
        Assert.Contains("ok: pong", entries[2].Text, StringComparison.Ordinal);
        Assert.Contains("rejected InvalidArguments", entries[3].Text, StringComparison.Ordinal);
    }
}
