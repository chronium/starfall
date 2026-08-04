using System.Net;

namespace Starfall.Client.Networking;

internal sealed record ConnectedClientLaunchOptions(IPAddress Address, int Port, string JoinTicketPath)
{
    internal static ConnectedClientLaunchOptions Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        string? address = null;
        int? port = null;
        string? ticket = null;
        for (int index = 0; index < args.Count; index++)
        {
            string option = args[index];
            if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"{option} requires a value.");
            string value = args[++index];
            switch (option)
            {
                case "--connect-address" when address is null:
                    address = value;
                    break;
                case "--connect-port" when port is null:
                    if (!int.TryParse(value, out int parsed) || parsed is < 1 or > 65535)
                        throw new ArgumentException("--connect-port must be between 1 and 65535.");
                    port = parsed;
                    break;
                case "--join-ticket-file" when ticket is null:
                    ticket = Path.GetFullPath(value);
                    break;
                default:
                    throw new ArgumentException($"Unknown or duplicate connected-client option '{option}'.");
            }
        }

        if (address is null || port is null || ticket is null)
            throw new ArgumentException("Connected mode requires --connect-address, --connect-port and --join-ticket-file.");
        if (!IPAddress.TryParse(address, out IPAddress? parsedAddress) || !IPAddress.IsLoopback(parsedAddress))
            throw new ArgumentException("--connect-address must be a literal loopback address (127.0.0.1 or ::1).");
        return new(parsedAddress, port.Value, ticket);
    }
}
