using Starfall.Protocol.Admission;
using Starfall.Protocol.Movement;
using Starfall.World.Lifecycle;

namespace Starfall.World.Movement;

internal enum WorldWalkingCommandDisposition
{
    Accepted,
    Corrected,
    MalformedPayload,
    UnknownSession,
    StaleOrDuplicate,
}

internal sealed class WorldWalkingCommandOutcome
{
    internal WorldWalkingCommandOutcome(
        WorldWalkingCommandDisposition disposition,
        byte[]? correctionPayload = null)
    {
        if (disposition == WorldWalkingCommandDisposition.Corrected && correctionPayload is null)
            throw new ArgumentException("A corrected command must carry a correction payload.", nameof(correctionPayload));
        if (disposition != WorldWalkingCommandDisposition.Corrected && correctionPayload is not null)
            throw new ArgumentException("Only a corrected command may carry a correction payload.", nameof(correctionPayload));

        Disposition = disposition;
        CorrectionPayload = correctionPayload;
    }

    internal WorldWalkingCommandDisposition Disposition
    {
        get;
    }

    internal byte[]? CorrectionPayload
    {
        get;
    }
}

internal sealed record WorldWalkingSnapshotPublication(
    GameplaySessionId SessionId,
    byte[] Payload);

internal sealed class WorldWalkingExchange
{
    private readonly WorldChannelRuntime runtime;

    internal WorldWalkingExchange(WorldChannelRuntime runtime)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    internal WorldWalkingCommandOutcome HandleCommand(
        GameplaySessionId sessionId,
        ReadOnlySpan<byte> payload)
    {
        if (!ConnectedWalkingCodec.TryDecodeCommand(payload, out GroundMovementCommand? command))
            return new(WorldWalkingCommandDisposition.MalformedPayload);

        WorldWalkingCommandResult result = runtime.HandleWalkingCommand(sessionId, command);
        return result.Correction is null
            ? new(result.Disposition)
            : new(
                result.Disposition,
                ConnectedWalkingCodec.EncodeCorrection(result.Correction));
    }

    internal IReadOnlyList<WorldWalkingSnapshotPublication> CaptureSnapshots() =>
        runtime.CaptureWalkingSnapshots()
            .Select(static publication => new WorldWalkingSnapshotPublication(
                publication.SessionId,
                ConnectedWalkingCodec.EncodeSnapshot(publication.Snapshot)))
            .ToArray();
}
