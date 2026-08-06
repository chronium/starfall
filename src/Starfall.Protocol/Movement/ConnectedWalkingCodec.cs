using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Starfall.Protocol.Movement;

public static class ConnectedWalkingCodec
{
    public const int CommandPayloadLength = 16;
    public const int SnapshotPayloadLength = 65;
    public const int CorrectionPayloadLength = 73;

    private const int SnapshotBodyLength = SnapshotPayloadLength;
    private const byte AcknowledgementAbsent = 0;
    private const byte AcknowledgementPresent = 1;

    public static byte[] EncodeCommand(GroundMovementCommand command)
    {
        ValidateCommand(command);

        byte[] payload = new byte[CommandPayloadLength];
        WriteUInt64(payload, 0, command.Sequence.Value);
        WriteSingle(payload, 8, command.Destination.XMetres);
        WriteSingle(payload, 12, command.Destination.ZMetres);
        return payload;
    }

    public static bool TryDecodeCommand(
        ReadOnlySpan<byte> payload,
        [NotNullWhen(true)] out GroundMovementCommand? command)
    {
        command = null;
        if (payload.Length != CommandPayloadLength)
            return false;

        ulong sequence = ReadUInt64(payload, 0);
        float destinationX = ReadSingle(payload, 8);
        float destinationZ = ReadSingle(payload, 12);
        if (sequence == 0 || !IsCanonicalFinite(destinationX) || !IsCanonicalFinite(destinationZ))
            return false;

        command = new GroundMovementCommand(
            new MovementIntentSequence(sequence),
            new GroundPosition(destinationX, destinationZ));
        return true;
    }

    public static byte[] EncodeSnapshot(PlayerMovementSnapshot snapshot)
    {
        ValidateSnapshot(snapshot);

        byte[] payload = new byte[SnapshotPayloadLength];
        WriteSnapshotBody(payload, snapshot);
        return payload;
    }

    public static bool TryDecodeSnapshot(
        ReadOnlySpan<byte> payload,
        [NotNullWhen(true)] out PlayerMovementSnapshot? snapshot)
    {
        snapshot = null;
        return payload.Length == SnapshotPayloadLength &&
            TryDecodeSnapshotBody(payload, out snapshot);
    }

    public static byte[] EncodeCorrection(PlayerMovementCorrection correction)
    {
        ValidateCorrection(correction);

        byte[] payload = new byte[CorrectionPayloadLength];
        WriteUInt64(payload, 0, correction.CorrectedIntentSequence.Value);
        WriteSnapshotBody(payload.AsSpan(8), correction.AuthoritativeSnapshot);
        return payload;
    }

    public static bool TryDecodeCorrection(
        ReadOnlySpan<byte> payload,
        [NotNullWhen(true)] out PlayerMovementCorrection? correction)
    {
        correction = null;
        if (payload.Length != CorrectionPayloadLength)
            return false;

        ulong correctedSequence = ReadUInt64(payload, 0);
        if (correctedSequence == 0 || !TryDecodeSnapshotBody(payload[8..], out PlayerMovementSnapshot? snapshot))
            return false;

        if (snapshot.LastProcessedIntentSequence is not { } acknowledged ||
            acknowledged.Value != correctedSequence)
        {
            return false;
        }

        correction = new PlayerMovementCorrection(
            new MovementIntentSequence(correctedSequence),
            snapshot);
        return true;
    }

    private static void ValidateCommand(GroundMovementCommand? command)
    {
        if (command is null ||
            !command.Sequence.IsValid ||
            !command.Destination.IsValid ||
            !IsCanonicalFinite(command.Destination.XMetres) ||
            !IsCanonicalFinite(command.Destination.ZMetres))
        {
            throw new ArgumentException("Movement command must be a complete canonical fact.", nameof(command));
        }
    }

    private static void ValidateSnapshot(PlayerMovementSnapshot? snapshot)
    {
        if (snapshot is null ||
            !snapshot.Sequence.IsValid ||
            !snapshot.EntityId.IsValid ||
            !snapshot.Position.IsValid ||
            !IsCanonicalFinite(snapshot.Position.XMetres) ||
            !IsCanonicalFinite(snapshot.Position.ZMetres) ||
            !IsCanonicalVector(snapshot.VelocityMetresPerSecond) ||
            !IsCanonicalVector(snapshot.Facing) ||
            !PlayerMovementSnapshot.IsValidFacing(snapshot.Facing) ||
            !snapshot.Collision.IsValid ||
            !IsCanonicalFinite(snapshot.Collision.RadiusMetres) ||
            !IsCanonicalFinite(snapshot.Collision.HeightMetres) ||
            snapshot.LastProcessedIntentSequence is { IsValid: false })
        {
            throw new ArgumentException("Movement snapshot must be a complete canonical fact.", nameof(snapshot));
        }
    }

    private static void ValidateCorrection(PlayerMovementCorrection? correction)
    {
        if (correction is null || !correction.CorrectedIntentSequence.IsValid)
            throw new ArgumentException("Movement correction must be a complete canonical fact.", nameof(correction));

        ValidateSnapshot(correction.AuthoritativeSnapshot);
        if (correction.AuthoritativeSnapshot.LastProcessedIntentSequence is not { } acknowledged ||
            acknowledged != correction.CorrectedIntentSequence)
        {
            throw new ArgumentException(
                "Movement correction snapshot must acknowledge the corrected intent sequence.",
                nameof(correction));
        }
    }

    private static void WriteSnapshotBody(Span<byte> body, PlayerMovementSnapshot snapshot)
    {
        if (body.Length != SnapshotBodyLength)
            throw new ArgumentException("Snapshot body has an invalid length.", nameof(body));

        WriteUInt64(body, 0, snapshot.Sequence.Value);
        WriteUInt64(body, 8, snapshot.SimulationTick);
        WriteUInt64(body, 16, snapshot.EntityId.Value);
        WriteSingle(body, 24, snapshot.Position.XMetres);
        WriteSingle(body, 28, snapshot.Position.ZMetres);
        WriteSingle(body, 32, snapshot.VelocityMetresPerSecond.X);
        WriteSingle(body, 36, snapshot.VelocityMetresPerSecond.Y);
        WriteSingle(body, 40, snapshot.Facing.X);
        WriteSingle(body, 44, snapshot.Facing.Y);
        WriteSingle(body, 48, snapshot.Collision.RadiusMetres);
        WriteSingle(body, 52, snapshot.Collision.HeightMetres);

        if (snapshot.LastProcessedIntentSequence is { } acknowledged)
        {
            body[56] = AcknowledgementPresent;
            WriteUInt64(body, 57, acknowledged.Value);
        }
        else
        {
            body[56] = AcknowledgementAbsent;
            WriteUInt64(body, 57, 0);
        }
    }

    private static bool TryDecodeSnapshotBody(
        ReadOnlySpan<byte> body,
        [NotNullWhen(true)] out PlayerMovementSnapshot? snapshot)
    {
        snapshot = null;
        if (body.Length != SnapshotBodyLength)
            return false;

        ulong sequence = ReadUInt64(body, 0);
        ulong simulationTick = ReadUInt64(body, 8);
        ulong entityId = ReadUInt64(body, 16);
        float positionX = ReadSingle(body, 24);
        float positionZ = ReadSingle(body, 28);
        var velocity = new Vector2(ReadSingle(body, 32), ReadSingle(body, 36));
        var facing = new Vector2(ReadSingle(body, 40), ReadSingle(body, 44));
        float radius = ReadSingle(body, 48);
        float height = ReadSingle(body, 52);
        byte acknowledgementFlag = body[56];
        ulong acknowledgementSequence = ReadUInt64(body, 57);

        if (sequence == 0 ||
            entityId == 0 ||
            !IsCanonicalFinite(positionX) ||
            !IsCanonicalFinite(positionZ) ||
            !IsCanonicalVector(velocity) ||
            !IsCanonicalVector(facing) ||
            !PlayerMovementSnapshot.IsValidFacing(facing) ||
            !IsCanonicalFinite(radius) ||
            !IsCanonicalFinite(height) ||
            radius <= 0.0f ||
            height <= radius * 2.0f ||
            acknowledgementFlag > AcknowledgementPresent ||
            (acknowledgementFlag == AcknowledgementAbsent && acknowledgementSequence != 0) ||
            (acknowledgementFlag == AcknowledgementPresent && acknowledgementSequence == 0))
        {
            return false;
        }

        MovementIntentSequence? acknowledged = acknowledgementFlag == AcknowledgementPresent
            ? new MovementIntentSequence(acknowledgementSequence)
            : null;
        snapshot = new PlayerMovementSnapshot(
            new MovementSnapshotSequence(sequence),
            simulationTick,
            new WorldEntityId(entityId),
            new GroundPosition(positionX, positionZ),
            velocity,
            facing,
            new PlayerCollisionCapsule(radius, height),
            acknowledged);
        return true;
    }

    private static bool IsCanonicalVector(Vector2 value) =>
        IsCanonicalFinite(value.X) && IsCanonicalFinite(value.Y);

    private static bool IsCanonicalFinite(float value) =>
        float.IsFinite(value) && BitConverter.SingleToInt32Bits(value) != int.MinValue;

    private static void WriteUInt64(Span<byte> destination, int offset, ulong value) =>
        BinaryPrimitives.WriteUInt64BigEndian(destination[offset..], value);

    private static ulong ReadUInt64(ReadOnlySpan<byte> source, int offset) =>
        BinaryPrimitives.ReadUInt64BigEndian(source[offset..]);

    private static void WriteSingle(Span<byte> destination, int offset, float value) =>
        BinaryPrimitives.WriteInt32BigEndian(destination[offset..], BitConverter.SingleToInt32Bits(value));

    private static float ReadSingle(ReadOnlySpan<byte> source, int offset) =>
        BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(source[offset..]));
}
