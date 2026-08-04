using ChronoFall.CharacterPresentation;
using ChronoFall.CharacterPresentation.Cooking;
using Starfall.Client.Networking;

namespace Starfall.Client;

internal static class ClientApplication
{
    private const string ValidateContentArgument = "--validate-character-content";
    private const string CaptureGrayboxSuiteArgument = "--capture-graybox-suite";

    internal static int Run(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        try
        {
            if (args.Length == 0)
            {
                CharacterPresentationContent content = CharacterPresentationContent.LoadFromRuntimeOutput();
                Console.WriteLine(
                    $"STARFALL_CLIENT_PRESENTATION_START asset={content.Cooked.Descriptor.AssetId} " +
                    $"joints={content.Cooked.Asset.Mesh.Skin.Skeleton.JointCount} clip={content.IdleAnimation.Name}");
                NativeClientPreview.Run(content);
                Console.WriteLine("STARFALL_CLIENT_PRESENTATION_STOP");
                return 0;
            }

            if (args.Length == 1 && string.Equals(args[0], ValidateContentArgument, StringComparison.Ordinal))
            {
                CharacterPresentationContent content = CharacterPresentationContent.LoadFromRuntimeOutput();
                Console.WriteLine(content.CreateValidationSummary());
                return 0;
            }

            if (args.Length == 2 && string.Equals(args[0], CaptureGrayboxSuiteArgument, StringComparison.Ordinal))
            {
                CharacterPresentationContent content = CharacterPresentationContent.LoadFromRuntimeOutput();
                NativeClientPreview.CaptureSuite(content, args[1]);
                return 0;
            }

            if (args.Contains("--connect-address", StringComparer.Ordinal))
            {
                ConnectedClientLaunchOptions options = ConnectedClientLaunchOptions.Parse(args);
                string ticket = File.ReadAllText(options.JoinTicketPath).Trim();
                CharacterPresentationContent content = CharacterPresentationContent.LoadFromRuntimeOutput();
                using var session = new ConnectedWalkingClientSession(ClientNetworkTransportFactory.Create(), ticket);
                session.ConnectAndAwaitInitialSnapshot(options);
                Console.WriteLine(
                    $"STARFALL_CLIENT_CONNECTED session={session.SessionId} " +
                    $"entity={session.Snapshot!.Value.Identity} tick={session.Snapshot.Value.Tick}");
                NativeClientPreview.Run(content, session);
                Console.WriteLine("STARFALL_CLIENT_CONNECTED_STOP");
                return 0;
            }

            Console.Error.WriteLine(
                $"Starfall.Client accepts no arguments for the native preview, {ValidateContentArgument}, " +
                $"{CaptureGrayboxSuiteArgument} <directory>, or connected walking options.");
            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"STARFALL_CLIENT_FAILURE: {exception}");
            return 1;
        }
    }
}

internal sealed record CharacterPresentationContent(
    CookedSkeletalCharacterAsset Cooked,
    AnimationClip IdleAnimation,
    AnimationClip WalkAnimation)
{
    private const string ExpectedAssetId = "quaternius-ual1-standard";
    private const string ExpectedIdleClip = "Idle_Loop";
    private const string ExpectedWalkClip = "Walk_Loop";
    private const int ExpectedJointCount = 65;

    private static readonly string[] ExpectedClips =
    [
        "Idle_Loop",
        "Walk_Loop",
        "Sword_Attack",
    ];

    internal static string RuntimeContentRoot => Path.Combine(
        AppContext.BaseDirectory,
        "content",
        "chronofall",
        "character-presentation",
        "client");

    internal static CharacterPresentationContent LoadFromRuntimeOutput()
    {
        string assetPath = Path.Combine(RuntimeContentRoot, "quaternius-ual1-standard.cfskel");
        if (!File.Exists(assetPath))
            throw new FileNotFoundException($"Staged Starfall character asset was not found: {assetPath}", assetPath);

        CookedSkeletalCharacterAsset cooked;
        using (FileStream stream = File.OpenRead(assetPath))
            cooked = SkeletalAssetCookedFormat.Read(stream);

        if (!string.Equals(cooked.Descriptor.AssetId, ExpectedAssetId, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Expected cooked asset '{ExpectedAssetId}', received '{cooked.Descriptor.AssetId}'.");
        }

        int jointCount = cooked.Asset.Mesh.Skin.Skeleton.JointCount;
        if (jointCount != ExpectedJointCount)
            throw new InvalidDataException($"Expected {ExpectedJointCount} joints, received {jointCount}.");

        string[] clips = cooked.Asset.Animations
            .Select(static clip => clip.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] expected = ExpectedClips.Order(StringComparer.Ordinal).ToArray();
        if (!clips.SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                $"Expected clips [{string.Join(",", expected)}], received [{string.Join(",", clips)}].");
        }

        AnimationClip idle = cooked.Asset.Animations.Single(
            clip => string.Equals(clip.Name, ExpectedIdleClip, StringComparison.Ordinal));
        AnimationClip walk = cooked.Asset.Animations.Single(
            clip => string.Equals(clip.Name, ExpectedWalkClip, StringComparison.Ordinal));
        return new CharacterPresentationContent(cooked, idle, walk);
    }

    internal string CreateValidationSummary() =>
        $"STARFALL_CLIENT_CHARACTER_CONTENT_READY asset={Cooked.Descriptor.AssetId} " +
        $"joints={Cooked.Asset.Mesh.Skin.Skeleton.JointCount} " +
        $"clips={string.Join(",", ExpectedClips)}";
}
