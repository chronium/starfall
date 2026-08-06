using Starfall.Protocol.Development;
using Starfall.Protocol.Networking;

namespace Starfall.Protocol.Tests.Development;

public sealed class DevelopmentCommandCodecTests
{
    private static readonly byte[] RequestGolden = Convert.FromHexString(
        "0102030405060708" +
        "0A70696E675F776F726C64" +
        "02" +
        "05616C706861" +
        "023432");

    private static readonly byte[] SucceededGolden = Convert.FromHexString(
        "01" +
        "0102030405060708" +
        "0A70696E675F776F726C64" +
        "0002" +
        "6F6B");

    private static readonly byte[] RejectedGolden = Convert.FromHexString(
        "02" +
        "0102030405060708" +
        "0A70696E675F776F726C64" +
        "01" +
        "0007" +
        "756E6B6E6F776E");

    [Fact]
    public void Public_channels_kinds_and_bounds_are_frozen()
    {
        Assert.Equal(7, StarfallNetworkChannels.DevelopmentCommands);
        Assert.Equal(8, StarfallNetworkChannels.DevelopmentCommandResults);
        Assert.Equal(1, (byte)DevelopmentCommandResultPayloadKind.Succeeded);
        Assert.Equal(2, (byte)DevelopmentCommandResultPayloadKind.Rejected);
        Assert.Equal(11, DevelopmentCommandCodec.MinimumRequestPayloadLength);
        Assert.Equal(594, DevelopmentCommandCodec.MaximumRequestPayloadLength);
        Assert.Equal(588, DevelopmentCommandCodec.MaximumSucceededPayloadLength);
        Assert.Equal(589, DevelopmentCommandCodec.MaximumRejectedPayloadLength);
        Assert.Equal(512, DevelopmentCommandCodec.MaximumDiagnosticByteLength);
    }

    [Fact]
    public void Request_matches_golden_bytes_is_deterministic_and_round_trips()
    {
        DevelopmentCommandRequest request = CreateRequest();

        byte[] first = DevelopmentCommandCodec.EncodeRequest(request);
        byte[] second = DevelopmentCommandCodec.EncodeRequest(request);

        Assert.Equal(RequestGolden, first);
        Assert.Equal(RequestGolden, second);
        Assert.NotSame(first, second);
        Assert.True(DevelopmentCommandCodec.TryDecodeRequest(first, out DevelopmentCommandRequest? decoded));
        Assert.Equal(request.Sequence, decoded!.Sequence);
        Assert.Equal(request.CommandId, decoded.CommandId);
        Assert.Equal(request.Arguments.ToArray(), decoded.Arguments.ToArray());
    }

    [Fact]
    public void Maximum_request_round_trips_at_the_public_bound()
    {
        var request = new DevelopmentCommandRequest(
            new DevelopmentCommandSequence(ulong.MaxValue),
            new DevelopmentCommandId($"a{new string('x', 63)}"),
            Enumerable.Repeat(new string('~', 64), 8));

        byte[] payload = DevelopmentCommandCodec.EncodeRequest(request);

        Assert.Equal(DevelopmentCommandCodec.MaximumRequestPayloadLength, payload.Length);
        Assert.True(DevelopmentCommandCodec.TryDecodeRequest(payload, out DevelopmentCommandRequest? decoded));
        Assert.Equal(request.Arguments.ToArray(), decoded!.Arguments.ToArray());
    }

    [Fact]
    public void Success_matches_golden_bytes_is_deterministic_and_round_trips()
    {
        var succeeded = new DevelopmentCommandSucceeded(
            new DevelopmentCommandSequence(0x0102030405060708),
            new DevelopmentCommandId("ping_world"),
            "ok");

        byte[] first = DevelopmentCommandCodec.EncodeSucceeded(succeeded);
        byte[] second = DevelopmentCommandCodec.EncodeSucceeded(succeeded);

        Assert.Equal(SucceededGolden, first);
        Assert.Equal(SucceededGolden, second);
        Assert.NotSame(first, second);
        Assert.True(DevelopmentCommandCodec.TryDecodeSucceeded(first, out DevelopmentCommandSucceeded? decoded));
        Assert.Equal(succeeded.Sequence, decoded!.Sequence);
        Assert.Equal(succeeded.CommandId, decoded.CommandId);
        Assert.Equal(succeeded.Diagnostic, decoded.Diagnostic);
    }

    [Fact]
    public void Rejection_matches_golden_bytes_and_every_reason_round_trips()
    {
        var rejected = new DevelopmentCommandRejected(
            new DevelopmentCommandSequence(0x0102030405060708),
            new DevelopmentCommandId("ping_world"),
            DevelopmentCommandRejectionReason.UnknownCommand,
            "unknown");
        Assert.Equal(RejectedGolden, DevelopmentCommandCodec.EncodeRejected(rejected));

        foreach (DevelopmentCommandRejectionReason reason in Enum.GetValues<DevelopmentCommandRejectionReason>())
        {
            byte[] payload = DevelopmentCommandCodec.EncodeRejected(new DevelopmentCommandRejected(
                rejected.Sequence,
                rejected.CommandId,
                reason,
                "rejected"));
            Assert.True(DevelopmentCommandCodec.TryDecodeRejected(payload, out DevelopmentCommandRejected? decoded));
            Assert.Equal(reason, decoded!.Reason);
            Assert.Equal(rejected.Sequence, decoded.Sequence);
            Assert.Equal(rejected.CommandId, decoded.CommandId);
        }
    }

    [Fact]
    public void Maximum_diagnostics_round_trip_at_public_result_bounds()
    {
        DevelopmentCommandSequence sequence = new(1);
        DevelopmentCommandId commandId = new($"a{new string('x', 63)}");
        string diagnostic = new('~', 512);

        byte[] succeeded = DevelopmentCommandCodec.EncodeSucceeded(
            new DevelopmentCommandSucceeded(sequence, commandId, diagnostic));
        byte[] rejected = DevelopmentCommandCodec.EncodeRejected(
            new DevelopmentCommandRejected(
                sequence,
                commandId,
                DevelopmentCommandRejectionReason.HandlerRejected,
                diagnostic));

        Assert.Equal(DevelopmentCommandCodec.MaximumSucceededPayloadLength, succeeded.Length);
        Assert.Equal(DevelopmentCommandCodec.MaximumRejectedPayloadLength, rejected.Length);
        Assert.True(DevelopmentCommandCodec.TryDecodeSucceeded(succeeded, out _));
        Assert.True(DevelopmentCommandCodec.TryDecodeRejected(rejected, out _));
    }

    [Fact]
    public void Every_shorter_and_representative_extended_payload_is_rejected()
    {
        AssertWrongLengths(RequestGolden, payload => DevelopmentCommandCodec.TryDecodeRequest(payload, out _));
        AssertWrongLengths(SucceededGolden, payload => DevelopmentCommandCodec.TryDecodeSucceeded(payload, out _));
        AssertWrongLengths(RejectedGolden, payload => DevelopmentCommandCodec.TryDecodeRejected(payload, out _));
    }

    [Fact]
    public void Malformed_requests_are_rejected_without_throwing()
    {
        AssertRequestRejected(payload => payload.AsSpan(0, 8).Clear());
        AssertRequestRejected(payload => payload[8] = 0);
        AssertRequestRejected(payload => payload[9] = (byte)'P');
        AssertRequestRejected(payload => payload[19] = 9);
        AssertRequestRejected(payload => payload[20] = 0);
        AssertRequestRejected(payload => payload[21] = (byte)' ');

        byte[] impossibleArgument = [.. RequestGolden];
        impossibleArgument[20] = 64;
        Assert.False(DevelopmentCommandCodec.TryDecodeRequest(impossibleArgument, out _));
    }

    [Fact]
    public void Malformed_results_are_rejected_without_throwing()
    {
        Assert.False(DevelopmentCommandCodec.TryReadResultPayloadKind([], out _));
        Assert.False(DevelopmentCommandCodec.TryReadResultPayloadKind([0], out _));
        Assert.False(DevelopmentCommandCodec.TryReadResultPayloadKind([3], out _));

        byte[] zeroSequence = [.. SucceededGolden];
        zeroSequence.AsSpan(1, 8).Clear();
        Assert.False(DevelopmentCommandCodec.TryDecodeSucceeded(zeroSequence, out _));

        byte[] badIdentity = [.. SucceededGolden];
        badIdentity[10] = (byte)'P';
        Assert.False(DevelopmentCommandCodec.TryDecodeSucceeded(badIdentity, out _));

        byte[] invalidReason = [.. RejectedGolden];
        invalidReason[20] = 0;
        Assert.False(DevelopmentCommandCodec.TryDecodeRejected(invalidReason, out _));

        byte[] invalidDiagnostic = [.. SucceededGolden];
        invalidDiagnostic[^1] = (byte)'\n';
        Assert.False(DevelopmentCommandCodec.TryDecodeSucceeded(invalidDiagnostic, out _));
    }

    [Fact]
    public void Arbitrary_malformed_payloads_never_throw()
    {
        var random = new Random(7319);
        for (int iteration = 0; iteration < 512; iteration++)
        {
            byte[] payload = new byte[random.Next(0, 700)];
            random.NextBytes(payload);

            Assert.Null(Record.Exception(() => DevelopmentCommandCodec.TryDecodeRequest(payload, out _)));
            Assert.Null(Record.Exception(() => DevelopmentCommandCodec.TryDecodeSucceeded(payload, out _)));
            Assert.Null(Record.Exception(() => DevelopmentCommandCodec.TryDecodeRejected(payload, out _)));
        }
    }

    [Fact]
    public void Encoders_reject_null_inputs_without_partial_output()
    {
        Assert.Throws<ArgumentException>(() => DevelopmentCommandCodec.EncodeRequest(null!));
        Assert.Throws<ArgumentException>(() => DevelopmentCommandCodec.EncodeSucceeded(null!));
        Assert.Throws<ArgumentException>(() => DevelopmentCommandCodec.EncodeRejected(null!));
    }

    private static DevelopmentCommandRequest CreateRequest() => new(
        new DevelopmentCommandSequence(0x0102030405060708),
        new DevelopmentCommandId("ping_world"),
        ["alpha", "42"]);

    private static void AssertWrongLengths(byte[] valid, Func<byte[], bool> decode)
    {
        for (int length = 0; length < valid.Length; length++)
            Assert.False(decode(valid[..length]));
        Assert.False(decode([.. valid, 0]));
        Assert.False(decode([.. valid, 0, 0, 0, 0]));
    }

    private static void AssertRequestRejected(Action<byte[]> mutate)
    {
        byte[] payload = [.. RequestGolden];
        mutate(payload);
        Assert.Null(Record.Exception(() => DevelopmentCommandCodec.TryDecodeRequest(payload, out _)));
        Assert.False(DevelopmentCommandCodec.TryDecodeRequest(payload, out _));
    }
}
