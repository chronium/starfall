using Starfall.Protocol.Admission;
using Starfall.Protocol.Compatibility;
using Starfall.World.Lifecycle;

namespace Starfall.World.Admission;

internal sealed class WorldJoinAdmissionOutcome
{
    private WorldJoinAdmissionOutcome(
        WorldJoinAccepted? accepted,
        WorldJoinRejected? rejected)
    {
        Accepted = accepted;
        Rejected = rejected;
    }

    internal bool IsAccepted => Accepted is not null;

    internal WorldJoinAccepted? Accepted
    {
        get;
    }

    internal WorldJoinRejected? Rejected
    {
        get;
    }

    internal static WorldJoinAdmissionOutcome Accept(WorldJoinAccepted accepted)
    {
        ArgumentNullException.ThrowIfNull(accepted);
        return new(accepted, null);
    }

    internal static WorldJoinAdmissionOutcome Reject(WorldJoinRejectionReason reason) =>
        new(null, new WorldJoinRejected(reason));
}

internal sealed class WorldJoinAdmissionExchange
{
    private readonly WorldChannelRuntime runtime;
    private readonly WorldJoinTicketVerificationKeyRing verificationKeys;

    internal WorldJoinAdmissionExchange(
        WorldChannelRuntime runtime,
        WorldJoinTicketVerificationKeyRing verificationKeys)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(verificationKeys);

        this.runtime = runtime;
        this.verificationKeys = verificationKeys;
    }

    internal WorldJoinAdmissionOutcome Handle(
        WorldJoinRequest request,
        long nowUnixMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ProtocolVersion != StarfallGameplayProtocol.CurrentVersion)
            return WorldJoinAdmissionOutcome.Reject(WorldJoinRejectionReason.IncompatibleProtocolVersion);

        WorldJoinTicketValidationResult validation = WorldJoinTicketCodec.Validate(
            request.Ticket,
            verificationKeys,
            runtime.AdmissionAudience,
            nowUnixMilliseconds);

        if (!validation.IsValid)
            return WorldJoinAdmissionOutcome.Reject(validation.ToRejectionReason());

        return runtime.ConsumeTicketAndCreateSession(
            validation.Claims!,
            request.ProtocolVersion,
            nowUnixMilliseconds);
    }
}
