using System.Security.Cryptography;
using Starfall.Protocol.Admission;

return DevelopmentAdmissionProgram.Run(args);

internal static class DevelopmentAdmissionProgram
{
    internal static int Run(string[] args)
    {
        try
        {
            if (args.Length == 0)
                throw new ArgumentException("Expected generate-key or issue-ticket.");
            return args[0] switch
            {
                "generate-key" => GenerateKey(Parse(args[1..])),
                "issue-ticket" => IssueTicket(Parse(args[1..])),
                _ => throw new ArgumentException($"Unknown command '{args[0]}'."),
            };
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Starfall.DevelopmentAdmission: {exception.Message}");
            return 2;
        }
    }

    private static int GenerateKey(IReadOnlyDictionary<string, string> options)
    {
        RequireOnly(options, "--key-id", "--output-directory");
        string keyId = Required(options, "--key-id");
        ValidateKeyId(keyId);
        string outputDirectory = Path.GetFullPath(Required(options, "--output-directory"));
        Directory.CreateDirectory(outputDirectory);
        string privatePath = Path.Combine(outputDirectory, "development.private.pem");
        string publicPath = Path.Combine(outputDirectory, "development.public.pem");
        RefuseExisting(privatePath, publicPath);

        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        File.WriteAllText(privatePath, key.ExportECPrivateKeyPem());
        File.WriteAllText(publicPath, key.ExportSubjectPublicKeyInfoPem());
        SetMode(privatePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        SetMode(publicPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
        Console.WriteLine($"STARFALL_DEVELOPMENT_KEY_READY keyId={keyId} publicKey={publicPath}");
        return 0;
    }

    private static int IssueTicket(IReadOnlyDictionary<string, string> options)
    {
        RequireOnly(
            options,
            "--key-id",
            "--key-directory",
            "--world",
            "--channel",
            "--world-instance",
            "--output");
        string keyId = Required(options, "--key-id");
        string keyDirectory = Path.GetFullPath(Required(options, "--key-directory"));
        string output = Path.GetFullPath(Required(options, "--output"));
        RefuseExisting(output);
        Guid worldInstance = Guid.Parse(Required(options, "--world-instance"));
        long issued = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var claims = new WorldJoinTicketClaims(
            new JoinTicketId(Guid.NewGuid()),
            new AccountId(Guid.NewGuid()),
            new CharacterId(Guid.NewGuid()),
            new WorldId(Required(options, "--world")),
            new ChannelId(Required(options, "--channel")),
            new WorldInstanceId(worldInstance),
            issued,
            checked(issued + WorldJoinTicketCodec.MaximumLifetimeMilliseconds));

        using ECDsa signingKey = ECDsa.Create();
        signingKey.ImportFromPem(File.ReadAllText(Path.Combine(keyDirectory, "development.private.pem")));
        string ticket = WorldJoinTicketCodec.Issue(claims, keyId, signingKey);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        File.WriteAllText(output, ticket);
        SetMode(output, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        Console.WriteLine($"STARFALL_DEVELOPMENT_TICKET_READY keyId={keyId} world={claims.WorldId} channel={claims.ChannelId} output={output}");
        return 0;
    }

    private static Dictionary<string, string> Parse(IReadOnlyList<string> args)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int index = 0; index < args.Count; index += 2)
        {
            if (index + 1 >= args.Count || !args[index].StartsWith("--", StringComparison.Ordinal) ||
                !values.TryAdd(args[index], args[index + 1]))
            {
                throw new ArgumentException("Options must be unique --name value pairs.");
            }
        }
        return values;
    }

    private static string Required(IReadOnlyDictionary<string, string> options, string name) =>
        options.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"{name} is required.");

    private static void RequireOnly(IReadOnlyDictionary<string, string> options, params string[] allowed)
    {
        string? unexpected = options.Keys.FirstOrDefault(key => !allowed.Contains(key, StringComparer.Ordinal));
        if (unexpected is not null)
            throw new ArgumentException($"Unknown option '{unexpected}'.");
    }

    private static void ValidateKeyId(string keyId)
    {
        if (keyId.Length is > 64 || keyId.Any(static character =>
                character is not (>= 'a' and <= 'z') &&
                character is not (>= 'A' and <= 'Z') &&
                character is not (>= '0' and <= '9') &&
                character is not '_' and not '-'))
        {
            throw new ArgumentException("--key-id must contain 1-64 ASCII letters, digits, underscores or hyphens.");
        }
    }

    private static void RefuseExisting(params string[] paths)
    {
        string? existing = paths.FirstOrDefault(File.Exists);
        if (existing is not null)
            throw new IOException($"Refusing to overwrite existing file: {existing}");
    }

    private static void SetMode(string path, UnixFileMode mode)
    {
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, mode);
    }
}
