using ChronoFall.Network.Transport;
using ChronoFall.Network.Transport.LiteNetLib;

namespace Starfall.Client.Networking;

internal static class ClientNetworkTransportFactory
{
    internal static INetworkTransport Create() => new LiteNetLibNetworkTransport();
}
