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
    ];

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
    public void Foundation_product_projects_are_libraries()
    {
        foreach (string projectName in ExpectedProductReferences.Keys)
        {
            XDocument project = LoadProductProject(projectName);
            string? outputType = project.Descendants("OutputType").SingleOrDefault()?.Value;

            Assert.True(
                string.IsNullOrWhiteSpace(outputType) ||
                string.Equals(outputType, "Library", StringComparison.OrdinalIgnoreCase),
                $"{projectName} must remain a library until its owning executable-shell task.");
        }
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
}
