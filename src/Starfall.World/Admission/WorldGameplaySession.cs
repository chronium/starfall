using Starfall.Protocol.Admission;

namespace Starfall.World.Admission;

internal sealed class WorldGameplaySession
{
    internal WorldGameplaySession(
        GameplaySessionId sessionId,
        AccountId accountId,
        CharacterId characterId,
        WorldInstanceId worldInstanceId)
    {
        if (sessionId.Value == Guid.Empty)
            throw new ArgumentException("Gameplay session identity must not be empty.", nameof(sessionId));
        if (accountId.Value == Guid.Empty)
            throw new ArgumentException("Account identity must not be empty.", nameof(accountId));
        if (characterId.Value == Guid.Empty)
            throw new ArgumentException("Character identity must not be empty.", nameof(characterId));
        if (worldInstanceId.Value == Guid.Empty)
            throw new ArgumentException("World instance identity must not be empty.", nameof(worldInstanceId));

        SessionId = sessionId;
        AccountId = accountId;
        CharacterId = characterId;
        WorldInstanceId = worldInstanceId;
    }

    internal GameplaySessionId SessionId { get; }

    internal AccountId AccountId { get; }

    internal CharacterId CharacterId { get; }

    internal WorldInstanceId WorldInstanceId { get; }
}
