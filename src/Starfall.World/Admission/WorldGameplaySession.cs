using Starfall.Protocol.Admission;
using Starfall.Simulation.Entities;

namespace Starfall.World.Admission;

internal sealed class WorldGameplaySession
{
    internal WorldGameplaySession(
        GameplaySessionId sessionId,
        AccountId accountId,
        CharacterId characterId,
        WorldInstanceId worldInstanceId,
        WorldEntityId playerEntityId)
    {
        if (sessionId.Value == Guid.Empty)
            throw new ArgumentException("Gameplay session identity must not be empty.", nameof(sessionId));
        if (accountId.Value == Guid.Empty)
            throw new ArgumentException("Account identity must not be empty.", nameof(accountId));
        if (characterId.Value == Guid.Empty)
            throw new ArgumentException("Character identity must not be empty.", nameof(characterId));
        if (worldInstanceId.Value == Guid.Empty)
            throw new ArgumentException("World instance identity must not be empty.", nameof(worldInstanceId));
        if (playerEntityId.Value == 0)
            throw new ArgumentException("Player entity identity must be valid.", nameof(playerEntityId));

        SessionId = sessionId;
        AccountId = accountId;
        CharacterId = characterId;
        WorldInstanceId = worldInstanceId;
        PlayerEntityId = playerEntityId;
    }

    internal GameplaySessionId SessionId
    {
        get;
    }

    internal AccountId AccountId
    {
        get;
    }

    internal CharacterId CharacterId
    {
        get;
    }

    internal WorldInstanceId WorldInstanceId
    {
        get;
    }

    internal WorldEntityId PlayerEntityId
    {
        get;
    }
}
