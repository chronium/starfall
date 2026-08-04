using Starfall.Protocol.Admission;

namespace Starfall.Protocol.Tests.Admission;

public sealed class WorldJoinAdmissionCodecTests
{
    [Fact]
    public void Request_has_stable_big_endian_length_and_round_trips()
    {
        var request = new WorldJoinRequest("sfjt1.key.payload.signature");
        byte[] payload = WorldJoinAdmissionCodec.EncodeRequest(request);

        Assert.Equal(1, payload[0]);
        Assert.Equal(1, payload[1]);
        Assert.Equal(0, payload[2]);
        Assert.Equal(request.Ticket.Length, payload[3]);
        Assert.True(WorldJoinAdmissionCodec.TryDecodeRequest(payload, out WorldJoinRequest? decoded));
        Assert.Equal(request.Ticket, decoded.Ticket);
        Assert.Equal(payload, WorldJoinAdmissionCodec.EncodeRequest(request));
    }

    [Fact]
    public void Request_encoder_rejects_non_ascii_source_text_without_substitution()
    {
        var request = new WorldJoinRequest("sfjt1.key.payload.signaturé");

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            WorldJoinAdmissionCodec.EncodeRequest(request));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public void Accepted_and_rejected_have_exact_stable_payloads()
    {
        var accepted = new WorldJoinAccepted(
            new GameplaySessionId(Guid.Parse("00112233-4455-6677-8899-aabbccddeeff")));
        byte[] acceptedPayload = WorldJoinAdmissionCodec.EncodeAccepted(accepted);
        Assert.Equal(
            "010200112233445566778899aabbccddeeff",
            Convert.ToHexString(acceptedPayload).ToLowerInvariant());
        Assert.True(WorldJoinAdmissionCodec.TryDecodeAccepted(acceptedPayload, out WorldJoinAccepted? decoded));
        Assert.Equal(accepted.SessionId, decoded.SessionId);

        byte[] rejected = WorldJoinAdmissionCodec.EncodeRejected(
            new WorldJoinRejected(WorldJoinRejectionReason.WrongDestination));
        Assert.Equal([1, 3, 3], rejected);
        Assert.True(WorldJoinAdmissionCodec.TryDecodeRejected(rejected, out WorldJoinRejected? decodedRejection));
        Assert.Equal(WorldJoinRejectionReason.WrongDestination, decodedRejection.Reason);
    }

    [Fact]
    public void Decoders_reject_every_shorter_length_and_trailing_data()
    {
        byte[] request = WorldJoinAdmissionCodec.EncodeRequest(new WorldJoinRequest("ticket"));
        AssertAllWrongLengths(request, static value => WorldJoinAdmissionCodec.TryDecodeRequest(value, out _));
        byte[] accepted = WorldJoinAdmissionCodec.EncodeAccepted(
            new WorldJoinAccepted(new GameplaySessionId(Guid.NewGuid())));
        AssertAllWrongLengths(accepted, static value => WorldJoinAdmissionCodec.TryDecodeAccepted(value, out _));
        byte[] rejected = WorldJoinAdmissionCodec.EncodeRejected(
            new WorldJoinRejected(WorldJoinRejectionReason.InvalidTicket));
        AssertAllWrongLengths(rejected, static value => WorldJoinAdmissionCodec.TryDecodeRejected(value, out _));
    }

    [Fact]
    public void Decoders_reject_unknown_version_kind_invalid_guid_reason_and_non_ascii()
    {
        byte[] request = WorldJoinAdmissionCodec.EncodeRequest(new WorldJoinRequest("ticket"));
        request[0] = 2;
        Assert.False(WorldJoinAdmissionCodec.TryDecodeRequest(request, out _));
        request[0] = 1;
        request[1] = 9;
        Assert.False(WorldJoinAdmissionCodec.TryDecodeRequest(request, out _));
        request[1] = 1;
        request[^1] = 0xff;
        Assert.False(WorldJoinAdmissionCodec.TryDecodeRequest(request, out _));

        byte[] accepted = new byte[WorldJoinAdmissionCodec.AcceptedPayloadLength];
        accepted[0] = 1;
        accepted[1] = 2;
        Assert.False(WorldJoinAdmissionCodec.TryDecodeAccepted(accepted, out _));
        Assert.False(WorldJoinAdmissionCodec.TryDecodeRejected([1, 3, 255], out _));
    }

    private static void AssertAllWrongLengths(byte[] valid, Func<byte[], bool> decode)
    {
        for (int length = 0; length < valid.Length; length++)
            Assert.False(decode(valid[..length]));
        Assert.False(decode([.. valid, 0]));
        Assert.False(decode([.. valid, 0, 0, 0, 0]));
    }
}
