using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using ChronoFall.Network.Transport;
using Starfall.Client.Networking;
using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Combat;
using Starfall.Protocol.Compatibility;
using Starfall.Protocol.Development;
using Starfall.Protocol.Monsters;
using Starfall.Protocol.Movement;
using Starfall.Protocol.Networking;
using Starfall.World.Admission;
using Starfall.World.Entities;
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

    [Fact]
    public async Task Real_udp_basic_arrow_resolves_three_hits_and_publishes_monster_defeat()
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
        string ticket = IssueTicket(runtime, key);
        using var client = new BasicArrowLoopbackClient(ClientNetworkTransportFactory.Create(), ticket);
        client.Connect(port);

        await PumpUntilAsync(host, client, runtime, () => client.IsReady);
        Assert.True(runtime.TryGetGameplaySession(client.Admission!.SessionId, out WorldGameplaySession? session));
        Assert.NotNull(session);
        await MoveToAsync(host, client, runtime, new GroundPoint(100.0f, 70.0f));
        await MoveToAsync(host, client, runtime, new GroundPoint(70.0f, 65.0f));
        WorldMonsterState target = Assert.Single(
            runtime.Monsters,
            static monster => monster.SpawnId == "spawn_easy_03");

        int[] expectedEffectiveDamage = [300, 300, 100];
        for (var shot = 0; shot < expectedEffectiveDamage.Length; shot++)
        {
            while (runtime.CurrentTick < runtime.GetNextBasicArrowStartTick(session!.PlayerEntityId))
                await PumpOneAsync(host, client, runtime);

            int priorOutcomes = client.Outcomes.Count;
            client.SendBasicArrow((ulong)shot + 1, target.EntityId.Value);
            await PumpUntilAsync(
                host,
                client,
                runtime,
                () => client.Outcomes.Count >= priorOutcomes + 2);

            Assert.IsType<BasicArrowAccepted>(client.Outcomes[priorOutcomes]);
            BasicArrowResolved resolved = Assert.IsType<BasicArrowResolved>(client.Outcomes[priorOutcomes + 1]);
            Assert.Equal(300, resolved.RequestedDamageUnits);
            Assert.Equal(expectedEffectiveDamage[shot], resolved.EffectiveDamageUnits);
            Assert.Equal(shot == 2, resolved.TargetDefeated);
        }

        await PumpUntilAsync(
            host,
            client,
            runtime,
            () => client.MonsterSnapshot?.DefeatedMonsters.Any(monster => monster.EntityId.Value == target.EntityId.Value) == true);
        Assert.False(runtime.TryGetMonster(target.EntityId, out _));
        Assert.Contains(
            client.MonsterSnapshot!.DefeatedMonsters,
            monster => monster.EntityId.Value == target.EntityId.Value);
    }

    [Fact]
    public async Task Real_udp_dispatches_session_bound_ping_world_and_returns_its_correlation()
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
        using var client = new BasicArrowLoopbackClient(
            ClientNetworkTransportFactory.Create(),
            IssueTicket(runtime, key));
        client.Connect(port);

        await PumpUntilAsync(host, client, runtime, () => client.IsReady);
        client.SendDevelopmentCommand(42, DevelopmentCommandIds.PingWorld);
        await PumpUntilAsync(host, client, runtime, () => client.DevelopmentResults.Count == 1);

        DevelopmentCommandSucceeded succeeded = Assert.IsType<DevelopmentCommandSucceeded>(
            client.DevelopmentResults[0]);
        Assert.Equal(42UL, succeeded.Sequence.Value);
        Assert.Equal(DevelopmentCommandIds.PingWorld, succeeded.CommandId);
        Assert.Contains($"session={client.Admission!.SessionId}", succeeded.Diagnostic, StringComparison.Ordinal);
        Assert.Contains("world=world_1 channel=channel_1", succeeded.Diagnostic, StringComparison.Ordinal);
    }

    private static int ReserveUdpPort()
    {
        using UdpClient socket = new(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.Client.LocalEndPoint!).Port;
    }

    private static string IssueTicket(WorldChannelRuntime runtime, ECDsa key)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return WorldJoinTicketCodec.Issue(
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
    }

    private static async Task MoveToAsync(
        WorldGameplayNetworkHost host,
        BasicArrowLoopbackClient client,
        WorldChannelRuntime runtime,
        GroundPoint destination)
    {
        client.SendMovement(destination);
        await PumpUntilAsync(
            host,
            client,
            runtime,
            () => client.PlayerSnapshot is { } snapshot &&
                snapshot.Position.XMetres == destination.XMetres &&
                snapshot.Position.ZMetres == destination.ZMetres);
    }

    private static async Task PumpUntilAsync(
        WorldGameplayNetworkHost host,
        BasicArrowLoopbackClient client,
        WorldChannelRuntime runtime,
        Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            timeout.Token.ThrowIfCancellationRequested();
            await PumpOneAsync(host, client, runtime);
        }
    }

    private static async Task PumpOneAsync(
        WorldGameplayNetworkHost host,
        BasicArrowLoopbackClient client,
        WorldChannelRuntime runtime)
    {
        host.Pump();
        client.Poll();
        runtime.Step();
        host.PublishBasicArrowTickOutcomes();
        host.Pump();
        client.Poll();
        await Task.Delay(1);
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

    private sealed class BasicArrowLoopbackClient : INetworkEventHandler, IDisposable
    {
        private readonly INetworkTransport transport;
        private readonly WorldJoinRequest admissionRequest;
        private NetworkPeerId peerId;
        private bool connected;
        private ulong nextMovementSequence = 1;

        internal BasicArrowLoopbackClient(INetworkTransport transport, string ticket)
        {
            this.transport = transport;
            admissionRequest = new WorldJoinRequest(StarfallGameplayProtocol.CurrentVersion, ticket);
        }

        internal bool IsReady => Admission is not null && PlayerSnapshot is not null && MonsterSnapshot is not null;
        internal WorldJoinAccepted? Admission
        {
            get; private set;
        }
        internal PlayerMovementSnapshot? PlayerSnapshot
        {
            get; private set;
        }
        internal BoundedMonsterSnapshot? MonsterSnapshot
        {
            get; private set;
        }
        internal List<object> Outcomes { get; } = [];
        internal List<object> DevelopmentResults { get; } = [];

        internal void Connect(int port)
        {
            transport.Start(0);
            peerId = transport.Connect(new NetworkEndpoint(IPAddress.Loopback.ToString(), port));
        }

        internal void Poll() => transport.Poll(this);

        internal void SendMovement(GroundPoint destination)
        {
            var command = new GroundMovementCommand(
                new MovementIntentSequence(nextMovementSequence++),
                new GroundPosition(destination.XMetres, destination.ZMetres));
            transport.Send(
                peerId,
                ConnectedWalkingCodec.EncodeCommand(command),
                NetworkDelivery.ReliableSequenced,
                StarfallNetworkChannels.MovementCommands);
        }

        internal void SendBasicArrow(ulong sequence, ulong targetEntityId)
        {
            var command = new BasicArrowCommand(
                new CombatCommandSequence(sequence),
                new Starfall.Protocol.Movement.WorldEntityId(targetEntityId));
            transport.Send(
                peerId,
                ConnectedBasicArrowCodec.EncodeCommand(command),
                NetworkDelivery.ReliableSequenced,
                StarfallNetworkChannels.BasicArrowCommands);
        }

        internal void SendDevelopmentCommand(
            ulong sequence,
            DevelopmentCommandId commandId,
            params string[] arguments)
        {
            transport.Send(
                peerId,
                DevelopmentCommandCodec.EncodeRequest(new DevelopmentCommandRequest(
                    new DevelopmentCommandSequence(sequence),
                    commandId,
                    arguments)),
                NetworkDelivery.ReliableOrdered,
                StarfallNetworkChannels.DevelopmentCommands);
        }

        public void Connected(NetworkPeerId connectedPeer, NetworkEndpoint endpoint)
        {
            Assert.Equal(peerId, connectedPeer);
            connected = true;
            transport.Send(
                peerId,
                WorldJoinAdmissionCodec.EncodeRequest(admissionRequest),
                NetworkDelivery.ReliableOrdered,
                StarfallNetworkChannels.Admission);
        }

        public void Disconnected(NetworkPeerId disconnectedPeer, NetworkDisconnectReason reason) =>
            connected = false;

        public void PacketReceived(NetworkPeerId sender, ReadOnlyMemory<byte> packet, NetworkDelivery delivery, byte channel)
        {
            Assert.Equal(peerId, sender);
            if (channel == StarfallNetworkChannels.Admission)
            {
                Assert.Equal(NetworkDelivery.ReliableOrdered, delivery);
                Assert.True(WorldJoinAdmissionCodec.TryDecodeAccepted(packet.Span, out WorldJoinAccepted? accepted));
                Admission = accepted;
                return;
            }
            if (channel == StarfallNetworkChannels.MovementSnapshots)
            {
                Assert.Equal(NetworkDelivery.Sequenced, delivery);
                Assert.True(ConnectedWalkingCodec.TryDecodeSnapshot(packet.Span, out PlayerMovementSnapshot? snapshot));
                PlayerSnapshot = snapshot;
                return;
            }
            if (channel == StarfallNetworkChannels.MonsterSnapshots)
            {
                Assert.Equal(NetworkDelivery.Sequenced, delivery);
                Assert.True(BoundedMonsterSnapshotCodec.TryDecode(packet.Span, out BoundedMonsterSnapshot? snapshot));
                MonsterSnapshot = snapshot;
                return;
            }
            if (channel == StarfallNetworkChannels.BasicArrowOutcomes)
            {
                Assert.Equal(NetworkDelivery.ReliableOrdered, delivery);
                Assert.True(ConnectedBasicArrowCodec.TryReadPayloadKind(packet.Span, out BasicArrowPayloadKind kind));
                Outcomes.Add(kind switch
                {
                    BasicArrowPayloadKind.Accepted when ConnectedBasicArrowCodec.TryDecodeAccepted(packet.Span, out BasicArrowAccepted? accepted) => accepted,
                    BasicArrowPayloadKind.Rejected when ConnectedBasicArrowCodec.TryDecodeRejected(packet.Span, out BasicArrowRejected? rejected) => rejected,
                    BasicArrowPayloadKind.Canceled when ConnectedBasicArrowCodec.TryDecodeCanceled(packet.Span, out BasicArrowCanceled? canceled) => canceled,
                    BasicArrowPayloadKind.Resolved when ConnectedBasicArrowCodec.TryDecodeResolved(packet.Span, out BasicArrowResolved? resolved) => resolved,
                    _ => throw new InvalidOperationException("Malformed Basic Arrow outcome in loopback test."),
                });
                return;
            }
            if (channel == StarfallNetworkChannels.DevelopmentCommandResults)
            {
                Assert.Equal(NetworkDelivery.ReliableOrdered, delivery);
                Assert.True(DevelopmentCommandCodec.TryReadResultPayloadKind(
                    packet.Span,
                    out DevelopmentCommandResultPayloadKind kind));
                DevelopmentResults.Add(kind switch
                {
                    DevelopmentCommandResultPayloadKind.Succeeded when
                        DevelopmentCommandCodec.TryDecodeSucceeded(packet.Span, out DevelopmentCommandSucceeded? succeeded) => succeeded,
                    DevelopmentCommandResultPayloadKind.Rejected when
                        DevelopmentCommandCodec.TryDecodeRejected(packet.Span, out DevelopmentCommandRejected? rejected) => rejected,
                    _ => throw new InvalidOperationException("Malformed development command result in loopback test."),
                });
            }
        }

        public void NetworkError(NetworkEndpoint? endpoint, SocketError socketError) =>
            throw new InvalidOperationException($"Loopback network error: {socketError}.");

        public void LatencyUpdated(NetworkPeerId updatedPeer, int latencyMilliseconds)
        {
        }

        public void Dispose()
        {
            if (connected)
                transport.Disconnect(peerId);
            transport.Dispose();
        }
    }
}
