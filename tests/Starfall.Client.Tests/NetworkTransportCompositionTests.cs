using ChronoFall.Network.Transport;
using Starfall.Client.Networking;

namespace Starfall.Client.Tests;

public sealed class NetworkTransportCompositionTests
{
    [Fact]
    public void Client_factory_creates_usable_source_built_transport()
    {
        using INetworkTransport transport = ClientNetworkTransportFactory.Create();

        transport.Start(0);
        transport.Dispose();

        Assert.Throws<ObjectDisposedException>(() => transport.Start(0));
    }
}
