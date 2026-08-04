using Starfall.World.Launch;

namespace Starfall.World.Tests;

public sealed class WorldLaunchOptionsTests
{
    [Fact]
    public void Parses_required_identities_and_optional_finite_tick_count()
    {
        WorldLaunchOptions options = WorldLaunchOptions.Parse(
        [
            "--channel", "channel_2",
            "--run-ticks", "120",
            "--world", "world_1",
        ]);

        Assert.Equal("world_1", options.WorldId.Value);
        Assert.Equal("channel_2", options.ChannelId.Value);
        Assert.Equal(120, options.RunTicks);
        Assert.Null(options.ListenPort);
        Assert.Empty(options.VerificationKeyPaths);
    }

    [Fact]
    public void Omits_finite_tick_count_for_persistent_hosting()
    {
        WorldLaunchOptions options = WorldLaunchOptions.Parse(
            ["--world", "world_1", "--channel", "channel_1"]);

        Assert.Null(options.RunTicks);
    }

    [Fact]
    public void Parses_connected_mode_and_requires_key_without_finite_ticks()
    {
        WorldLaunchOptions options = WorldLaunchOptions.Parse(
        [
            "--world", "world_1", "--channel", "channel_1",
            "--listen-port", "7777",
            "--verification-key", "development=keys/public.pem",
        ]);
        Assert.True(options.IsConnected);
        Assert.Equal(7777, options.ListenPort);
        Assert.EndsWith(Path.Combine("keys", "public.pem"), options.VerificationKeyPaths["development"]);
        Assert.Null(options.RunTicks);

        Assert.Throws<WorldLaunchOptionsException>(() => WorldLaunchOptions.Parse(
            ["--world", "world_1", "--channel", "channel_1", "--listen-port", "7777"]));
        Assert.Throws<WorldLaunchOptionsException>(() => WorldLaunchOptions.Parse(
            ["--world", "world_1", "--channel", "channel_1", "--verification-key", "development=x"]));
        Assert.Throws<WorldLaunchOptionsException>(() => WorldLaunchOptions.Parse(
            ["--world", "world_1", "--channel", "channel_1", "--run-ticks", "1", "--listen-port", "7777", "--verification-key", "development=x"]));
    }

    [Theory]
    [InlineData()]
    [InlineData("--world", "world_1")]
    [InlineData("--channel", "channel_1")]
    public void Requires_world_and_channel(params string[] arguments)
    {
        WorldLaunchOptionsException exception = Assert.Throws<WorldLaunchOptionsException>(
            () => WorldLaunchOptions.Parse(arguments));

        Assert.Equal("requires both --world <id> and --channel <id>.", exception.Message);
    }

    [Theory]
    [InlineData("--world", "world_1", "--world", "world_2", "--channel", "channel_1")]
    [InlineData("--world", "world_1", "--channel", "channel_1", "--channel", "channel_2")]
    [InlineData("--world", "world_1", "--channel", "channel_1", "--run-ticks", "1", "--run-ticks", "2")]
    public void Rejects_duplicate_options(params string[] arguments)
    {
        Assert.Throws<WorldLaunchOptionsException>(() => WorldLaunchOptions.Parse(arguments));
    }

    [Theory]
    [InlineData("--unexpected")]
    [InlineData("world_1")]
    public void Rejects_unknown_options(params string[] arguments)
    {
        WorldLaunchOptionsException exception = Assert.Throws<WorldLaunchOptionsException>(
            () => WorldLaunchOptions.Parse(arguments));

        Assert.Contains("does not recognize argument", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--world")]
    [InlineData("--world", "--channel", "channel_1")]
    public void Rejects_missing_option_values(params string[] arguments)
    {
        Assert.Throws<WorldLaunchOptionsException>(() => WorldLaunchOptions.Parse(arguments));
    }

    [Theory]
    [InlineData("World")]
    [InlineData("1_world")]
    [InlineData("world-with-dash")]
    public void Rejects_malformed_world_identity(string value)
    {
        Assert.Throws<WorldLaunchOptionsException>(() => WorldLaunchOptions.Parse(
            ["--world", value, "--channel", "channel_1"]));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("many")]
    public void Rejects_invalid_finite_tick_counts(string value)
    {
        Assert.Throws<WorldLaunchOptionsException>(() => WorldLaunchOptions.Parse(
            ["--world", "world_1", "--channel", "channel_1", "--run-ticks", value]));
    }
}
