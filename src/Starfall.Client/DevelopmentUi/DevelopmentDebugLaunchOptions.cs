namespace Starfall.Client.DevelopmentUi;

internal readonly record struct DevelopmentDebugLaunchOptions(
    bool InitiallyVisible,
    string[] RemainingArguments)
{
    internal const string HiddenArgument = "--debug-ui-hidden";

    internal static DevelopmentDebugLaunchOptions Extract(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var remaining = new List<string>(args.Count);
        bool hidden = false;
        foreach (string argument in args)
        {
            if (!string.Equals(argument, HiddenArgument, StringComparison.Ordinal))
            {
                remaining.Add(argument);
                continue;
            }

            if (hidden)
                throw new ArgumentException($"Duplicate client option '{HiddenArgument}'.");
            hidden = true;
        }

        if (hidden && remaining.Any(static argument =>
                string.Equals(argument, "--validate-character-content", StringComparison.Ordinal) ||
                string.Equals(argument, "--capture-graybox-suite", StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                $"{HiddenArgument} is available only for interactive local or connected previews.");
        }

        return new DevelopmentDebugLaunchOptions(!hidden, remaining.ToArray());
    }
}
