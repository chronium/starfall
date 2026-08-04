using System.Numerics;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Simulation.Entities;
using Starfall.Simulation.Movement;
using Starfall.World.Admission;
using Starfall.World.Entities;

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
    private readonly Dictionary<WorldEntityId, WorldPlayerState> players = [];
    private readonly WorldEntityIdSequence entityIds = new();
    private readonly Draft0PlayerMovementSimulation movement;
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
        movement = new Draft0PlayerMovementSimulation(layout);
    }

    internal WorldId WorldId
    {
        get;
    }

    internal ChannelId ChannelId
    {
        get;
    }

    internal WorldInstanceId InstanceId
    {
        get;
    }

    internal Draft0GrayboxLayout Layout
    {
        get;
    }

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

    internal int PlayerCount
    {
        get
        {
            lock (synchronization)
                return players.Count;
        }
    }

    internal IReadOnlyList<WorldPlayerState> Players
    {
        get
        {
            lock (synchronization)
            {
                WorldPlayerState[] snapshot = players.Values
                    .OrderBy(static player => player.EntityId.Value)
                    .ToArray();
                return Array.AsReadOnly(snapshot);
            }
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

            IReadOnlyList<AuthoritativePlayerMovementState> movementStates = movement.Step();
            foreach (AuthoritativePlayerMovementState movementState in movementStates)
            {
                if (!players.ContainsKey(movementState.EntityId))
                {
                    throw new InvalidOperationException(
                        $"Movement produced unknown player {movementState.EntityId}.");
                }

                players[movementState.EntityId] = CreateWorldPlayerState(movementState);
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
            players.Clear();
            movement.Dispose();
        }
    }

    internal WorldPlayerState CreateTechnicalPlayer()
    {
        lock (synchronization)
        {
            if (state != WorldChannelLifecycleState.Running)
            {
                throw new InvalidOperationException(
                    $"Cannot create a technical player in a world in the {state} state.");
            }

            WorldEntityId entityId = entityIds.Allocate();
            AuthoritativePlayerMovementState movementState = movement.RegisterPlayer(
                entityId,
                Layout.Town.RespawnAnchor,
                Vector2.UnitY);
            WorldPlayerState player = CreateWorldPlayerState(movementState);
            players.Add(entityId, player);
            return player;
        }
    }

    internal GroundMovementIntentDisposition SubmitMovementIntent(
        WorldEntityId entityId,
        GroundPoint destination)
    {
        lock (synchronization)
        {
            if (state is not WorldChannelLifecycleState.Running and
                not WorldChannelLifecycleState.Draining)
            {
                throw new InvalidOperationException(
                    $"Cannot submit movement in a world in the {state} state.");
            }

            return movement.Submit(new GroundMovementIntent(entityId, destination));
        }
    }

    internal bool TryGetPlayer(WorldEntityId entityId, out WorldPlayerState? player)
    {
        lock (synchronization)
            return players.TryGetValue(entityId, out player);
    }

    internal bool RemovePlayer(WorldEntityId entityId)
    {
        lock (synchronization)
        {
            if (state is not WorldChannelLifecycleState.Running and
                not WorldChannelLifecycleState.Draining)
            {
                throw new InvalidOperationException(
                    $"Cannot remove a player from a world in the {state} state.");
            }

            bool removedPlayer = players.Remove(entityId);
            bool removedMovement = movement.RemovePlayer(entityId);
            if (removedPlayer != removedMovement)
                throw new InvalidOperationException("World player and movement ownership diverged.");
            return removedPlayer;
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

    private static WorldPlayerState CreateWorldPlayerState(
        AuthoritativePlayerMovementState movementState) =>
        new(
            movementState.EntityId,
            movementState.Position,
            movementState.VelocityMetresPerSecond,
            movementState.Facing,
            movementState.Collision,
            movementState.Outcome);
}
