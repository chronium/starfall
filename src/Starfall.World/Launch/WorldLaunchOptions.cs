using Starfall.Protocol.Admission;

namespace Starfall.World.Launch;

internal sealed record WorldLaunchOptions(
    WorldId WorldId,
    ChannelId ChannelId,
    int? RunTicks)
{
    internal static WorldLaunchOptions Parse(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        WorldId? worldId = null;
        ChannelId? channelId = null;
        int? runTicks = null;

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

        return new(worldId.Value, channelId.Value, runTicks);
    }

    private static string ReadValue(
        IReadOnlyList<string> arguments,
        ref int index,
        string option)
    {
        if (option is not "--world" and not "--channel" and not "--run-ticks")
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
