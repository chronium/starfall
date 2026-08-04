using ChronoFall.Network.Transport;
using Starfall.World.Networking;

namespace Starfall.World.Tests;

public sealed class NetworkTransportCompositionTests
{
    [Fact]
    public void World_factory_creates_usable_source_built_transport()
    {
        using INetworkTransport transport = WorldNetworkTransportFactory.Create();

        transport.Start(0);
        transport.Dispose();

        Assert.Throws<ObjectDisposedException>(() => transport.Start(0));
    }
}
