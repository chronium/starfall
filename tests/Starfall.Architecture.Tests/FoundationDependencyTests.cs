using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Xml.Linq;
using Starfall.Protocol.Admission;

namespace Starfall.Architecture.Tests;

public sealed class FoundationDependencyTests
{
    private const string NetworkTransportAdapterReference =
        "$(ChronoFallFamilyRoot)src/ChronoFall.Network.Transport.LiteNetLib/ChronoFall.Network.Transport.LiteNetLib.csproj";

    private static readonly IReadOnlyDictionary<string, string[]> ExpectedProductReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Starfall.BalanceLab"] = ["Starfall.Content", "Starfall.Simulation"],
            ["Starfall.Client"] = ["Starfall.Content", "Starfall.Protocol"],
            ["Starfall.Content"] = [],
            ["Starfall.Editor"] = ["Starfall.Content"],
            ["Starfall.Protocol"] = [],
            ["Starfall.Simulation"] = ["Starfall.Content"],
            ["Starfall.World"] = ["Starfall.Content", "Starfall.Protocol", "Starfall.Simulation"],
        };

    private static readonly string[] ExpectedSolutionProjects =
    [
        .. ExpectedProductReferences.Keys,
        "Starfall.Architecture.Tests",
        "Starfall.Client.Tests",
        "Starfall.Content.Tests",
        "Starfall.Protocol.Tests",
        "Starfall.Simulation.Tests",
        "Starfall.World.Tests",
        "Starfall.ConnectedWalking.Tests",
        "Starfall.DevelopmentAdmission",
    ];

    private static readonly IReadOnlySet<string> ExpectedExecutableProjects =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Starfall.Client",
            "Starfall.World",
        };

    private static readonly string[] HeadlessProjects =
    [
        "Starfall.BalanceLab",
        "Starfall.Simulation",
        "Starfall.World",
    ];

    private static readonly string[] ForbiddenHeadlessDependencyFragments =
    [
        "Client",
        "Editor",
        "Gpu",
        "ImGui",
        "Rendering",
        "Sdl",
    ];

    private static readonly IReadOnlySet<string> ApprovedClientFamilySourceReferences =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "$(ChronoFallFamilyRoot)src/ChronoFall.CharacterPresentation/ChronoFall.CharacterPresentation.csproj",
            "$(ChronoFallFamilyRoot)src/ChronoFall.CharacterPresentation.Cooking/ChronoFall.CharacterPresentation.Cooking.csproj",
            "$(ChronoFallFamilyRoot)src/ChronoFall.CharacterPresentation.SdlGpu/ChronoFall.CharacterPresentation.SdlGpu.csproj",
            NetworkTransportAdapterReference,
        };

    private static readonly IReadOnlySet<string> ApprovedWorldFamilySourceReferences =
        new HashSet<string>(StringComparer.Ordinal)
        {
            NetworkTransportAdapterReference,
        };

    private static readonly IReadOnlySet<string> ApprovedSimulationFamilySourceReferences =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "$(ChronoFallFamilyRoot)src/ChronoFall.Box3D/ChronoFall.Box3D.csproj",
        };

    private static string RepositoryRoot { get; } = FindRepositoryRoot();

    [Fact]
    public void Solution_contains_exact_foundation_projects()
    {
        XDocument solution = XDocument.Load(Path.Combine(RepositoryRoot, "Starfall.slnx"));

        string[] actual = solution
            .Descendants("Project")
            .Select(project => project.Attribute("Path")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFileNameWithoutExtension(path!))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedSolutionProjects.Order(StringComparer.Ordinal), actual);
    }

    [Fact]
    public void Product_project_output_types_match_foundation_processes()
    {
        foreach (string projectName in ExpectedProductReferences.Keys)
        {
            XDocument project = LoadProductProject(projectName);
            string? outputType = project.Descendants("OutputType").SingleOrDefault()?.Value;
            string actualOutputType = string.IsNullOrWhiteSpace(outputType) ? "Library" : outputType;
            string expectedOutputType = ExpectedExecutableProjects.Contains(projectName) ? "Exe" : "Library";

            Assert.True(
                string.Equals(expectedOutputType, actualOutputType, StringComparison.OrdinalIgnoreCase),
                $"{projectName} must be {expectedOutputType}, but declares {actualOutputType}.");
        }
    }

    [Fact]
    public async Task World_finite_lifecycle_starts_drains_and_stops_successfully()
    {
        ProcessResult result = await RunProductProcessAsync(
            "Starfall.World",
            "--world", "world_1",
            "--channel", "channel_1",
            "--run-ticks", "1");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StandardError);

        string[] lines = result.StandardOutput.Split(
            Environment.NewLine,
            StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);

        string instance = Assert.IsType<System.Text.RegularExpressions.Match>(
            System.Text.RegularExpressions.Regex.Match(
                lines[0],
                "instance=(?<instance>[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})"))
            .Groups["instance"]
            .Value;
        Assert.True(Guid.TryParseExact(instance, "D", out Guid parsedInstance));
        Assert.NotEqual(Guid.Empty, parsedInstance);

        Assert.Equal(
            $"STARFALL_WORLD_READY world=world_1 channel=channel_1 instance={instance} " +
            "zone=draft_0_first_playable_zone town=town_safe branches=3 routes=4 proxies=7 spawns=10 " +
            "mode=offline listenPort=none technicalPlayer=1 players=1 tickRate=60 state=running",
            lines[0]);
        Assert.Equal(
            $"STARFALL_WORLD_DRAINING world=world_1 channel=channel_1 instance={instance} ticks=1 " +
            "players=1 state=draining",
            lines[1]);
        Assert.Equal(
            $"STARFALL_WORLD_STOPPED world=world_1 channel=channel_1 instance={instance} ticks=1 " +
            "players=0 catchUpClamps=0 reason=finite state=stopped",
            lines[2]);
    }

    [Fact]
    public async Task Client_validates_staged_character_content_without_starting_sdl()
    {
        ProcessResult result = await RunProductProcessAsync(
            "Starfall.Client",
            "--validate-character-content");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            "STARFALL_CLIENT_CHARACTER_CONTENT_READY asset=quaternius-ual1-standard " +
            $"joints=65 clips=Idle_Loop,Walk_Loop,Sword_Attack{Environment.NewLine}",
            result.StandardOutput);
        Assert.Empty(result.StandardError);
    }

    [Fact]
    public async Task Development_admission_tool_generates_non_overwriting_private_inputs_without_printing_secrets()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"starfall-admission-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string assembly = Path.Combine(
                GetProjectOutputDirectory("tools", "Starfall.DevelopmentAdmission"),
                "Starfall.DevelopmentAdmission.dll");
            ProcessResult generated = await RunAssemblyProcessAsync(
                assembly,
                "generate-key", "--key-id", "development", "--output-directory", directory);
            Assert.Equal(0, generated.ExitCode);
            string privatePath = Path.Combine(directory, "development.private.pem");
            string publicPath = Path.Combine(directory, "development.public.pem");
            Assert.True(File.Exists(privatePath));
            Assert.True(File.Exists(publicPath));
            Assert.DoesNotContain("PRIVATE KEY", generated.StandardOutput, StringComparison.Ordinal);

            Guid instance = Guid.NewGuid();
            string ticketPath = Path.Combine(directory, "world_1-channel_1.ticket");
            ProcessResult issued = await RunAssemblyProcessAsync(
                assembly,
                "issue-ticket", "--key-id", "development", "--key-directory", directory,
                "--world", "world_1", "--channel", "channel_1", "--world-instance", instance.ToString("D"),
                "--output", ticketPath);
            Assert.Equal(0, issued.ExitCode);
            string ticket = File.ReadAllText(ticketPath);
            Assert.DoesNotContain(ticket, issued.StandardOutput, StringComparison.Ordinal);
            using ECDsa publicKey = ECDsa.Create();
            publicKey.ImportFromPem(File.ReadAllText(publicPath));
            WorldJoinTicketValidationResult validation = WorldJoinTicketCodec.Validate(
                ticket,
                new WorldJoinTicketVerificationKeyRing(
                [
                    new WorldJoinTicketVerificationKey("development", publicKey.ExportSubjectPublicKeyInfo()),
                ]),
                new WorldJoinTicketAudience(
                    new WorldId("world_1"),
                    new ChannelId("channel_1"),
                    new WorldInstanceId(instance)),
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            Assert.True(validation.IsValid);

            ProcessResult overwrite = await RunAssemblyProcessAsync(
                assembly,
                "generate-key", "--key-id", "development", "--output-directory", directory);
            Assert.Equal(2, overwrite.ExitCode);
            if (!OperatingSystem.IsWindows())
            {
                Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(privatePath));
                Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(ticketPath));
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("Starfall.World", "Starfall.World: does not recognize argument '--unexpected'.")]
    [InlineData("Starfall.Client", "Starfall.Client accepts no arguments for the native preview, --validate-character-content, --capture-graybox-suite <directory>, or connected walking options.")]
    public async Task Foundation_processes_reject_unknown_arguments(
        string projectName,
        string expectedError)
    {
        ProcessResult result = await RunProductProcessAsync(projectName, "--unexpected");

        Assert.Equal(2, result.ExitCode);
        Assert.Empty(result.StandardOutput);
        Assert.Equal(expectedError + Environment.NewLine, result.StandardError);
    }

    [Fact]
    public async Task World_requires_explicit_world_and_channel_identities()
    {
        ProcessResult result = await RunProductProcessAsync("Starfall.World");

        Assert.Equal(2, result.ExitCode);
        Assert.Empty(result.StandardOutput);
        Assert.Equal(
            $"Starfall.World: requires both --world <id> and --channel <id>.{Environment.NewLine}",
            result.StandardError);
    }

    [Fact]
    public void World_output_excludes_presentation_artifacts()
    {
        string outputDirectory = GetProductOutputDirectory("Starfall.World");
        string[] fileNames = Directory
            .EnumerateFiles(outputDirectory, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(fileName => fileName is not null)
            .Cast<string>()
            .ToArray();

        string[] forbiddenFragments =
        [
            "Starfall.Client",
            "Starfall.Editor",
            "ChronoFall.CharacterPresentation",
            "SDL",
            "ImGui",
            "Blurg",
            "Rendering",
        ];
        string[] forbiddenExtensions = [".metal", ".spv", ".png", ".jpg", ".jpeg", ".ktx", ".dds"];

        string[] forbiddenFiles = fileNames
            .Where(fileName =>
                forbiddenFragments.Any(fragment =>
                    fileName.Contains(fragment, StringComparison.OrdinalIgnoreCase)) ||
                forbiddenExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            forbiddenFiles.Length == 0,
            $"Starfall.World output contains presentation artifacts: {string.Join(", ", forbiddenFiles)}.");
    }

    [Fact]
    public void Headless_consumers_contain_the_approved_shared_runtimes()
    {
        foreach (string projectName in new[]
                 {
                     "Starfall.Simulation",
                     "Starfall.World",
                     "Starfall.BalanceLab",
                 })
        {
            string output = GetProductOutputDirectory(projectName);
            Assert.True(File.Exists(Path.Combine(output, "ChronoFall.Box3D.dll")));
            Assert.True(File.Exists(Path.Combine(output, "ChronoFall.Box3D.Bindings.dll")));

            if (OperatingSystem.IsMacOS() &&
                RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
            {
                Assert.True(File.Exists(Path.Combine(
                    output,
                    "runtimes",
                    "osx-arm64",
                    "native",
                    "libbox3d.dylib")));
            }

            string[] forbidden = Directory
                .EnumerateFiles(output, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(output, path))
                .Where(path => ForbiddenHeadlessDependencyFragments.Any(fragment =>
                    path.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            Assert.True(
                forbidden.Length == 0,
                $"{projectName} contains forbidden presentation artifacts: {string.Join(", ", forbidden)}.");
        }
    }

    [Fact]
    public void Network_transport_is_emitted_only_by_client_and_world()
    {
        string[] expectedNetworkAssemblies =
        [
            "ChronoFall.Network.Transport.dll",
            "ChronoFall.Network.Transport.LiteNetLib.dll",
            "LiteNetLib.dll",
        ];

        foreach (string projectName in new[] { "Starfall.Client", "Starfall.World" })
        {
            string output = GetProductOutputDirectory(projectName);
            Assert.All(
                expectedNetworkAssemblies,
                assembly => Assert.True(
                    File.Exists(Path.Combine(output, assembly)),
                    $"{projectName} output is missing approved network assembly {assembly}."));
        }

        foreach (string projectName in new[]
                 {
                     "Starfall.Content",
                     "Starfall.Protocol",
                     "Starfall.Simulation",
                     "Starfall.Editor",
                     "Starfall.BalanceLab",
                 })
        {
            string output = GetProductOutputDirectory(projectName);
            Assert.All(
                expectedNetworkAssemblies,
                assembly => Assert.False(
                    File.Exists(Path.Combine(output, assembly)),
                    $"{projectName} unexpectedly emits network assembly {assembly}."));
        }
    }

    [Fact]
    public void BalanceLab_output_excludes_presentation_editor_and_runtime_host_artifacts()
    {
        string outputDirectory = GetProductOutputDirectory("Starfall.BalanceLab");
        string[] relativePaths = Directory
            .EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(outputDirectory, path))
            .ToArray();

        string[] forbiddenFragments =
        [
            "Starfall.Client",
            "Starfall.Editor",
            "Starfall.Protocol",
            "Starfall.World",
            "ChronoFall.CharacterPresentation",
            "SDL",
            "Gpu",
            "ImGui",
            "Blurg",
            "Rendering",
            "Shader",
            "Texture",
        ];
        string[] forbiddenExtensions =
        [
            ".metal",
            ".spv",
            ".png",
            ".jpg",
            ".jpeg",
            ".ktx",
            ".dds",
        ];

        string[] forbiddenFiles = relativePaths
            .Where(path =>
                forbiddenFragments.Any(fragment =>
                    path.Contains(fragment, StringComparison.OrdinalIgnoreCase)) ||
                forbiddenExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            forbiddenFiles.Length == 0,
            $"Starfall.BalanceLab output contains forbidden artifacts: {string.Join(", ", forbiddenFiles)}.");
    }

    [Fact]
    public void Product_project_references_match_approved_graph()
    {
        foreach ((string projectName, string[] expectedReferences) in ExpectedProductReferences)
        {
            string[] actualReferences = ReadReferencesForLocalProductGraph(
                    projectName,
                    LoadProductProject(projectName))
                .Select(path => Path.GetFileNameWithoutExtension(path) ??
                    throw new InvalidDataException($"Project reference has no file name: {path}."))
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(expectedReferences.Order(StringComparer.Ordinal), actualReferences);
        }
    }

    [Fact]
    public void Development_admission_tool_is_protocol_and_bcl_only()
    {
        string path = Path.Combine(
            RepositoryRoot,
            "tools",
            "Starfall.DevelopmentAdmission",
            "Starfall.DevelopmentAdmission.csproj");
        XDocument project = XDocument.Load(path);
        string[] references = ReadProjectReferences(project)
            .Select(Path.GetFileNameWithoutExtension)
            .Order(StringComparer.Ordinal)
            .ToArray()!;
        Assert.Equal(["Starfall.Protocol"], references);
        Assert.Equal("Exe", Assert.Single(project.Descendants("OutputType")).Value);
    }

    [Fact]
    public void Approved_client_family_references_do_not_change_the_local_product_graph()
    {
        XDocument project = new(
            new XElement(
                "Project",
                new XElement(
                    "ItemGroup",
                    new XElement(
                        "ProjectReference",
                        new XAttribute("Include", "../Starfall.Content/Starfall.Content.csproj")),
                    new XElement(
                        "ProjectReference",
                        new XAttribute("Include", "../Starfall.Protocol/Starfall.Protocol.csproj")),
                    ApprovedClientFamilySourceReferences
                        .Order(StringComparer.Ordinal)
                        .Select(reference => new XElement(
                            "ProjectReference",
                            new XAttribute("Include", reference))))));

        Assert.All(
            ApprovedClientFamilySourceReferences,
            reference => Assert.True(IsApprovedFamilySourceReference("Starfall.Client", reference)));
        Assert.Equal(
        [
            "../Starfall.Content/Starfall.Content.csproj",
            "../Starfall.Protocol/Starfall.Protocol.csproj",
        ],
        ReadReferencesForLocalProductGraph("Starfall.Client", project));

        const string unapprovedReference =
            "$(ChronoFallFamilyRoot)src/ChronoFall.CharacterExperiment.SdlGpu/ChronoFall.CharacterExperiment.SdlGpu.csproj";
        project.Root!.Element("ItemGroup")!.Add(
            new XElement("ProjectReference", new XAttribute("Include", unapprovedReference)));

        Assert.Contains(
            unapprovedReference,
            ReadReferencesForLocalProductGraph("Starfall.Client", project));
    }

    [Fact]
    public void Client_references_exact_approved_family_source_set()
    {
        XDocument client = LoadProductProject("Starfall.Client");
        XElement[] familyReferences = client
            .Descendants("ProjectReference")
            .Where(reference => IsApprovedFamilySourceReference(
                "Starfall.Client",
                reference.Attribute("Include")?.Value ?? string.Empty))
            .ToArray();
        string[] actual = familyReferences
            .Select(reference => reference.Attribute("Include")!.Value)
            .Where(reference => IsApprovedFamilySourceReference("Starfall.Client", reference))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ApprovedClientFamilySourceReferences.Order(StringComparer.Ordinal), actual);
        XElement configurationPolicy = Assert.Single(
            client.Descendants("ShouldUnsetParentConfigurationAndPlatform"));
        Assert.Equal("false", configurationPolicy.Value);
        Assert.All(
            familyReferences,
            reference => Assert.Equal(
                "ShouldUnsetParentConfigurationAndPlatform=false",
                reference.Attribute("AdditionalProperties")?.Value));
    }

    [Fact]
    public void World_references_exact_approved_family_source_set()
    {
        XDocument world = LoadProductProject("Starfall.World");
        XElement[] familyReferences = world
            .Descendants("ProjectReference")
            .Where(reference => IsApprovedFamilySourceReference(
                "Starfall.World",
                reference.Attribute("Include")?.Value ?? string.Empty))
            .ToArray();
        string[] actual = familyReferences
            .Select(reference => reference.Attribute("Include")!.Value)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ApprovedWorldFamilySourceReferences.Order(StringComparer.Ordinal), actual);
        XElement configurationPolicy = Assert.Single(
            world.Descendants("ShouldUnsetParentConfigurationAndPlatform"));
        Assert.Equal("false", configurationPolicy.Value);
        Assert.All(
            familyReferences,
            reference => Assert.Equal(
                "ShouldUnsetParentConfigurationAndPlatform=false",
                reference.Attribute("AdditionalProperties")?.Value));
    }

    [Fact]
    public void Simulation_references_exact_approved_headless_family_source_set()
    {
        XDocument simulation = LoadProductProject("Starfall.Simulation");
        XElement[] familyReferences = simulation
            .Descendants("ProjectReference")
            .Where(reference => IsApprovedFamilySourceReference(
                "Starfall.Simulation",
                reference.Attribute("Include")?.Value ?? string.Empty))
            .ToArray();
        string[] actual = familyReferences
            .Select(reference => reference.Attribute("Include")!.Value)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ApprovedSimulationFamilySourceReferences.Order(StringComparer.Ordinal), actual);
        XElement configurationPolicy = Assert.Single(
            simulation.Descendants("ShouldUnsetParentConfigurationAndPlatform"));
        Assert.Equal("false", configurationPolicy.Value);
        Assert.All(
            familyReferences,
            reference => Assert.Equal(
                "ShouldUnsetParentConfigurationAndPlatform=false",
                reference.Attribute("AdditionalProperties")?.Value));
        Assert.DoesNotContain(
            simulation.Descendants("ProjectReference"),
            reference => (reference.Attribute("Include")?.Value ?? string.Empty)
                .Contains("ChronoFall.Box3D.Bindings", StringComparison.Ordinal));
    }

    [Fact]
    public void Client_output_contains_only_the_bounded_character_runtime_inputs()
    {
        string output = GetProductOutputDirectory("Starfall.Client");
        string content = Path.Combine(
            output,
            "content",
            "chronofall",
            "character-presentation",
            "client");

        Assert.True(File.Exists(Path.Combine(content, "quaternius-ual1-standard.cfskel")));
        Assert.True(File.Exists(Path.Combine(content, "quaternius-ual1-standard.provenance.json")));
        Assert.True(File.Exists(Path.Combine(
            content,
            "licenses",
            "quaternius-ual1-standard",
            "License.txt")));
        Assert.True(File.Exists(Path.Combine(
            content,
            "licenses",
            "quaternius-ual1-standard",
            "README.txt")));

        string[] contentFiles = Directory
            .EnumerateFiles(content, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(content, path))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
        [
            Path.Combine("licenses", "quaternius-ual1-standard", "License.txt"),
            Path.Combine("licenses", "quaternius-ual1-standard", "README.txt"),
            "quaternius-ual1-standard.cfskel",
            "quaternius-ual1-standard.provenance.json",
        ],
        contentFiles);
        Assert.DoesNotContain(
            Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories),
            path => string.Equals(Path.GetExtension(path), ".glb", StringComparison.OrdinalIgnoreCase));

        string shaderDirectory = Path.Combine(output, "shaders");
        Assert.True(File.Exists(Path.Combine(shaderDirectory, "skinned-character.vert.msl")));
        Assert.True(File.Exists(Path.Combine(shaderDirectory, "skinned-character.frag.msl")));
        Assert.True(File.Exists(Path.Combine(shaderDirectory, "skinned-character.vert.spv")));
        Assert.True(File.Exists(Path.Combine(shaderDirectory, "skinned-character.frag.spv")));

        if (OperatingSystem.IsMacOS() &&
            RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
        {
            Assert.True(File.Exists(Path.Combine(
                output,
                "runtimes",
                "osx-arm64",
                "native",
                "libSDL3.dylib")));
        }
    }

    [Fact]
    public void Product_projects_have_no_external_packages()
    {
        foreach (string projectName in ExpectedProductReferences.Keys)
        {
            string[] packageReferences = LoadProductProject(projectName)
                .Descendants("PackageReference")
                .Select(reference => reference.Attribute("Include")?.Value)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .ToArray();

            Assert.True(
                packageReferences.Length == 0,
                $"{projectName} unexpectedly references packages: {string.Join(", ", packageReferences)}.");
        }
    }

    [Fact]
    public void Headless_projects_exclude_presentation_and_editor_dependencies()
    {
        foreach (string projectName in HeadlessProjects)
        {
            XDocument project = LoadProductProject(projectName);
            string[] dependencies = project
                .Descendants()
                .Where(element => element.Name.LocalName is "ProjectReference" or "PackageReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToArray();

            string[] forbidden = dependencies
                .Where(dependency => ForbiddenHeadlessDependencyFragments.Any(
                    fragment => dependency.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            Assert.True(
                forbidden.Length == 0,
                $"{projectName} has forbidden headless dependencies: {string.Join(", ", forbidden)}.");
        }
    }

    [Fact]
    public void Family_root_defaults_to_the_canonical_coordinator_checkout()
    {
        XDocument properties = XDocument.Load(Path.Combine(RepositoryRoot, "Directory.Build.props"));
        XElement root = Assert.Single(properties.Descendants("ChronoFallFamilyRoot"));

        Assert.Equal("'$(ChronoFallFamilyRoot)' == ''", root.Attribute("Condition")?.Value);
        Assert.Equal(
            "$([MSBuild]::NormalizeDirectory('$(MSBuildThisFileDirectory)..'))",
            root.Value);
    }

    [Fact]
    public void Repository_root_centralizes_local_generated_content_paths()
    {
        XDocument properties = XDocument.Load(Path.Combine(RepositoryRoot, "Directory.Build.props"));
        XElement root = Assert.Single(properties.Descendants("StarfallRepositoryRoot"));
        Assert.Equal(
            "$([MSBuild]::NormalizeDirectory('$(MSBuildThisFileDirectory)'))",
            root.Value);

        XDocument client = LoadProductProject("Starfall.Client");
        XElement stageRoot = Assert.Single(client.Descendants("StarfallCharacterStageRoot"));
        Assert.Equal(
            "$(StarfallRepositoryRoot)artifacts/chronofall/character-presentation/client/",
            stageRoot.Value);
    }

    [Theory]
    [InlineData("Starfall.Client", "$(ChronoFallFamilyRoot)src/ChronoFall.CharacterPresentation/ChronoFall.CharacterPresentation.csproj", true)]
    [InlineData("Starfall.Client", "$(ChronoFallFamilyRoot)src/ChronoFall.CharacterPresentation.Cooking/ChronoFall.CharacterPresentation.Cooking.csproj", true)]
    [InlineData("Starfall.Client", "$(ChronoFallFamilyRoot)src/ChronoFall.CharacterPresentation.SdlGpu/ChronoFall.CharacterPresentation.SdlGpu.csproj", true)]
    [InlineData("Starfall.Client", NetworkTransportAdapterReference, true)]
    [InlineData("Starfall.World", NetworkTransportAdapterReference, true)]
    [InlineData("Starfall.Protocol", NetworkTransportAdapterReference, false)]
    [InlineData("Starfall.World", "$(ChronoFallFamilyRoot)src/ChronoFall.CharacterPresentation/ChronoFall.CharacterPresentation.csproj", false)]
    [InlineData("Starfall.Client", "$(ChronoFallFamilyRoot)royale/src/Royale.Client/Royale.Client.csproj", false)]
    [InlineData("Starfall.Client", "$(ChronoFallFamilyRoot)thirdparty/repos/SDL3-CS/SDL3-CS/SDL3-CS.csproj", false)]
    [InlineData("Starfall.Simulation", "$(ChronoFallFamilyRoot)src/ChronoFall.Box3D/ChronoFall.Box3D.csproj", true)]
    [InlineData("Starfall.World", "$(ChronoFallFamilyRoot)src/ChronoFall.Box3D/ChronoFall.Box3D.csproj", false)]
    [InlineData("Starfall.Simulation", "$(ChronoFallFamilyRoot)src/ChronoFall.Box3D.Bindings/ChronoFall.Box3D.Bindings.csproj", false)]
    [InlineData("Starfall.Client", "$(OtherRoot)src/ChronoFall.CharacterPresentation/ChronoFall.CharacterPresentation.csproj", false)]
    public void Family_source_reference_policy_is_narrow(
        string projectName,
        string reference,
        bool expected)
    {
        Assert.Equal(expected, IsApprovedFamilySourceReference(projectName, reference));
    }

    [Fact]
    public void Project_references_and_imports_follow_repository_and_family_policy()
    {
        IEnumerable<string> projectFiles = Directory.EnumerateFiles(
            RepositoryRoot,
            "*.csproj",
            SearchOption.AllDirectories);
        IEnumerable<string> buildFiles = Directory.EnumerateFiles(
            RepositoryRoot,
            "*.*",
            SearchOption.TopDirectoryOnly)
            .Where(path => Path.GetExtension(path) is ".props" or ".targets");

        foreach (string filePath in projectFiles.Concat(buildFiles))
        {
            XDocument document = XDocument.Load(filePath);
            IEnumerable<(string Kind, string Path)> paths = document
                .Descendants()
                .Where(element => element.Name.LocalName is "ProjectReference" or "Import")
                .Select(element => (
                    element.Name.LocalName,
                    element.Attribute(element.Name.LocalName == "Import" ? "Project" : "Include")?.Value))
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Value))
                .Select(entry => (entry.LocalName, entry.Value!));

            string projectName = Path.GetFileNameWithoutExtension(filePath);
            foreach ((string kind, string path) in paths)
            {
                if (path.Contains("$(", StringComparison.Ordinal))
                {
                    Assert.True(
                        kind == "ProjectReference" && IsApprovedFamilySourceReference(projectName, path),
                        $"{filePath} uses unapproved property-rooted {kind} path {path}.");
                    continue;
                }

                Assert.False(Path.IsPathRooted(path), $"{filePath} uses absolute path {path}.");

                string resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(filePath)!, path));
                string relative = Path.GetRelativePath(RepositoryRoot, resolved);

                Assert.False(
                    relative == ".." ||
                    relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal),
                    $"{filePath} escapes the Starfall repository through {path}.");
            }
        }
    }

    private static bool IsApprovedFamilySourceReference(string projectName, string reference)
    {
        return (string.Equals(projectName, "Starfall.Client", StringComparison.Ordinal) &&
                ApprovedClientFamilySourceReferences.Contains(reference)) ||
            (string.Equals(projectName, "Starfall.World", StringComparison.Ordinal) &&
                ApprovedWorldFamilySourceReferences.Contains(reference)) ||
            (string.Equals(projectName, "Starfall.Simulation", StringComparison.Ordinal) &&
                ApprovedSimulationFamilySourceReferences.Contains(reference));
    }

    private static XDocument LoadProductProject(string projectName)
    {
        string projectPath = Path.Combine(
            RepositoryRoot,
            "src",
            projectName,
            $"{projectName}.csproj");

        return XDocument.Load(projectPath);
    }

    private static IEnumerable<string> ReadProjectReferences(XDocument project)
    {
        return project
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>();
    }

    private static IEnumerable<string> ReadReferencesForLocalProductGraph(
        string projectName,
        XDocument project)
    {
        return ReadProjectReferences(project)
            .Where(reference => !IsApprovedFamilySourceReference(projectName, reference));
    }

    private static string GetProductOutputDirectory(string projectName)
    {
        DirectoryInfo testOutputDirectory = new(AppContext.BaseDirectory);
        string targetFramework = testOutputDirectory.Name;
        string configuration = testOutputDirectory.Parent?.Name ??
            throw new DirectoryNotFoundException(
                $"Could not determine the build configuration from {AppContext.BaseDirectory}.");

        string outputDirectory = Path.Combine(
            RepositoryRoot,
            "src",
            projectName,
            "bin",
            configuration,
            targetFramework);

        Assert.True(
            Directory.Exists(outputDirectory),
            $"Expected product output directory {outputDirectory} does not exist.");

        return outputDirectory;
    }

    private static string GetProjectOutputDirectory(string folder, string projectName)
    {
        DirectoryInfo testOutputDirectory = new(AppContext.BaseDirectory);
        string targetFramework = testOutputDirectory.Name;
        string configuration = testOutputDirectory.Parent?.Name ??
            throw new DirectoryNotFoundException("Could not determine test build configuration.");
        return Path.Combine(RepositoryRoot, folder, projectName, "bin", configuration, targetFramework);
    }

    private static async Task<ProcessResult> RunProductProcessAsync(
        string projectName,
        params string[] arguments)
    {
        string assemblyPath = Path.Combine(
            GetProductOutputDirectory(projectName),
            $"{projectName}.dll");
        return await RunAssemblyProcessAsync(assemblyPath, arguments);
    }

    private static async Task<ProcessResult> RunAssemblyProcessAsync(
        string assemblyPath,
        params string[] arguments)
    {
        Assert.True(File.Exists(assemblyPath), $"Expected executable assembly {assemblyPath} does not exist.");

        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = RepositoryRoot,
        };
        startInfo.ArgumentList.Add(assemblyPath);
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        Assert.True(process.Start(), $"Failed to start {Path.GetFileName(assemblyPath)}.");

        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);

            await process.WaitForExitAsync();
            throw new TimeoutException($"{Path.GetFileName(assemblyPath)} did not exit within 10 seconds.");
        }

        return new ProcessResult(
            process.ExitCode,
            await standardOutput,
            await standardError);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Starfall.slnx")) &&
                File.Exists(Path.Combine(directory.FullName, ".pm", "project_id.txt")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate the Starfall repository from {AppContext.BaseDirectory}.");
    }

    private sealed record ProcessResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
