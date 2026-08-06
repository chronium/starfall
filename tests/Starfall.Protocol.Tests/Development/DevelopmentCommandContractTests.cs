using System.Collections.Immutable;
using Starfall.Protocol.Development;

namespace Starfall.Protocol.Tests.Development;

public sealed class DevelopmentCommandContractTests
{
    [Fact]
    public void Sequence_requires_a_nonzero_value()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DevelopmentCommandSequence(0));
        Assert.Equal(42UL, new DevelopmentCommandSequence(42).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ping")]
    [InlineData("1ping")]
    [InlineData("ping-world")]
    [InlineData("ping world")]
    [InlineData("pingé")]
    public void Command_identity_rejects_noncanonical_values(string value)
    {
        Assert.Throws<ArgumentException>(() => new DevelopmentCommandId(value));
    }

    [Fact]
    public void Command_identity_accepts_its_exact_maximum()
    {
        string value = $"a{new string('0', DevelopmentCommandId.MaximumByteLength - 1)}";
        Assert.Equal(value, new DevelopmentCommandId(value).Value);
        Assert.Throws<ArgumentException>(() => new DevelopmentCommandId($"{value}0"));
    }

    [Fact]
    public void Request_defensively_copies_ordered_arguments()
    {
        var source = new List<string> { "alpha", "42" };
        var request = new DevelopmentCommandRequest(
            new DevelopmentCommandSequence(1),
            new DevelopmentCommandId("ping_world"),
            source);

        source[0] = "changed";
        source.Add("extra");

        Assert.Equal(["alpha", "42"], request.Arguments.ToArray());
        Assert.Equal("alpha", request.Arguments[0]);
    }

    [Fact]
    public void Request_accepts_zero_arguments_and_rejects_invalid_collections_or_tokens()
    {
        DevelopmentCommandSequence sequence = new(1);
        DevelopmentCommandId commandId = new("ping_world");

        Assert.Empty(new DevelopmentCommandRequest(sequence, commandId, []).Arguments);
        Assert.Throws<ArgumentException>(() => new DevelopmentCommandRequest(sequence, commandId, default(ImmutableArray<string>)));
        Assert.Throws<ArgumentException>(() => new DevelopmentCommandRequest(sequence, commandId, Enumerable.Repeat("x", 9)));
        Assert.Throws<ArgumentException>(() => new DevelopmentCommandRequest(sequence, commandId, [""]));
        Assert.Throws<ArgumentException>(() => new DevelopmentCommandRequest(sequence, commandId, ["has space"]));
        Assert.Throws<ArgumentException>(() => new DevelopmentCommandRequest(sequence, commandId, [new string('x', 65)]));
    }

    [Fact]
    public void Availability_rejection_and_diagnostics_are_bounded()
    {
        _ = new DevelopmentCommandAvailability(DevelopmentCommandAvailabilityState.Disabled);
        _ = new DevelopmentCommandAvailability(DevelopmentCommandAvailabilityState.Enabled);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DevelopmentCommandAvailability((DevelopmentCommandAvailabilityState)2));

        DevelopmentCommandSequence sequence = new(1);
        DevelopmentCommandId commandId = new("ping_world");
        _ = new DevelopmentCommandSucceeded(sequence, commandId, new string('x', 512));
        Assert.Throws<ArgumentException>(() => new DevelopmentCommandSucceeded(sequence, commandId, ""));
        Assert.Throws<ArgumentException>(() => new DevelopmentCommandSucceeded(sequence, commandId, "two\nlines"));
        Assert.Throws<ArgumentException>(() => new DevelopmentCommandSucceeded(sequence, commandId, new string('x', 513)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DevelopmentCommandRejected(
            sequence,
            commandId,
            (DevelopmentCommandRejectionReason)0,
            "rejected"));
    }
}
