using System.Numerics;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Movement;
using Starfall.Simulation.Movement;
using Starfall.World.Admission;
using Starfall.World.Entities;
using Starfall.World.Movement;
using ProtocolCollisionCapsule = Starfall.Protocol.Movement.PlayerCollisionCapsule;
using ProtocolEntityId = Starfall.Protocol.Movement.WorldEntityId;
using SimulationEntityId = Starfall.Simulation.Entities.WorldEntityId;

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
    private readonly Dictionary<GameplaySessionId, WorldWalkingSessionState> walkingSessions = [];
    private readonly Dictionary<SimulationEntityId, WorldPlayerState> players = [];
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
            walkingSessions.Clear();
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

            SimulationEntityId entityId = entityIds.Allocate();
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
        SimulationEntityId entityId,
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

    internal bool TryGetPlayer(SimulationEntityId entityId, out WorldPlayerState? player)
    {
        lock (synchronization)
            return players.TryGetValue(entityId, out player);
    }

    internal bool RemovePlayer(SimulationEntityId entityId)
    {
        lock (synchronization)
        {
            if (state is not WorldChannelLifecycleState.Running and
                not WorldChannelLifecycleState.Draining)
            {
                throw new InvalidOperationException(
                    $"Cannot remove a player from a world in the {state} state.");
            }

            if (activeSessions.Values.Any(session => session.PlayerEntityId == entityId))
            {
                throw new InvalidOperationException(
                    $"Cannot remove session-bound player {entityId} through the technical player seam.");
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

    internal bool TerminateGameplaySession(GameplaySessionId sessionId)
    {
        lock (synchronization)
        {
            if (state is not WorldChannelLifecycleState.Running and
                not WorldChannelLifecycleState.Draining)
            {
                throw new InvalidOperationException(
                    $"Cannot terminate a gameplay session in a world in the {state} state.");
            }

            if (!activeSessions.Remove(sessionId, out WorldGameplaySession? session))
                return false;
            if (!walkingSessions.Remove(sessionId))
                throw new InvalidOperationException("Gameplay and walking session ownership diverged.");
            bool playerRemoved = players.Remove(session.PlayerEntityId);
            bool movementRemoved = movement.RemovePlayer(session.PlayerEntityId);
            if (!playerRemoved || !movementRemoved)
                throw new InvalidOperationException("Session player and movement ownership diverged.");
            return true;
        }
    }

    internal WorldWalkingCommandResult HandleWalkingCommand(
        GameplaySessionId sessionId,
        GroundMovementCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        lock (synchronization)
        {
            if (!walkingSessions.TryGetValue(sessionId, out WorldWalkingSessionState? walkingSession))
                return new(WorldWalkingCommandDisposition.UnknownSession);

            if (walkingSession.LastProcessedIntentSequence is { } lastProcessed &&
                command.Sequence.Value <= lastProcessed.Value)
            {
                return new(WorldWalkingCommandDisposition.StaleOrDuplicate);
            }

            GroundMovementIntentDisposition movementDisposition = movement.Submit(
                new GroundMovementIntent(
                    walkingSession.Session.PlayerEntityId,
                    new GroundPoint(
                        command.Destination.XMetres,
                        command.Destination.ZMetres)));

            if (movementDisposition == GroundMovementIntentDisposition.UnknownPlayer)
            {
                throw new InvalidOperationException(
                    $"Session {sessionId} is bound to missing player {walkingSession.Session.PlayerEntityId}.");
            }

            walkingSession.LastProcessedIntentSequence = command.Sequence;
            if (movementDisposition == GroundMovementIntentDisposition.Accepted)
                return new(WorldWalkingCommandDisposition.Accepted);

            PlayerMovementSnapshot snapshot = CreateMovementSnapshot(walkingSession);
            return new(
                WorldWalkingCommandDisposition.Corrected,
                new PlayerMovementCorrection(command.Sequence, snapshot));
        }
    }

    internal IReadOnlyList<WorldWalkingSnapshot> CaptureWalkingSnapshots()
    {
        lock (synchronization)
        {
            if (state is not WorldChannelLifecycleState.Running and
                not WorldChannelLifecycleState.Draining)
            {
                return [];
            }

            return walkingSessions.Values
                .OrderBy(static session => session.Session.PlayerEntityId.Value)
                .Where(session => session.LastPublishedTick != currentTick)
                .Select(session => new WorldWalkingSnapshot(
                    session.Session.SessionId,
                    CreateMovementSnapshot(session)))
                .ToArray();
        }
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
                claims.WorldInstanceId,
                CreateSessionPlayer().EntityId);
            activeSessions.Add(sessionId, session);
            walkingSessions.Add(sessionId, new WorldWalkingSessionState(session));

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

    private WorldPlayerState CreateSessionPlayer()
    {
        SimulationEntityId entityId = entityIds.Allocate();
        AuthoritativePlayerMovementState movementState = movement.RegisterPlayer(
            entityId,
            Layout.Town.RespawnAnchor,
            Vector2.UnitY);
        WorldPlayerState player = CreateWorldPlayerState(movementState);
        players.Add(entityId, player);
        return player;
    }

    private PlayerMovementSnapshot CreateMovementSnapshot(WorldWalkingSessionState walkingSession)
    {
        if (!players.TryGetValue(walkingSession.Session.PlayerEntityId, out WorldPlayerState? player))
        {
            throw new InvalidOperationException(
                $"Session {walkingSession.Session.SessionId} is bound to missing player {walkingSession.Session.PlayerEntityId}.");
        }

        var snapshot = new PlayerMovementSnapshot(
            walkingSession.SnapshotSequences.Allocate(),
            currentTick,
            new ProtocolEntityId(player.EntityId.Value),
            new GroundPosition(
                CanonicalizeZero(player.Position.XMetres),
                CanonicalizeZero(player.Position.ZMetres)),
            CanonicalizeZero(player.VelocityMetresPerSecond),
            CanonicalizeZero(player.Facing),
            new ProtocolCollisionCapsule(
                CanonicalizeZero(player.Collision.RadiusMetres),
                CanonicalizeZero(player.Collision.HeightMetres)),
            walkingSession.LastProcessedIntentSequence);
        walkingSession.LastPublishedTick = currentTick;
        return snapshot;
    }

    private static Vector2 CanonicalizeZero(Vector2 value) =>
        new(CanonicalizeZero(value.X), CanonicalizeZero(value.Y));

    private static float CanonicalizeZero(float value) =>
        value == 0.0f ? 0.0f : value;

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
