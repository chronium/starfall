using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Starfall.Protocol.Movement;

namespace Starfall.Protocol.Monsters;

public static class BoundedMonsterSnapshotCodec
{
    public const byte SchemaVersion = 1;
    public const int HeaderPayloadLength = 19;
    public const int MaxPayloadLength = 1_209;

    private const int LiveFixedPayloadLength = 55;
    private const int DefeatedFixedPayloadLength = 33;
    private const byte TargetAbsent = 0;
    private const byte TargetPresent = 1;

    public static byte[] Encode(BoundedMonsterSnapshot snapshot)
    {
        Validate(snapshot);

        int length = HeaderPayloadLength;
        foreach (LiveMonsterSnapshot monster in snapshot.LiveMonsters)
            length = checked(length + LiveFixedPayloadLength + monster.ArchetypeId.Value.Length);
        foreach (DefeatedMonsterSnapshot monster in snapshot.DefeatedMonsters)
            length = checked(length + DefeatedFixedPayloadLength + monster.ArchetypeId.Value.Length);

        byte[] payload = new byte[length];
        int offset = 0;
        WriteByte(payload, ref offset, SchemaVersion);
        WriteUInt64(payload, ref offset, snapshot.Sequence.Value);
        WriteUInt64(payload, ref offset, snapshot.SimulationTick);
        WriteByte(payload, ref offset, checked((byte)snapshot.LiveMonsters.Length));
        WriteByte(payload, ref offset, checked((byte)snapshot.DefeatedMonsters.Length));

        foreach (LiveMonsterSnapshot monster in snapshot.LiveMonsters)
            WriteLiveMonster(payload, ref offset, monster);
        foreach (DefeatedMonsterSnapshot monster in snapshot.DefeatedMonsters)
            WriteDefeatedMonster(payload, ref offset, monster);

        if (offset != payload.Length)
            throw new InvalidOperationException("Monster snapshot length calculation diverged from encoding.");
        return payload;
    }

    public static bool TryDecode(
        ReadOnlySpan<byte> payload,
        [NotNullWhen(true)] out BoundedMonsterSnapshot? snapshot)
    {
        snapshot = null;
        if (payload.Length is < HeaderPayloadLength or > MaxPayloadLength)
            return false;

        int offset = 0;
        if (!TryReadByte(payload, ref offset, out byte version) || version != SchemaVersion ||
            !TryReadUInt64(payload, ref offset, out ulong sequence) || sequence == 0 ||
            !TryReadUInt64(payload, ref offset, out ulong simulationTick) ||
            !TryReadByte(payload, ref offset, out byte liveCount) ||
            !TryReadByte(payload, ref offset, out byte defeatedCount) ||
            liveCount + defeatedCount > BoundedMonsterSnapshot.MaxEntries)
        {
            return false;
        }

        var live = ImmutableArray.CreateBuilder<LiveMonsterSnapshot>(liveCount);
        var defeated = ImmutableArray.CreateBuilder<DefeatedMonsterSnapshot>(defeatedCount);
        try
        {
            for (int index = 0; index < liveCount; index++)
            {
                if (!TryReadLiveMonster(payload, ref offset, out LiveMonsterSnapshot? monster))
                    return false;
                live.Add(monster);
            }
            for (int index = 0; index < defeatedCount; index++)
            {
                if (!TryReadDefeatedMonster(payload, ref offset, out DefeatedMonsterSnapshot? monster))
                    return false;
                defeated.Add(monster);
            }

            if (offset != payload.Length)
                return false;

            snapshot = new BoundedMonsterSnapshot(
                new MonsterSnapshotSequence(sequence),
                simulationTick,
                live.MoveToImmutable(),
                defeated.MoveToImmutable());
            return true;
        }
        catch (ArgumentException)
        {
            snapshot = null;
            return false;
        }
    }

    private static void Validate(BoundedMonsterSnapshot? snapshot)
    {
        if (snapshot is null ||
            !snapshot.Sequence.IsValid ||
            snapshot.LiveMonsters.IsDefault ||
            snapshot.DefeatedMonsters.IsDefault ||
            snapshot.LiveMonsters.Length + snapshot.DefeatedMonsters.Length > BoundedMonsterSnapshot.MaxEntries)
        {
            throw new ArgumentException("Monster snapshot must be a complete bounded fact.", nameof(snapshot));
        }

        ulong previous = 0;
        var identities = new HashSet<ulong>();
        foreach (LiveMonsterSnapshot monster in snapshot.LiveMonsters)
        {
            if (!IsValid(monster) || monster.EntityId.Value <= previous || !identities.Add(monster.EntityId.Value))
                throw new ArgumentException("Monster snapshot contains an invalid or unordered live entry.", nameof(snapshot));
            previous = monster.EntityId.Value;
        }

        previous = 0;
        foreach (DefeatedMonsterSnapshot monster in snapshot.DefeatedMonsters)
        {
            if (!IsValid(monster) ||
                monster.DefeatedAtTick > snapshot.SimulationTick ||
                monster.EntityId.Value <= previous ||
                !identities.Add(monster.EntityId.Value))
            {
                throw new ArgumentException("Monster snapshot contains an invalid or unordered defeated entry.", nameof(snapshot));
            }
            previous = monster.EntityId.Value;
        }
    }

    private static bool IsValid(LiveMonsterSnapshot? monster) =>
        monster is not null &&
        monster.EntityId.IsValid &&
        monster.ArchetypeId.IsValid &&
        monster.Position.IsValid &&
        IsCanonicalFinite(monster.Position.XMetres) &&
        IsCanonicalFinite(monster.Position.ZMetres) &&
        IsCanonicalVector(monster.VelocityMetresPerSecond) &&
        IsCanonicalVector(monster.Facing) &&
        LiveMonsterSnapshot.IsValidFacing(monster.Facing) &&
        IsCanonicalFinite(monster.CollisionRadiusMetres) &&
        monster.CollisionRadiusMetres > 0.0f &&
        Enum.IsDefined(monster.Behavior) &&
        IsValidTarget(monster) &&
        monster.CurrentHealthUnits > 0 &&
        monster.MaximumHealthUnits > 0 &&
        monster.CurrentHealthUnits <= monster.MaximumHealthUnits;

    private static bool IsValid(DefeatedMonsterSnapshot? monster) =>
        monster is not null &&
        monster.EntityId.IsValid &&
        monster.ArchetypeId.IsValid &&
        monster.LastPosition.IsValid &&
        IsCanonicalFinite(monster.LastPosition.XMetres) &&
        IsCanonicalFinite(monster.LastPosition.ZMetres) &&
        IsCanonicalVector(monster.LastFacing) &&
        LiveMonsterSnapshot.IsValidFacing(monster.LastFacing);

    private static bool IsValidTarget(LiveMonsterSnapshot monster)
    {
        if (monster.Behavior is MonsterBehaviorKind.Pursuing or MonsterBehaviorKind.Attacking)
        {
            return monster.TargetEntityId is { IsValid: true } target && target != monster.EntityId;
        }
        return monster.TargetEntityId is null;
    }

    private static void WriteLiveMonster(Span<byte> payload, ref int offset, LiveMonsterSnapshot monster)
    {
        WriteUInt64(payload, ref offset, monster.EntityId.Value);
        WriteIdentity(payload, ref offset, monster.ArchetypeId);
        WriteSingle(payload, ref offset, monster.Position.XMetres);
        WriteSingle(payload, ref offset, monster.Position.ZMetres);
        WriteSingle(payload, ref offset, monster.VelocityMetresPerSecond.X);
        WriteSingle(payload, ref offset, monster.VelocityMetresPerSecond.Y);
        WriteSingle(payload, ref offset, monster.Facing.X);
        WriteSingle(payload, ref offset, monster.Facing.Y);
        WriteSingle(payload, ref offset, monster.CollisionRadiusMetres);
        WriteByte(payload, ref offset, (byte)monster.Behavior);
        if (monster.TargetEntityId is { } target)
        {
            WriteByte(payload, ref offset, TargetPresent);
            WriteUInt64(payload, ref offset, target.Value);
        }
        else
        {
            WriteByte(payload, ref offset, TargetAbsent);
            WriteUInt64(payload, ref offset, 0);
        }
        WriteInt32(payload, ref offset, monster.CurrentHealthUnits);
        WriteInt32(payload, ref offset, monster.MaximumHealthUnits);
    }

    private static void WriteDefeatedMonster(Span<byte> payload, ref int offset, DefeatedMonsterSnapshot monster)
    {
        WriteUInt64(payload, ref offset, monster.EntityId.Value);
        WriteIdentity(payload, ref offset, monster.ArchetypeId);
        WriteSingle(payload, ref offset, monster.LastPosition.XMetres);
        WriteSingle(payload, ref offset, monster.LastPosition.ZMetres);
        WriteSingle(payload, ref offset, monster.LastFacing.X);
        WriteSingle(payload, ref offset, monster.LastFacing.Y);
        WriteUInt64(payload, ref offset, monster.DefeatedAtTick);
    }

    private static bool TryReadLiveMonster(
        ReadOnlySpan<byte> payload,
        ref int offset,
        [NotNullWhen(true)] out LiveMonsterSnapshot? monster)
    {
        monster = null;
        if (!TryReadUInt64(payload, ref offset, out ulong entityId) || entityId == 0 ||
            !TryReadIdentity(payload, ref offset, out MonsterArchetypeId archetypeId) ||
            !TryReadCanonicalSingle(payload, ref offset, out float positionX) ||
            !TryReadCanonicalSingle(payload, ref offset, out float positionZ) ||
            !TryReadCanonicalSingle(payload, ref offset, out float velocityX) ||
            !TryReadCanonicalSingle(payload, ref offset, out float velocityZ) ||
            !TryReadCanonicalSingle(payload, ref offset, out float facingX) ||
            !TryReadCanonicalSingle(payload, ref offset, out float facingZ) ||
            !TryReadCanonicalSingle(payload, ref offset, out float radius) || radius <= 0.0f ||
            !TryReadByte(payload, ref offset, out byte behaviorByte) || behaviorByte > (byte)MonsterBehaviorKind.Returning ||
            !TryReadByte(payload, ref offset, out byte targetFlag) || targetFlag > TargetPresent ||
            !TryReadUInt64(payload, ref offset, out ulong targetId) ||
            (targetFlag == TargetAbsent && targetId != 0) ||
            (targetFlag == TargetPresent && targetId == 0) ||
            !TryReadInt32(payload, ref offset, out int currentHealth) ||
            !TryReadInt32(payload, ref offset, out int maximumHealth) ||
            currentHealth <= 0 || maximumHealth <= 0 || currentHealth > maximumHealth)
        {
            return false;
        }

        var entity = new WorldEntityId(entityId);
        var behavior = (MonsterBehaviorKind)behaviorByte;
        WorldEntityId? target = targetFlag == TargetPresent ? new WorldEntityId(targetId) : null;
        var facing = new Vector2(facingX, facingZ);
        if (!LiveMonsterSnapshot.IsValidFacing(facing))
            return false;

        monster = new LiveMonsterSnapshot(
            entity,
            archetypeId,
            new GroundPosition(positionX, positionZ),
            new Vector2(velocityX, velocityZ),
            facing,
            radius,
            behavior,
            target,
            currentHealth,
            maximumHealth);
        return true;
    }

    private static bool TryReadDefeatedMonster(
        ReadOnlySpan<byte> payload,
        ref int offset,
        [NotNullWhen(true)] out DefeatedMonsterSnapshot? monster)
    {
        monster = null;
        if (!TryReadUInt64(payload, ref offset, out ulong entityId) || entityId == 0 ||
            !TryReadIdentity(payload, ref offset, out MonsterArchetypeId archetypeId) ||
            !TryReadCanonicalSingle(payload, ref offset, out float positionX) ||
            !TryReadCanonicalSingle(payload, ref offset, out float positionZ) ||
            !TryReadCanonicalSingle(payload, ref offset, out float facingX) ||
            !TryReadCanonicalSingle(payload, ref offset, out float facingZ) ||
            !TryReadUInt64(payload, ref offset, out ulong defeatedAtTick))
        {
            return false;
        }

        var facing = new Vector2(facingX, facingZ);
        if (!LiveMonsterSnapshot.IsValidFacing(facing))
            return false;

        monster = new DefeatedMonsterSnapshot(
            new WorldEntityId(entityId),
            archetypeId,
            new GroundPosition(positionX, positionZ),
            facing,
            defeatedAtTick);
        return true;
    }

    private static void WriteIdentity(Span<byte> payload, ref int offset, MonsterArchetypeId identity)
    {
        WriteByte(payload, ref offset, checked((byte)identity.Value.Length));
        foreach (char character in identity.Value)
            WriteByte(payload, ref offset, checked((byte)character));
    }

    private static bool TryReadIdentity(
        ReadOnlySpan<byte> payload,
        ref int offset,
        out MonsterArchetypeId identity)
    {
        identity = default;
        if (!TryReadByte(payload, ref offset, out byte length) ||
            length is 0 or > MonsterArchetypeId.MaxByteLength ||
            payload.Length - offset < length)
        {
            return false;
        }

        Span<char> characters = stackalloc char[length];
        for (int index = 0; index < length; index++)
            characters[index] = (char)payload[offset + index];
        offset += length;

        string value = new(characters);
        if (!MonsterArchetypeId.IsValidValue(value))
            return false;
        identity = new MonsterArchetypeId(value);
        return true;
    }

    private static bool TryReadCanonicalSingle(ReadOnlySpan<byte> source, ref int offset, out float value)
    {
        value = 0.0f;
        if (!TryReadInt32(source, ref offset, out int bits))
            return false;
        value = BitConverter.Int32BitsToSingle(bits);
        return IsCanonicalFinite(value);
    }

    private static bool IsCanonicalVector(Vector2 value) =>
        IsCanonicalFinite(value.X) && IsCanonicalFinite(value.Y);

    private static bool IsCanonicalFinite(float value) =>
        float.IsFinite(value) && BitConverter.SingleToInt32Bits(value) != int.MinValue;

    private static void WriteByte(Span<byte> destination, ref int offset, byte value) =>
        destination[offset++] = value;

    private static bool TryReadByte(ReadOnlySpan<byte> source, ref int offset, out byte value)
    {
        value = 0;
        if ((uint)offset >= (uint)source.Length)
            return false;
        value = source[offset++];
        return true;
    }

    private static void WriteUInt64(Span<byte> destination, ref int offset, ulong value)
    {
        BinaryPrimitives.WriteUInt64BigEndian(destination[offset..], value);
        offset += sizeof(ulong);
    }

    private static bool TryReadUInt64(ReadOnlySpan<byte> source, ref int offset, out ulong value)
    {
        value = 0;
        if (source.Length - offset < sizeof(ulong))
            return false;
        value = BinaryPrimitives.ReadUInt64BigEndian(source[offset..]);
        offset += sizeof(ulong);
        return true;
    }

    private static void WriteInt32(Span<byte> destination, ref int offset, int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(destination[offset..], value);
        offset += sizeof(int);
    }

    private static bool TryReadInt32(ReadOnlySpan<byte> source, ref int offset, out int value)
    {
        value = 0;
        if (source.Length - offset < sizeof(int))
            return false;
        value = BinaryPrimitives.ReadInt32BigEndian(source[offset..]);
        offset += sizeof(int);
        return true;
    }

    private static void WriteSingle(Span<byte> destination, ref int offset, float value) =>
        WriteInt32(destination, ref offset, BitConverter.SingleToInt32Bits(value));
}
