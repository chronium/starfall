using Starfall.Protocol.Admission;

namespace Starfall.World.Launch;

internal sealed record WorldLaunchOptions(
    WorldId WorldId,
    ChannelId ChannelId,
    int? RunTicks,
    int? ListenPort,
    IReadOnlyDictionary<string, string> VerificationKeyPaths)
{
    internal bool IsConnected => ListenPort is not null;

    internal static WorldLaunchOptions Parse(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        WorldId? worldId = null;
        ChannelId? channelId = null;
        int? runTicks = null;
        int? listenPort = null;
        var verificationKeys = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var index = 0; index < arguments.Count; index++)
        {
            string option = arguments[index];
            string value = ReadValue(arguments, ref index, option);

            switch (option)
            {
                case "--world":
                    if (worldId is not null)
                        throw new WorldLaunchOptionsException("--world may be specified only once.");

                    worldId = ParseWorldId(value);
                    break;

                case "--channel":
                    if (channelId is not null)
                        throw new WorldLaunchOptionsException("--channel may be specified only once.");

                    channelId = ParseChannelId(value);
                    break;

                case "--run-ticks":
                    if (runTicks is not null)
                        throw new WorldLaunchOptionsException("--run-ticks may be specified only once.");
                    if (!int.TryParse(value, out int parsedRunTicks) || parsedRunTicks <= 0)
                    {
                        throw new WorldLaunchOptionsException(
                            "--run-ticks must be a positive 32-bit integer.");
                    }

                    runTicks = parsedRunTicks;
                    break;

                case "--listen-port":
                    if (listenPort is not null)
                        throw new WorldLaunchOptionsException("--listen-port may be specified only once.");
                    if (!int.TryParse(value, out int parsedPort) || parsedPort is < 1 or > 65535)
                        throw new WorldLaunchOptionsException("--listen-port must be between 1 and 65535.");
                    listenPort = parsedPort;
                    break;

                case "--verification-key":
                    int separator = value.IndexOf('=');
                    if (separator <= 0 || separator == value.Length - 1 ||
                        !verificationKeys.TryAdd(value[..separator], Path.GetFullPath(value[(separator + 1)..])))
                    {
                        throw new WorldLaunchOptionsException(
                            "--verification-key must be a unique <key-id>=<public-pem-path> pair.");
                    }
                    break;

                default:
                    throw new WorldLaunchOptionsException(
                        $"does not recognize argument '{option}'.");
            }
        }

        if (worldId is null || channelId is null)
        {
            throw new WorldLaunchOptionsException(
                "requires both --world <id> and --channel <id>.");
        }

        if (listenPort is not null && verificationKeys.Count == 0)
            throw new WorldLaunchOptionsException("--listen-port requires at least one --verification-key.");
        if (listenPort is null && verificationKeys.Count != 0)
            throw new WorldLaunchOptionsException("--verification-key requires --listen-port.");
        if (listenPort is not null && runTicks is not null)
            throw new WorldLaunchOptionsException("--listen-port cannot be combined with --run-ticks.");

        return new(worldId.Value, channelId.Value, runTicks, listenPort, verificationKeys);
    }

    private static string ReadValue(
        IReadOnlyList<string> arguments,
        ref int index,
        string option)
    {
        if (option is not "--world" and not "--channel" and not "--run-ticks" and
            not "--listen-port" and not "--verification-key")
            throw new WorldLaunchOptionsException($"does not recognize argument '{option}'.");

        int valueIndex = index + 1;
        if (valueIndex >= arguments.Count || arguments[valueIndex].StartsWith("--", StringComparison.Ordinal))
            throw new WorldLaunchOptionsException($"{option} requires a value.");

        index = valueIndex;
        return arguments[valueIndex];
    }

    private static WorldId ParseWorldId(string value)
    {
        try
        {
            return new(value);
        }
        catch (ArgumentException exception)
        {
            throw new WorldLaunchOptionsException(
                $"--world value '{value}' is invalid: {exception.Message}");
        }
    }

    private static ChannelId ParseChannelId(string value)
    {
        try
        {
            return new(value);
        }
        catch (ArgumentException exception)
        {
            throw new WorldLaunchOptionsException(
                $"--channel value '{value}' is invalid: {exception.Message}");
        }
    }
}

internal sealed class WorldLaunchOptionsException(string message) : Exception(message);
