using System.Numerics;
using Starfall.Content.Zones;
using Starfall.Simulation.Entities;
using Starfall.Simulation.Movement;

namespace Starfall.Simulation.Tests;

public sealed class Draft0PlayerMovementSimulationTests
{
    private const float Tolerance = 1e-4f;

    [Fact]
    public void Technical_contract_uses_the_approved_units_and_capsule()
    {
        using var simulation = CreateSimulation();
        AuthoritativePlayerMovementState state = simulation.RegisterPlayer(
            new WorldEntityId(1),
            Draft0GrayboxCatalog.FirstPlayable.Town.RespawnAnchor,
            Vector2.UnitY);

        Assert.Equal(60, Draft0PlayerMovementSimulation.TickRateHz);
        Assert.Equal(4.0f, Draft0PlayerMovementSimulation.SpeedMetresPerSecond);
        Assert.Equal(0.35f, state.Collision.RadiusMetres);
        Assert.Equal(1.8f, state.Collision.HeightMetres);
        Assert.Equal(GroundMovementTickOutcome.Idle, state.Outcome);
    }

    [Fact]
    public void Sixty_ticks_advance_exactly_four_metres()
    {
        using var simulation = CreateSimulation();
        WorldEntityId entityId = Register(simulation, new GroundPoint(100.0f, 25.0f));
        Assert.Equal(
            GroundMovementIntentDisposition.Accepted,
            simulation.Submit(new GroundMovementIntent(entityId, new GroundPoint(100.0f, 35.0f))));

        AuthoritativePlayerMovementState state = default;
        for (var tick = 0; tick < 60; tick++)
            state = Assert.Single(simulation.Step());

        AssertPoint(state.Position, 100.0f, 29.0f);
        AssertVector(state.VelocityMetresPerSecond, 0.0f, 4.0f);
        Assert.Equal(Vector2.UnitY, state.Facing);
        Assert.Equal(GroundMovementTickOutcome.Moving, state.Outcome);
    }

    [Fact]
    public void Latest_accepted_destination_replaces_the_previous_one()
    {
        using var simulation = CreateSimulation();
        WorldEntityId entityId = Register(simulation, new GroundPoint(100.0f, 25.0f));
        simulation.Submit(new GroundMovementIntent(entityId, new GroundPoint(110.0f, 25.0f)));
        simulation.Step();
        simulation.Submit(new GroundMovementIntent(entityId, new GroundPoint(100.0f, 35.0f)));

        AuthoritativePlayerMovementState state = Assert.Single(simulation.Step());

        Assert.True(state.Facing.Y > 0.0f);
        Assert.True(state.VelocityMetresPerSecond.Y > 0.0f);
        Assert.Equal(GroundMovementTickOutcome.Moving, state.Outcome);
    }

    [Fact]
    public void Arrival_clamps_exactly_and_preserves_last_facing()
    {
        using var simulation = CreateSimulation();
        WorldEntityId entityId = Register(simulation, new GroundPoint(100.0f, 25.0f));
        GroundPoint destination = new(100.05f, 25.0f);
        simulation.Submit(new GroundMovementIntent(entityId, destination));

        AuthoritativePlayerMovementState arrived = Assert.Single(simulation.Step());
        AuthoritativePlayerMovementState idle = Assert.Single(simulation.Step());

        Assert.Equal(destination, arrived.Position);
        Assert.Equal(Vector2.Zero, arrived.VelocityMetresPerSecond);
        Assert.Equal(Vector2.UnitX, arrived.Facing);
        Assert.Equal(GroundMovementTickOutcome.Arrived, arrived.Outcome);
        Assert.Equal(arrived.Position, idle.Position);
        Assert.Equal(arrived.Facing, idle.Facing);
        Assert.Equal(GroundMovementTickOutcome.Idle, idle.Outcome);
    }

    [Fact]
    public void Rejected_intent_does_not_replace_an_accepted_destination()
    {
        using var simulation = CreateSimulation();
        WorldEntityId entityId = Register(simulation, new GroundPoint(100.0f, 25.0f));
        simulation.Submit(new GroundMovementIntent(entityId, new GroundPoint(100.0f, 35.0f)));

        Assert.Equal(
            GroundMovementIntentDisposition.OutsideWalkableBounds,
            simulation.Submit(new GroundMovementIntent(entityId, new GroundPoint(5.0f, 100.0f))));
        Assert.Equal(
            GroundMovementIntentDisposition.ObstructedDestination,
            simulation.Submit(new GroundMovementIntent(entityId, new GroundPoint(85.0f, 20.0f))));
        Assert.Equal(
            GroundMovementIntentDisposition.UnknownPlayer,
            simulation.Submit(new GroundMovementIntent(new WorldEntityId(2), new GroundPoint(100.0f, 35.0f))));

        AuthoritativePlayerMovementState state = Assert.Single(simulation.Step());
        Assert.True(state.VelocityMetresPerSecond.Y > 0.0f);
        Assert.Equal(GroundMovementTickOutcome.Moving, state.Outcome);
    }

    [Theory]
    [MemberData(nameof(ProxyCrossings))]
    public void Every_proxy_blocks_and_clears_direct_movement(
        GroundPoint start,
        GroundPoint destination)
    {
        using var simulation = CreateSimulation();
        WorldEntityId entityId = Register(simulation, start);
        Assert.Equal(
            GroundMovementIntentDisposition.Accepted,
            simulation.Submit(new GroundMovementIntent(entityId, destination)));

        AuthoritativePlayerMovementState state = default;
        for (var tick = 0; tick < 300; tick++)
        {
            state = Assert.Single(simulation.Step());
            if (state.Outcome == GroundMovementTickOutcome.Blocked)
                break;
        }

        Assert.Equal(GroundMovementTickOutcome.Blocked, state.Outcome);
        Assert.Equal(Vector2.Zero, state.VelocityMetresPerSecond);
        Assert.Equal(GroundMovementTickOutcome.Idle, Assert.Single(simulation.Step()).Outcome);
    }

    [Fact]
    public void Capsule_adjusted_outer_bounds_reject_unsafe_centres()
    {
        using var simulation = CreateSimulation();
        WorldEntityId entityId = Register(simulation, new GroundPoint(100.0f, 100.0f));

        Assert.Equal(
            GroundMovementIntentDisposition.Accepted,
            simulation.Submit(new GroundMovementIntent(entityId, new GroundPoint(5.35f, 100.0f))));
        Assert.Equal(
            GroundMovementIntentDisposition.OutsideWalkableBounds,
            simulation.Submit(new GroundMovementIntent(entityId, new GroundPoint(5.349f, 100.0f))));
        Assert.Equal(
            GroundMovementIntentDisposition.OutsideWalkableBounds,
            simulation.Submit(new GroundMovementIntent(entityId, new GroundPoint(194.651f, 100.0f))));
    }

    [Fact]
    public void Players_may_cross_the_protected_town_boundary()
    {
        using var simulation = CreateSimulation();
        WorldEntityId entityId = Register(simulation, new GroundPoint(100.0f, 54.0f));
        simulation.Submit(new GroundMovementIntent(entityId, new GroundPoint(100.0f, 56.0f)));

        AuthoritativePlayerMovementState state = default;
        for (var tick = 0; tick < 31; tick++)
            state = Assert.Single(simulation.Step());

        Assert.Equal(new GroundPoint(100.0f, 56.0f), state.Position);
        Assert.False(Draft0GrayboxCatalog.FirstPlayable.Town.Bounds.Contains(state.Position));
    }

    [Fact]
    public void Stable_entity_order_and_repeated_runs_produce_equal_states()
    {
        AuthoritativePlayerMovementState[] first = RunOrderedFixture(registerReverse: false);
        AuthoritativePlayerMovementState[] second = RunOrderedFixture(registerReverse: true);

        Assert.Equal(first, second);
        Assert.Equal([1UL, 2UL], first.Select(static state => state.EntityId.Value));
    }

    [Fact]
    public void Removal_and_disposal_are_explicit()
    {
        var simulation = CreateSimulation();
        WorldEntityId entityId = Register(simulation, new GroundPoint(100.0f, 25.0f));

        Assert.True(simulation.RemovePlayer(entityId));
        Assert.False(simulation.RemovePlayer(entityId));
        Assert.Equal(0, simulation.PlayerCount);

        simulation.Dispose();
        simulation.Dispose();
        Assert.Throws<ObjectDisposedException>(() => simulation.Step());
        Assert.Throws<ObjectDisposedException>(() => simulation.RegisterPlayer(
            entityId,
            new GroundPoint(100.0f, 25.0f),
            Vector2.UnitY));
    }

    public static TheoryData<GroundPoint, GroundPoint> ProxyCrossings => new()
    {
        { new GroundPoint(75.0f, 20.0f), new GroundPoint(100.0f, 20.0f) },
        { new GroundPoint(101.0f, 20.0f), new GroundPoint(125.0f, 20.0f) },
        { new GroundPoint(75.0f, 40.0f), new GroundPoint(100.0f, 40.0f) },
        { new GroundPoint(95.0f, 133.0f), new GroundPoint(105.0f, 133.0f) },
        { new GroundPoint(125.0f, 110.0f), new GroundPoint(140.0f, 110.0f) },
        { new GroundPoint(150.0f, 110.0f), new GroundPoint(165.0f, 110.0f) },
        { new GroundPoint(145.0f, 117.0f), new GroundPoint(145.0f, 130.0f) },
    };

    private static Draft0PlayerMovementSimulation CreateSimulation() =>
        new(Draft0GrayboxCatalog.FirstPlayable);

    private static WorldEntityId Register(
        Draft0PlayerMovementSimulation simulation,
        GroundPoint position,
        ulong value = 1)
    {
        WorldEntityId entityId = new(value);
        simulation.RegisterPlayer(entityId, position, Vector2.UnitY);
        return entityId;
    }

    private static AuthoritativePlayerMovementState[] RunOrderedFixture(bool registerReverse)
    {
        using var simulation = CreateSimulation();
        WorldEntityId first = new(1);
        WorldEntityId second = new(2);
        if (registerReverse)
        {
            simulation.RegisterPlayer(second, new GroundPoint(120.0f, 60.0f), Vector2.UnitY);
            simulation.RegisterPlayer(first, new GroundPoint(80.0f, 60.0f), Vector2.UnitY);
        }
        else
        {
            simulation.RegisterPlayer(first, new GroundPoint(80.0f, 60.0f), Vector2.UnitY);
            simulation.RegisterPlayer(second, new GroundPoint(120.0f, 60.0f), Vector2.UnitY);
        }

        simulation.Submit(new GroundMovementIntent(first, new GroundPoint(82.0f, 60.0f)));
        simulation.Submit(new GroundMovementIntent(second, new GroundPoint(122.0f, 60.0f)));
        return simulation.Step().ToArray();
    }

    private static void AssertPoint(GroundPoint actual, float x, float z)
    {
        Assert.InRange(actual.XMetres, x - Tolerance, x + Tolerance);
        Assert.InRange(actual.ZMetres, z - Tolerance, z + Tolerance);
    }

    private static void AssertVector(Vector2 actual, float x, float y)
    {
        Assert.InRange(actual.X, x - Tolerance, x + Tolerance);
        Assert.InRange(actual.Y, y - Tolerance, y + Tolerance);
    }
}
