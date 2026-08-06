using System.Collections.Immutable;
using System.Globalization;
using Starfall.Protocol.Development;

namespace Starfall.World.Development;

internal sealed class PingWorldDevelopmentCommandHandler : IWorldDevelopmentCommandHandler
{
    public DevelopmentCommandId CommandId => DevelopmentCommandIds.PingWorld;

    public WorldDevelopmentCommandHandlerResult Handle(
        WorldDevelopmentCommandContext context,
        ImmutableArray<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!arguments.IsEmpty)
        {
            return WorldDevelopmentCommandHandlerResult.Reject(
                DevelopmentCommandRejectionReason.InvalidArguments,
                "ping_world accepts no arguments");
        }

        return WorldDevelopmentCommandHandlerResult.Success(string.Create(
            CultureInfo.InvariantCulture,
            $"pong world={context.Runtime.WorldId} channel={context.Runtime.ChannelId} " +
            $"tick={context.Runtime.CurrentTick} session={context.Session.SessionId} " +
            $"player={context.Session.PlayerEntityId}"));
    }
}
