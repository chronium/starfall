using System.Collections.Immutable;
using System.Text;
using Starfall.Client.Networking;
using Starfall.Protocol.Development;

namespace Starfall.Client.DevelopmentUi;

internal enum DevelopmentConsoleEntryKind
{
    Command,
    Success,
    Rejection,
    Error,
}

internal sealed record DevelopmentConsoleInvocation(
    DevelopmentCommandId CommandId,
    ImmutableArray<string> Arguments,
    string CanonicalText);

internal sealed record DevelopmentConsoleEntry(
    DevelopmentConsoleEntryKind Kind,
    string Text,
    double CreatedAtSeconds);

internal sealed record DevelopmentConsoleVisibleEntry(
    DevelopmentConsoleEntryKind Kind,
    string Text,
    float Alpha);

internal sealed class DevelopmentConsoleState
{
    internal const int MaximumInputByteLength = 584;
    internal const int TranscriptCapacity = 128;
    internal const int HistoryCapacity = 32;
    internal const int ClosedEntryCount = 6;
    internal const double ClosedHoldSeconds = 10.0;
    internal const double ClosedFadeSeconds = 2.0;

    private readonly byte[] inputBuffer = new byte[MaximumInputByteLength + 1];
    private readonly List<DevelopmentConsoleEntry> transcript = new(TranscriptCapacity);
    private readonly List<string> history = new(HistoryCapacity);
    private readonly Queue<DevelopmentConsoleInvocation> submissions = [];
    private int historyCursor = -1;
    private string historyDraft = string.Empty;
    private bool focusRequested;

    internal bool IsInputOpen { get; private set; }
    internal byte[] InputBuffer => inputBuffer;
    internal int TranscriptCount => transcript.Count;
    internal int HistoryCount => history.Count;
    internal ulong TranscriptRevision { get; private set; }

    internal string InputValue
    {
        get
        {
            int length = Array.IndexOf(inputBuffer, (byte)0);
            if (length < 0)
                length = inputBuffer.Length;
            return Encoding.UTF8.GetString(inputBuffer, 0, length);
        }
    }

    internal void OpenInput()
    {
        IsInputOpen = true;
        focusRequested = true;
    }

    internal void CloseInput()
    {
        IsInputOpen = false;
        focusRequested = false;
        historyCursor = -1;
        historyDraft = string.Empty;
        Array.Clear(inputBuffer);
    }

    internal bool ConsumeFocusRequest()
    {
        bool requested = focusRequested;
        focusRequested = false;
        return requested;
    }

    internal void Submit(double nowSeconds)
    {
        ValidateNow(nowSeconds);
        string trimmed = InputValue.Trim(' ');
        if (trimmed.Length == 0)
        {
            CloseInput();
            return;
        }

        RememberHistory(trimmed);
        if (DevelopmentConsoleCommandParser.TryParse(
                trimmed,
                out DevelopmentConsoleInvocation? invocation,
                out string error))
        {
            submissions.Enqueue(invocation!);
        }
        else
        {
            Append(DevelopmentConsoleEntryKind.Error, $"error: {error}", nowSeconds);
        }

        CloseInput();
    }

    internal bool TryDequeueSubmission(out DevelopmentConsoleInvocation? invocation) =>
        submissions.TryDequeue(out invocation);

    internal void NavigateHistory(int direction)
    {
        if (!IsInputOpen || history.Count == 0 || direction is not (-1 or 1))
            return;

        if (direction < 0)
        {
            if (historyCursor < 0)
            {
                historyDraft = InputValue;
                historyCursor = history.Count - 1;
            }
            else if (historyCursor > 0)
            {
                historyCursor--;
            }
            SetInput(history[historyCursor]);
            return;
        }

        if (historyCursor < 0)
            return;
        historyCursor++;
        if (historyCursor >= history.Count)
        {
            historyCursor = -1;
            SetInput(historyDraft);
            historyDraft = string.Empty;
        }
        else
        {
            SetInput(history[historyCursor]);
        }
    }

    internal void MarkInputEdited()
    {
        historyCursor = -1;
        historyDraft = string.Empty;
    }

    internal void RecordCommandSent(
        DevelopmentCommandSequence sequence,
        DevelopmentConsoleInvocation invocation,
        double nowSeconds)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        Append(
            DevelopmentConsoleEntryKind.Command,
            $"> [{sequence}] {invocation.CanonicalText}",
            nowSeconds);
    }

    internal void RecordLocalFailure(
        DevelopmentConsoleInvocation invocation,
        string diagnostic,
        double nowSeconds)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        ArgumentException.ThrowIfNullOrWhiteSpace(diagnostic);
        Append(DevelopmentConsoleEntryKind.Command, $"> {invocation.CanonicalText}", nowSeconds);
        Append(DevelopmentConsoleEntryKind.Error, $"error: {diagnostic}", nowSeconds);
    }

    internal void RecordResult(ConnectedDevelopmentCommandResult result, double nowSeconds)
    {
        ArgumentNullException.ThrowIfNull(result);
        string text = result.Kind switch
        {
            ConnectedDevelopmentCommandResultKind.Succeeded =>
                $"[{result.Sequence}] ok: {result.Diagnostic}",
            ConnectedDevelopmentCommandResultKind.Rejected =>
                $"[{result.Sequence}] rejected {result.RejectionReason}: {result.Diagnostic}",
            _ => throw new ArgumentOutOfRangeException(nameof(result)),
        };
        Append(
            result.Kind == ConnectedDevelopmentCommandResultKind.Succeeded
                ? DevelopmentConsoleEntryKind.Success
                : DevelopmentConsoleEntryKind.Rejection,
            text,
            nowSeconds);
    }

    internal IReadOnlyList<DevelopmentConsoleVisibleEntry> GetVisibleEntries(double nowSeconds)
    {
        ValidateNow(nowSeconds);
        if (IsInputOpen)
        {
            return transcript
                .Select(static entry => new DevelopmentConsoleVisibleEntry(entry.Kind, entry.Text, 1.0f))
                .ToArray();
        }

        return transcript
            .Select(entry => new DevelopmentConsoleVisibleEntry(
                entry.Kind,
                entry.Text,
                CalculateClosedAlpha(entry.CreatedAtSeconds, nowSeconds)))
            .Where(static entry => entry.Alpha > 0.0f)
            .TakeLast(ClosedEntryCount)
            .ToArray();
    }

    internal void SetInput(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > MaximumInputByteLength)
            throw new ArgumentException("Development console input exceeds its byte limit.", nameof(value));

        Array.Clear(inputBuffer);
        Encoding.UTF8.GetBytes(value, inputBuffer);
    }

    private static float CalculateClosedAlpha(double createdAtSeconds, double nowSeconds)
    {
        double age = Math.Max(0.0, nowSeconds - createdAtSeconds);
        if (age <= ClosedHoldSeconds)
            return 1.0f;
        if (age >= ClosedHoldSeconds + ClosedFadeSeconds)
            return 0.0f;
        return (float)(1.0 - ((age - ClosedHoldSeconds) / ClosedFadeSeconds));
    }

    private void RememberHistory(string value)
    {
        if (history.Count == HistoryCapacity)
            history.RemoveAt(0);
        history.Add(value);
        historyCursor = -1;
        historyDraft = string.Empty;
    }

    private void Append(DevelopmentConsoleEntryKind kind, string text, double nowSeconds)
    {
        ValidateNow(nowSeconds);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (transcript.Count == TranscriptCapacity)
            transcript.RemoveAt(0);
        transcript.Add(new DevelopmentConsoleEntry(kind, text, nowSeconds));
        TranscriptRevision++;
    }

    private static void ValidateNow(double nowSeconds)
    {
        if (!double.IsFinite(nowSeconds) || nowSeconds < 0.0)
            throw new ArgumentOutOfRangeException(nameof(nowSeconds));
    }
}

internal static class DevelopmentConsoleCommandParser
{
    internal static bool TryParse(
        string input,
        out DevelopmentConsoleInvocation? invocation,
        out string error)
    {
        invocation = null;
        error = string.Empty;
        ArgumentNullException.ThrowIfNull(input);
        if (Encoding.UTF8.GetByteCount(input) > DevelopmentConsoleState.MaximumInputByteLength)
        {
            error = $"command line exceeds {DevelopmentConsoleState.MaximumInputByteLength} ASCII bytes";
            return false;
        }
        if (input.Any(static character => character is < ' ' or > '~'))
        {
            error = "command line must contain printable ASCII and use spaces as separators";
            return false;
        }

        string[] tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            error = "command line is empty";
            return false;
        }

        try
        {
            var commandId = new DevelopmentCommandId(tokens[0]);
            ImmutableArray<string> arguments = tokens.Skip(1).ToImmutableArray();
            _ = new DevelopmentCommandRequest(new DevelopmentCommandSequence(1), commandId, arguments);
            invocation = new DevelopmentConsoleInvocation(
                commandId,
                arguments,
                string.Join(' ', tokens));
            return true;
        }
        catch (ArgumentException exception)
        {
            error = exception.ParamName == "value"
                ? "command identity must begin with a lowercase letter and contain only lowercase letters, digits or underscores"
                : "command accepts at most eight printable non-whitespace ASCII arguments of at most 64 bytes each";
            return false;
        }
    }
}
