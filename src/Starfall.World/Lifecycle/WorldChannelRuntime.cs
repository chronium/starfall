using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.World.Admission;

namespace Starfall.World.Lifecycle;

internal enum WorldChannelLifecycleState
{
    Created,
    Running,
    Draining,
    Stopped,
}

internal sealed class WorldChannelRuntime
{
    private readonly object synchronization = new();
    private readonly Dictionary<JoinTicketId, long> consumedTickets = [];
    private readonly Dictionary<GameplaySessionId, WorldGameplaySession> activeSessions = [];
    private WorldChannelLifecycleState state;
    private ulong currentTick;

    internal WorldChannelRuntime(
        WorldId worldId,
        ChannelId channelId,
        WorldInstanceId instanceId,
        Draft0GrayboxLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        WorldId = worldId;
        ChannelId = channelId;
        InstanceId = instanceId;
        Layout = layout;
    }

    internal WorldId WorldId { get; }

    internal ChannelId ChannelId { get; }

    internal WorldInstanceId InstanceId { get; }

    internal Draft0GrayboxLayout Layout { get; }

    internal WorldChannelLifecycleState State
    {
        get
        {
            lock (synchronization)
                return state;
        }
    }

    internal ulong CurrentTick
    {
        get
        {
            lock (synchronization)
                return currentTick;
        }
    }

    internal bool AcceptingAdmissions
    {
        get
        {
            lock (synchronization)
                return state == WorldChannelLifecycleState.Running;
        }
    }

    internal WorldJoinTicketAudience AdmissionAudience => new(WorldId, ChannelId, InstanceId);

    internal int ActiveSessionCount
    {
        get
        {
            lock (synchronization)
                return activeSessions.Count;
        }
    }

    internal int ConsumedTicketCount
    {
        get
        {
            lock (synchronization)
                return consumedTickets.Count;
        }
    }

    internal void Start()
    {
        lock (synchronization)
        {
            RequireState(WorldChannelLifecycleState.Created, "start");
            state = WorldChannelLifecycleState.Running;
        }
    }

    internal void Step()
    {
        lock (synchronization)
        {
            if (state is not WorldChannelLifecycleState.Running and
                not WorldChannelLifecycleState.Draining)
            {
                throw new InvalidOperationException(
                    $"Cannot advance a world in the {state} state.");
            }

            currentTick = checked(currentTick + 1);
        }
    }

    internal void BeginDrain()
    {
        lock (synchronization)
        {
            if (state == WorldChannelLifecycleState.Draining)
                return;

            RequireState(WorldChannelLifecycleState.Running, "drain");
            state = WorldChannelLifecycleState.Draining;
        }
    }

    internal void Stop()
    {
        lock (synchronization)
        {
            if (state == WorldChannelLifecycleState.Stopped)
                return;
            if (state is not WorldChannelLifecycleState.Running and
                not WorldChannelLifecycleState.Draining)
            {
                throw new InvalidOperationException(
                    $"Cannot stop a world in the {state} state.");
            }

            state = WorldChannelLifecycleState.Stopped;
            activeSessions.Clear();
            consumedTickets.Clear();
        }
    }

    internal bool TryGetGameplaySession(
        GameplaySessionId sessionId,
        out WorldGameplaySession? session)
    {
        lock (synchronization)
            return activeSessions.TryGetValue(sessionId, out session);
    }

    internal WorldJoinAdmissionOutcome ConsumeTicketAndCreateSession(
        WorldJoinTicketClaims claims,
        long nowUnixMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(claims);

        lock (synchronization)
        {
            PruneConsumedTickets(nowUnixMilliseconds);

            if (state != WorldChannelLifecycleState.Running)
                return WorldJoinAdmissionOutcome.Reject(WorldJoinRejectionReason.WorldNotAcceptingAdmissions);

            if (claims.WorldId != WorldId ||
                claims.ChannelId != ChannelId ||
                claims.WorldInstanceId != InstanceId)
            {
                return WorldJoinAdmissionOutcome.Reject(WorldJoinRejectionReason.WrongDestination);
            }

            long retainUntilUnixMilliseconds = checked(
                claims.ExpiresAtUnixMilliseconds + WorldJoinTicketCodec.AllowedClockSkewMilliseconds);
            if (!consumedTickets.TryAdd(claims.TicketId, retainUntilUnixMilliseconds))
                return WorldJoinAdmissionOutcome.Reject(WorldJoinRejectionReason.AlreadyConsumed);

            GameplaySessionId sessionId;
            do
                sessionId = new(Guid.NewGuid());
            while (activeSessions.ContainsKey(sessionId));

            var session = new WorldGameplaySession(
                sessionId,
                claims.AccountId,
                claims.CharacterId,
                claims.WorldInstanceId);
            activeSessions.Add(sessionId, session);

            return WorldJoinAdmissionOutcome.Accept(new WorldJoinAccepted(sessionId));
        }
    }

    private void PruneConsumedTickets(long nowUnixMilliseconds)
    {
        JoinTicketId[] expired = consumedTickets
            .Where(pair => pair.Value <= nowUnixMilliseconds)
            .Select(static pair => pair.Key)
            .ToArray();
        foreach (JoinTicketId ticketId in expired)
            consumedTickets.Remove(ticketId);
    }

    private void RequireState(WorldChannelLifecycleState required, string operation)
    {
        if (state != required)
        {
            throw new InvalidOperationException(
                $"Cannot {operation} a world in the {state} state; expected {required}.");
        }
    }
}
