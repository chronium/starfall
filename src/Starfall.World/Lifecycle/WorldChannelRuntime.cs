using System.Numerics;
using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Movement;
using Starfall.Simulation.Combat;
using Starfall.Simulation.Monsters;
using Starfall.Simulation.Movement;
using Starfall.World.Admission;
using Starfall.World.Combat;
using Starfall.World.Entities;
using Starfall.World.Monsters;
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
    private readonly Draft0GroundCollisionWorld collisionWorld;
    private readonly Draft0PlayerMovementSimulation movement;
    private readonly WorldMonsterPopulation monsterPopulation;
    private readonly WorldBasicArrowCombat basicArrowCombat = new();
    private IReadOnlyList<BasicArrowResolution> lastBasicArrowResolutions = [];
    private IReadOnlyList<Draft0MonsterAttackResolution> lastMonsterAttackResolutions = [];
    private WorldChannelLifecycleState state;
    private ulong currentTick;

    internal WorldChannelRuntime(
        WorldId worldId,
        ChannelId channelId,
        WorldInstanceId instanceId,
        Draft0GrayboxLayout layout,
        Draft0StarterMonsterCatalogDefinition monsterCatalog,
        Draft0CampPolicyCatalogDefinition campPolicies)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(monsterCatalog);
        ArgumentNullException.ThrowIfNull(campPolicies);

        WorldId = worldId;
        ChannelId = channelId;
        InstanceId = instanceId;
        Layout = layout;
        collisionWorld = new Draft0GroundCollisionWorld(layout);
        try
        {
            movement = new Draft0PlayerMovementSimulation(
                layout,
                collisionWorld,
                advancesCollisionWorld: false,
                ownsCollisionWorld: false);
            monsterPopulation = new WorldMonsterPopulation(
                layout,
                monsterCatalog,
                campPolicies,
                collisionWorld);
        }
        catch
        {
            collisionWorld.Dispose();
            throw;
        }
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

    internal int MonsterCount
    {
        get
        {
            lock (synchronization)
                return monsterPopulation.Count;
        }
    }

    internal int PendingBasicArrowCount
    {
        get
        {
            lock (synchronization)
                return basicArrowCombat.PendingCount;
        }
    }

    internal IReadOnlyList<BasicArrowResolution> LastBasicArrowResolutions
    {
        get
        {
            lock (synchronization)
                return lastBasicArrowResolutions;
        }
    }

    internal IReadOnlyList<Draft0MonsterAttackResolution> LastMonsterAttackResolutions
    {
        get
        {
            lock (synchronization)
                return lastMonsterAttackResolutions;
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

    internal IReadOnlyList<WorldMonsterState> Monsters
    {
        get
        {
            lock (synchronization)
                return monsterPopulation.Snapshot();
        }
    }

    internal void Start()
    {
        lock (synchronization)
        {
            RequireState(WorldChannelLifecycleState.Created, "start");
            monsterPopulation.Initialize(currentTick, entityIds.Allocate);
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

            collisionWorld.Step();
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
            lastBasicArrowResolutions = ResolveBasicArrows();
            lastMonsterAttackResolutions = monsterPopulation.StepBehavior(
                players.Values
                    .OrderBy(static player => player.EntityId.Value)
                    .Select(static player => new Draft0MonsterPlayerTarget(
                        player.EntityId,
                        player.Position)),
                currentTick);
            monsterPopulation.ApplyEligible(currentTick, entityIds.Allocate);
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
            basicArrowCombat.Clear();
            lastBasicArrowResolutions = [];
            lastMonsterAttackResolutions = [];
            monsterPopulation.Dispose();
            movement.Dispose();
            collisionWorld.Dispose();
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

            GroundMovementIntentDisposition disposition = movement.Submit(
                new GroundMovementIntent(entityId, destination));
            if (disposition == GroundMovementIntentDisposition.Accepted)
                RecordAcceptedMovement(entityId);
            return disposition;
        }
    }

    internal BasicArrowStartEvaluation SubmitBasicArrow(BasicArrowIntent intent)
    {
        lock (synchronization)
        {
            if (state is not WorldChannelLifecycleState.Running and
                not WorldChannelLifecycleState.Draining)
            {
                throw new InvalidOperationException(
                    $"Cannot submit Basic Arrow in a world in the {state} state.");
            }

            if (!players.TryGetValue(intent.ActorId, out WorldPlayerState? player))
                return BasicArrowStartEvaluation.Reject(BasicArrowStartDisposition.UnknownActor);
            if (!monsterPopulation.TryGet(intent.TargetId, out WorldMonsterState? monster) || monster is null)
                return BasicArrowStartEvaluation.Reject(BasicArrowStartDisposition.UnknownTarget);

            BasicArrowStartEvaluation evaluation = basicArrowCombat.TryStart(
                intent,
                CreateBasicArrowActorState(player),
                CreateBasicArrowTargetState(monster),
                currentTick);
            if (evaluation.PendingAction is not { } pending)
                return evaluation;

            AuthoritativePlayerMovementState stopped = movement.StopAndFace(
                player.EntityId,
                pending.AcceptedFacing);
            players[player.EntityId] = CreateWorldPlayerState(stopped);
            return evaluation;
        }
    }

    internal bool TryGetPendingBasicArrow(
        SimulationEntityId actorId,
        out PendingBasicArrow pending)
    {
        lock (synchronization)
            return basicArrowCombat.TryGetPending(actorId, out pending);
    }

    internal ulong GetNextBasicArrowStartTick(SimulationEntityId actorId)
    {
        lock (synchronization)
            return basicArrowCombat.GetNextAllowedStartTick(actorId);
    }

    internal bool TryGetPlayer(SimulationEntityId entityId, out WorldPlayerState? player)
    {
        lock (synchronization)
            return players.TryGetValue(entityId, out player);
    }

    internal bool TryGetMonster(SimulationEntityId entityId, out WorldMonsterState? monster)
    {
        lock (synchronization)
            return monsterPopulation.TryGet(entityId, out monster);
    }

    internal bool RemoveMonster(SimulationEntityId entityId)
    {
        lock (synchronization)
        {
            if (state is not WorldChannelLifecycleState.Running and
                not WorldChannelLifecycleState.Draining)
            {
                throw new InvalidOperationException(
                    $"Cannot remove a monster from a world in the {state} state.");
            }

            return monsterPopulation.Remove(entityId, currentTick);
        }
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
            if (removedPlayer)
                basicArrowCombat.RemoveActor(entityId);
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
            basicArrowCombat.RemoveActor(session.PlayerEntityId);
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
            {
                RecordAcceptedMovement(walkingSession.Session.PlayerEntityId);
                return new(WorldWalkingCommandDisposition.Accepted);
            }

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

    private void RecordAcceptedMovement(SimulationEntityId entityId)
    {
        if (basicArrowCombat.CancelForMovement(entityId, currentTick) is { } cancellation)
            lastBasicArrowResolutions = Array.AsReadOnly([cancellation]);
    }

    private IReadOnlyList<BasicArrowResolution> ResolveBasicArrows()
    {
        IReadOnlyList<PendingBasicArrow> due = basicArrowCombat.TakeDue(currentTick);
        if (due.Count == 0)
            return [];

        var resolutions = new List<BasicArrowResolution>(due.Count);
        foreach (PendingBasicArrow action in due)
        {
            if (!players.TryGetValue(action.ActorId, out WorldPlayerState? player))
            {
                resolutions.Add(BasicArrowResolution.Cancel(
                    action,
                    currentTick,
                    BasicArrowResolutionDisposition.ActorUnavailable));
                continue;
            }

            if (!monsterPopulation.TryGet(action.TargetId, out WorldMonsterState? monster) || monster is null)
            {
                resolutions.Add(BasicArrowResolution.Cancel(
                    action,
                    currentTick,
                    BasicArrowResolutionDisposition.TargetUnavailable));
                continue;
            }

            BasicArrowResolution resolution = Draft0BasicArrowRules.Resolve(
                Draft0BasicArrowTuning.FirstPlayable,
                action,
                CreateBasicArrowActorState(player),
                CreateBasicArrowTargetState(monster),
                currentTick);
            if (resolution.Damage is { } expectedDamage)
            {
                AuthoritativeDamageResult appliedDamage = monsterPopulation.ApplyDamage(
                    monster.EntityId,
                    expectedDamage.RequestedDamageUnits,
                    currentTick) ?? throw new InvalidOperationException(
                        $"Basic Arrow target {monster.EntityId} disappeared during resolution.");
                if (appliedDamage != expectedDamage)
                    throw new InvalidOperationException("Basic Arrow rule and world health application diverged.");
            }

            resolutions.Add(resolution);
        }

        return Array.AsReadOnly(resolutions.ToArray());
    }

    private static BasicArrowActorState CreateBasicArrowActorState(WorldPlayerState player) =>
        new(
            player.EntityId,
            player.Position,
            player.VelocityMetresPerSecond,
            player.Facing);

    private static BasicArrowTargetState CreateBasicArrowTargetState(WorldMonsterState monster) =>
        new(monster.EntityId, monster.Position, monster.HealthUnits);
}
