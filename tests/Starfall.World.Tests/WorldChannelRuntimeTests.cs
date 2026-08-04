using Starfall.Protocol.Admission;
using Starfall.World.Lifecycle;

namespace Starfall.World.Tests;

public sealed class WorldChannelRuntimeTests
{
    [Fact]
    public void Owns_explicit_identity_and_independent_tick_state()
    {
        WorldChannelRuntime first = CreateRuntime(Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"));
        WorldChannelRuntime second = CreateRuntime(Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210"));

        first.Start();
        second.Start();
        first.Step();
        first.Step();
        second.Step();

        Assert.Equal("world_1", first.WorldId.Value);
        Assert.Equal("channel_1", first.ChannelId.Value);
        Assert.NotEqual(first.InstanceId, second.InstanceId);
        Assert.Equal(2UL, first.CurrentTick);
        Assert.Equal(1UL, second.CurrentTick);
    }

    [Fact]
    public void Moves_through_created_running_draining_and_stopped_states()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());

        Assert.Equal(WorldChannelLifecycleState.Created, runtime.State);
        Assert.False(runtime.AcceptingAdmissions);

        runtime.Start();
        Assert.Equal(WorldChannelLifecycleState.Running, runtime.State);
        Assert.True(runtime.AcceptingAdmissions);

        runtime.Step();
        runtime.BeginDrain();
        runtime.BeginDrain();
        Assert.Equal(WorldChannelLifecycleState.Draining, runtime.State);
        Assert.False(runtime.AcceptingAdmissions);

        runtime.Step();
        runtime.Stop();
        runtime.Stop();
        Assert.Equal(WorldChannelLifecycleState.Stopped, runtime.State);
        Assert.Equal(2UL, runtime.CurrentTick);
    }

    [Fact]
    public void Rejects_illegal_lifecycle_transitions()
    {
        WorldChannelRuntime runtime = CreateRuntime(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(runtime.Step);
        Assert.Throws<InvalidOperationException>(runtime.BeginDrain);
        Assert.Throws<InvalidOperationException>(runtime.Stop);

        runtime.Start();
        Assert.Throws<InvalidOperationException>(runtime.Start);
        runtime.Stop();
        Assert.Throws<InvalidOperationException>(runtime.Start);
        Assert.Throws<InvalidOperationException>(runtime.Step);
        Assert.Throws<InvalidOperationException>(runtime.BeginDrain);
    }

    private static WorldChannelRuntime CreateRuntime(Guid instanceId) =>
        new(new("world_1"), new("channel_1"), new(instanceId));
}
