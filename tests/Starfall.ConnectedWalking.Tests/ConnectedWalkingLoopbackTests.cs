using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Starfall.Client.Networking;
using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Compatibility;
using Starfall.World.Lifecycle;
using Starfall.World.Networking;

namespace Starfall.ConnectedWalking.Tests;

public sealed class ConnectedWalkingLoopbackTests
{
    [Fact]
    public async Task Real_udp_admits_moves_corrects_and_cleans_up_one_session()
    {
        int port = ReserveUdpPort();
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var runtime = new WorldChannelRuntime(
            new WorldId("world_1"),
            new ChannelId("channel_1"),
            new WorldInstanceId(Guid.NewGuid()),
            Draft0GrayboxCatalog.FirstPlayable,
            Draft0StarterMonsterCatalog.FirstPlayable,
            Draft0CampPolicyCatalog.FirstPlayable);
        runtime.Start();
        using var host = new WorldGameplayNetworkHost(
            WorldNetworkTransportFactory.Create(),
            runtime,
            new WorldJoinTicketVerificationKeyRing(
            [
                new WorldJoinTicketVerificationKey("test", key.ExportSubjectPublicKeyInfo()),
            ]));
        host.Start(port);

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string ticket = WorldJoinTicketCodec.Issue(
            new WorldJoinTicketClaims(
                new JoinTicketId(Guid.NewGuid()),
                new AccountId(Guid.NewGuid()),
                new CharacterId(Guid.NewGuid()),
                runtime.WorldId,
                runtime.ChannelId,
                runtime.InstanceId,
                now,
                now + 30_000),
            "test",
            key);
        using var client = new ConnectedWalkingClientSession(ClientNetworkTransportFactory.Create(), ticket);
        using var pumping = new CancellationTokenSource();
        Task serverPump = Task.Run(async () =>
        {
            while (!pumping.IsCancellationRequested)
            {
                host.Pump();
                await Task.Delay(2, pumping.Token).ConfigureAwait(false);
            }
        });

        client.ConnectAndAwaitInitialSnapshot(new(IPAddress.Loopback, port, "unused"));
        Assert.True(client.IsReady);
        Assert.Equal(StarfallGameplayProtocol.CurrentVersion, client.ProtocolVersion);
        Assert.Equal(1, runtime.ActiveSessionCount);
        Assert.Equal(Draft0GrayboxCatalog.FirstPlayable.Town.RespawnAnchor, client.Snapshot!.Value.Position);
        await PumpUntilAsync(client, () => client.MonsterSnapshot is not null);
        Assert.Equal(0UL, client.MonsterSnapshot!.SimulationTick);
        Assert.Equal(10, client.MonsterSnapshot.LiveMonsters.Length);
        Assert.Empty(client.MonsterSnapshot.DefeatedMonsters);

        client.SendMovementIntent(new GroundPoint(100, 50));
        await PumpUntilAsync(client, () =>
        {
            runtime.Step();
            return client.Snapshot!.Value.Position.ZMetres > 25;
        });

        client.SendMovementIntent(new GroundPoint(-1, -1));
        await PumpUntilAsync(client, () => client.Snapshot!.Value.Tick >= 1);
        client.Dispose();
        await WaitUntilAsync(() => runtime.ActiveSessionCount == 0 && runtime.PlayerCount == 0);

        pumping.Cancel();
        try
        {
            await serverPump;
        }
        catch (OperationCanceledException) { }
    }

    private static int ReserveUdpPort()
    {
        using UdpClient socket = new(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.Client.LocalEndPoint!).Port;
    }

    private static async Task PumpUntilAsync(ConnectedWalkingClientSession client, Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            timeout.Token.ThrowIfCancellationRequested();
            client.Poll();
            await Task.Delay(2, timeout.Token);
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            timeout.Token.ThrowIfCancellationRequested();
            await Task.Delay(2, timeout.Token);
        }
    }
}
