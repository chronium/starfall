using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Starfall.Architecture.Tests;

public sealed class FoundationDependencyTests
{
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
    public async Task World_foundation_shell_starts_and_exits_successfully()
    {
        ProcessResult result = await RunProductProcessAsync("Starfall.World");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            $"Starfall.World foundation shell started.{Environment.NewLine}",
            result.StandardOutput);
        Assert.Empty(result.StandardError);
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

    [Theory]
    [InlineData("Starfall.World", "Starfall.World foundation shell does not accept arguments.")]
    [InlineData("Starfall.Client", "Starfall.Client accepts no arguments for the native preview or --validate-character-content.")]
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
    [InlineData("Starfall.World", "$(ChronoFallFamilyRoot)src/ChronoFall.CharacterPresentation/ChronoFall.CharacterPresentation.csproj", false)]
    [InlineData("Starfall.Client", "$(ChronoFallFamilyRoot)royale/src/Royale.Client/Royale.Client.csproj", false)]
    [InlineData("Starfall.Client", "$(ChronoFallFamilyRoot)thirdparty/repos/SDL3-CS/SDL3-CS/SDL3-CS.csproj", false)]
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
        return string.Equals(projectName, "Starfall.Client", StringComparison.Ordinal) &&
            ApprovedClientFamilySourceReferences.Contains(reference);
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

    private static async Task<ProcessResult> RunProductProcessAsync(
        string projectName,
        params string[] arguments)
    {
        string assemblyPath = Path.Combine(
            GetProductOutputDirectory(projectName),
            $"{projectName}.dll");
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
        Assert.True(process.Start(), $"Failed to start {projectName}.");

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
            throw new TimeoutException($"{projectName} did not exit within 10 seconds.");
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
