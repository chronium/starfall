using ChronoFall.Network.Transport;
using ChronoFall.Network.Transport.LiteNetLib;

namespace Starfall.World.Networking;

internal static class WorldNetworkTransportFactory
{
    internal static INetworkTransport Create() => new LiteNetLibNetworkTransport();
}
