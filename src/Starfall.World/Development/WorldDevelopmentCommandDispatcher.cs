using System.Collections.Immutable;
using Starfall.Protocol.Admission;
using Starfall.Protocol.Development;
using Starfall.World.Admission;
using Starfall.World.Lifecycle;

namespace Starfall.World.Development;

internal enum WorldDevelopmentCommandDisposition
{
    Succeeded,
    Rejected,
    MalformedPayload,
    UnknownSession,
}

internal sealed record WorldDevelopmentCommandOutcome(
    WorldDevelopmentCommandDisposition Disposition,
    byte[]? Payload = null);

internal sealed record WorldDevelopmentCommandContext(
    WorldChannelRuntime Runtime,
    WorldGameplaySession Session);

internal sealed class WorldDevelopmentCommandHandlerResult
{
    private WorldDevelopmentCommandHandlerResult(
        bool succeeded,
        DevelopmentCommandRejectionReason? rejectionReason,
        string diagnostic)
    {
        if (string.IsNullOrEmpty(diagnostic))
            throw new ArgumentException("A development command handler result requires a diagnostic.", nameof(diagnostic));
        if (succeeded != (rejectionReason is null))
            throw new ArgumentException("A development command handler result must be either successful or rejected.");
        if (rejectionReason is not null and
            not DevelopmentCommandRejectionReason.InvalidArguments and
            not DevelopmentCommandRejectionReason.HandlerRejected)
        {
            throw new ArgumentOutOfRangeException(nameof(rejectionReason));
        }

        Succeeded = succeeded;
        RejectionReason = rejectionReason;
        Diagnostic = diagnostic;
    }

    internal bool Succeeded { get; }
    internal DevelopmentCommandRejectionReason? RejectionReason { get; }
    internal string Diagnostic { get; }

    internal static WorldDevelopmentCommandHandlerResult Success(string diagnostic) =>
        new(true, null, diagnostic);

    internal static WorldDevelopmentCommandHandlerResult Reject(
        DevelopmentCommandRejectionReason reason,
        string diagnostic) => new(false, reason, diagnostic);
}

internal interface IWorldDevelopmentCommandHandler
{
    DevelopmentCommandId CommandId { get; }

    WorldDevelopmentCommandHandlerResult Handle(
        WorldDevelopmentCommandContext context,
        ImmutableArray<string> arguments);
}

internal sealed class WorldDevelopmentCommandDispatcher
{
    private const string HandlerFailureDiagnostic = "handler failed";
    private readonly WorldChannelRuntime runtime;
    private readonly Dictionary<string, IWorldDevelopmentCommandHandler> handlers;
    private readonly Dictionary<GameplaySessionId, DevelopmentCommandSequence> lastSequences = [];

    internal WorldDevelopmentCommandDispatcher(
        WorldChannelRuntime runtime,
        IEnumerable<IWorldDevelopmentCommandHandler> handlers)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        ArgumentNullException.ThrowIfNull(handlers);

        this.handlers = new Dictionary<string, IWorldDevelopmentCommandHandler>(StringComparer.Ordinal);
        foreach (IWorldDevelopmentCommandHandler? handler in handlers)
        {
            if (handler is null || string.IsNullOrEmpty(handler.CommandId.Value))
                throw new ArgumentException("Development command handlers must expose a valid command identity.", nameof(handlers));
            if (!this.handlers.TryAdd(handler.CommandId.Value, handler))
                throw new ArgumentException($"Development command handler '{handler.CommandId}' is registered more than once.", nameof(handlers));
        }
    }

    internal WorldDevelopmentCommandOutcome Handle(
        GameplaySessionId sessionId,
        ReadOnlySpan<byte> payload)
    {
        if (!DevelopmentCommandCodec.TryDecodeRequest(payload, out DevelopmentCommandRequest? request))
            return new(WorldDevelopmentCommandDisposition.MalformedPayload);
        if (!runtime.TryGetGameplaySession(sessionId, out WorldGameplaySession? session) || session is null)
            return new(WorldDevelopmentCommandDisposition.UnknownSession);

        if (lastSequences.TryGetValue(sessionId, out DevelopmentCommandSequence lastSequence) &&
            request.Sequence.Value <= lastSequence.Value)
        {
            return Rejected(
                request,
                DevelopmentCommandRejectionReason.StaleOrDuplicateSequence,
                "stale or duplicate sequence");
        }

        lastSequences[sessionId] = request.Sequence;
        if (!handlers.TryGetValue(request.CommandId.Value, out IWorldDevelopmentCommandHandler? handler))
        {
            return Rejected(
                request,
                DevelopmentCommandRejectionReason.UnknownCommand,
                "unknown command");
        }

        try
        {
            WorldDevelopmentCommandHandlerResult result = handler.Handle(
                new WorldDevelopmentCommandContext(runtime, session),
                request.Arguments);
            return result.Succeeded
                ? Succeeded(request, result.Diagnostic)
                : Rejected(request, result.RejectionReason!.Value, result.Diagnostic);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"STARFALL_WORLD_DEVELOPMENT_COMMAND_FAILURE session={sessionId} command={request.CommandId} error={exception.Message}");
            return Rejected(
                request,
                DevelopmentCommandRejectionReason.HandlerRejected,
                HandlerFailureDiagnostic);
        }
    }

    internal void RemoveSession(GameplaySessionId sessionId) => lastSequences.Remove(sessionId);

    internal void Clear() => lastSequences.Clear();

    private static WorldDevelopmentCommandOutcome Succeeded(
        DevelopmentCommandRequest request,
        string diagnostic) => new(
            WorldDevelopmentCommandDisposition.Succeeded,
            DevelopmentCommandCodec.EncodeSucceeded(new DevelopmentCommandSucceeded(
                request.Sequence,
                request.CommandId,
                diagnostic)));

    private static WorldDevelopmentCommandOutcome Rejected(
        DevelopmentCommandRequest request,
        DevelopmentCommandRejectionReason reason,
        string diagnostic) => new(
            WorldDevelopmentCommandDisposition.Rejected,
            DevelopmentCommandCodec.EncodeRejected(new DevelopmentCommandRejected(
                request.Sequence,
                request.CommandId,
                reason,
                diagnostic)));
}
