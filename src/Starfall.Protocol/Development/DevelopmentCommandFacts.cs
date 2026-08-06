using System.Collections.Immutable;
using System.Globalization;

namespace Starfall.Protocol.Development;

public readonly record struct DevelopmentCommandSequence
{
    public DevelopmentCommandSequence(ulong value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Development command sequences must be positive.");

        Value = value;
    }

    public ulong Value
    {
        get;
    }

    internal bool IsValid => Value != 0;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public readonly record struct DevelopmentCommandId
{
    public const int MaximumByteLength = 64;

    public DevelopmentCommandId(string value)
    {
        if (!DevelopmentCommandText.IsValidIdentifier(value))
        {
            throw new ArgumentException(
                $"Development command identities must contain 1-{MaximumByteLength} lowercase ASCII letters, digits or underscores and begin with a letter.",
                nameof(value));
        }

        Value = value;
    }

    public string Value
    {
        get;
    }

    internal bool IsValid => DevelopmentCommandText.IsValidIdentifier(Value);

    public override string ToString() => Value ?? string.Empty;
}

public static class DevelopmentCommandIds
{
    public static DevelopmentCommandId Ping { get; } = new("ping");
}

public sealed class DevelopmentCommandRequest
{
    public const int MaximumArgumentCount = 8;
    public const int MaximumArgumentByteLength = 64;

    public DevelopmentCommandRequest(
        DevelopmentCommandSequence sequence,
        DevelopmentCommandId commandId,
        IEnumerable<string> arguments)
    {
        if (!sequence.IsValid)
            throw new ArgumentException("Development command sequence must be valid.", nameof(sequence));
        if (!commandId.IsValid)
            throw new ArgumentException("Development command identity must be valid.", nameof(commandId));
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments is ImmutableArray<string> immutable && immutable.IsDefault)
            throw new ArgumentException("Development command arguments must not be a default immutable array.", nameof(arguments));

        ImmutableArray<string> copiedArguments = arguments.ToImmutableArray();
        if (copiedArguments.Length > MaximumArgumentCount ||
            copiedArguments.Any(static argument => !DevelopmentCommandText.IsValidArgument(argument)))
        {
            throw new ArgumentException(
                $"Development commands accept at most {MaximumArgumentCount} non-empty printable ASCII arguments of at most {MaximumArgumentByteLength} bytes each.",
                nameof(arguments));
        }

        Sequence = sequence;
        CommandId = commandId;
        Arguments = copiedArguments;
    }

    public DevelopmentCommandSequence Sequence
    {
        get;
    }

    public DevelopmentCommandId CommandId
    {
        get;
    }

    public ImmutableArray<string> Arguments
    {
        get;
    }
}

public enum DevelopmentCommandRejectionReason : byte
{
    UnknownCommand = 1,
    InvalidArguments = 2,
    StaleOrDuplicateSequence = 3,
    HandlerRejected = 4,
}

public sealed class DevelopmentCommandSucceeded
{
    public DevelopmentCommandSucceeded(
        DevelopmentCommandSequence sequence,
        DevelopmentCommandId commandId,
        string diagnostic)
    {
        DevelopmentCommandFactValidation.ValidateResult(sequence, commandId, diagnostic);
        Sequence = sequence;
        CommandId = commandId;
        Diagnostic = diagnostic;
    }

    public DevelopmentCommandSequence Sequence
    {
        get;
    }

    public DevelopmentCommandId CommandId
    {
        get;
    }

    public string Diagnostic
    {
        get;
    }
}

public sealed class DevelopmentCommandRejected
{
    public DevelopmentCommandRejected(
        DevelopmentCommandSequence sequence,
        DevelopmentCommandId commandId,
        DevelopmentCommandRejectionReason reason,
        string diagnostic)
    {
        DevelopmentCommandFactValidation.ValidateResult(sequence, commandId, diagnostic);
        if (!Enum.IsDefined(reason))
            throw new ArgumentOutOfRangeException(nameof(reason));

        Sequence = sequence;
        CommandId = commandId;
        Reason = reason;
        Diagnostic = diagnostic;
    }

    public DevelopmentCommandSequence Sequence
    {
        get;
    }

    public DevelopmentCommandId CommandId
    {
        get;
    }

    public DevelopmentCommandRejectionReason Reason
    {
        get;
    }

    public string Diagnostic
    {
        get;
    }
}

internal static class DevelopmentCommandFactValidation
{
    internal static void ValidateResult(
        DevelopmentCommandSequence sequence,
        DevelopmentCommandId commandId,
        string diagnostic)
    {
        if (!sequence.IsValid)
            throw new ArgumentException("Development command sequence must be valid.", nameof(sequence));
        if (!commandId.IsValid)
            throw new ArgumentException("Development command identity must be valid.", nameof(commandId));
        if (!DevelopmentCommandText.IsValidDiagnostic(diagnostic))
        {
            throw new ArgumentException(
                $"Development command diagnostics must contain 1-{DevelopmentCommandText.MaximumDiagnosticByteLength} printable ASCII bytes.",
                nameof(diagnostic));
        }
    }
}

internal static class DevelopmentCommandText
{
    internal const int MaximumDiagnosticByteLength = 512;

    internal static bool IsValidIdentifier(string? value) =>
        value is { Length: > 0 and <= DevelopmentCommandId.MaximumByteLength } &&
        value[0] is >= 'a' and <= 'z' &&
        value.All(static character =>
            character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');

    internal static bool IsValidArgument(string? value) =>
        value is { Length: > 0 and <= DevelopmentCommandRequest.MaximumArgumentByteLength } &&
        value.All(static character => character is >= '!' and <= '~');

    internal static bool IsValidDiagnostic(string? value) =>
        value is { Length: > 0 and <= MaximumDiagnosticByteLength } &&
        value.All(static character => character is >= ' ' and <= '~');
}
