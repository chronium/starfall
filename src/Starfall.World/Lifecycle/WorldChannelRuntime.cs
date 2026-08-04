using Starfall.Content.Zones;
using Starfall.Protocol.Admission;

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

    internal WorldChannelLifecycleState State { get; private set; }

    internal ulong CurrentTick { get; private set; }

    internal bool AcceptingAdmissions => State == WorldChannelLifecycleState.Running;

    internal void Start()
    {
        RequireState(WorldChannelLifecycleState.Created, "start");
        State = WorldChannelLifecycleState.Running;
    }

    internal void Step()
    {
        if (State is not WorldChannelLifecycleState.Running and
            not WorldChannelLifecycleState.Draining)
        {
            throw new InvalidOperationException(
                $"Cannot advance a world in the {State} state.");
        }

        CurrentTick = checked(CurrentTick + 1);
    }

    internal void BeginDrain()
    {
        if (State == WorldChannelLifecycleState.Draining)
            return;

        RequireState(WorldChannelLifecycleState.Running, "drain");
        State = WorldChannelLifecycleState.Draining;
    }

    internal void Stop()
    {
        if (State == WorldChannelLifecycleState.Stopped)
            return;
        if (State is not WorldChannelLifecycleState.Running and
            not WorldChannelLifecycleState.Draining)
        {
            throw new InvalidOperationException(
                $"Cannot stop a world in the {State} state.");
        }

        State = WorldChannelLifecycleState.Stopped;
    }

    private void RequireState(WorldChannelLifecycleState required, string operation)
    {
        if (State != required)
        {
            throw new InvalidOperationException(
                $"Cannot {operation} a world in the {State} state; expected {required}.");
        }
    }
}
