using Mono.Cecil;
using Mono.Cecil.Cil;
using System.IO.Compression;

if (args.Length == 2 && args[0] == "--verify-apk")
{
    VerifyApkCryptoPatches(args[1]);
    return;
}

if (args.Length < 2 || args.Length > 3)
{
    Console.Error.WriteLine("Usage: SteamKitPatch <SteamKit2.dll> <STS2Mobile.dll> [sts2.dll]");
    Console.Error.WriteLine("       SteamKitPatch --verify-apk <apk>");
    Environment.Exit(2);
}

var steamKitPath = args[0];
var helperPath = args[1];
var gamePath = args.Length == 3 ? args[2] : null;
AssertStagedTogether(steamKitPath, helperPath);
var resolver = CreateResolver(steamKitPath, helperPath, gamePath);
var reader = new ReaderParameters { AssemblyResolver = resolver, ReadWrite = false };
var steamKit = ModuleDefinition.ReadModule(steamKitPath, reader);
var helper = ModuleDefinition.ReadModule(helperPath, reader);

var helperType = helper.GetType("STS2Mobile.Steam.AndroidJavaCrypto")
    ?? throw new InvalidOperationException("Could not find AndroidJavaCrypto helper type");
if (!helperType.IsPublic)
    throw new InvalidOperationException("AndroidJavaCrypto helper type must be public for patched external assemblies");

var cryptoPatchRules = CreateCryptoPatchRules(helperType, steamKit);
var cryptoPatchCounts = PatchCryptoCalls(steamKit, cryptoPatchRules);
AssertRequiredCryptoPatches(cryptoPatchCounts);
AssertNoForbiddenCryptoCallsites(steamKit);
AssertPatchedHelperCallsites(steamKit, cryptoPatchRules, cryptoPatchCounts);
AssertReferencesHelperAssembly(steamKit, helperType);

WriteModuleOverOriginal(steamKit, steamKitPath);
Console.WriteLine(FormatCryptoPatchSummary(cryptoPatchCounts));

var webSocketClientPath = Path.Combine(
    Path.GetDirectoryName(Path.GetFullPath(steamKitPath))!,
    "System.Net.WebSockets.Client.dll"
);
if (File.Exists(webSocketClientPath))
{
    AssertStagedTogether(webSocketClientPath, helperPath);
    PatchWebSocketClientCrypto(webSocketClientPath, helperType, resolver);
}
else
    throw new FileNotFoundException(
        "System.Net.WebSockets.Client.dll is required for Android WebSocket crypto patching",
        webSocketClientPath
    );

helper.Dispose();

if (!string.IsNullOrWhiteSpace(gamePath))
    PatchGamePlatformUtil(gamePath, resolver);

static DefaultAssemblyResolver CreateResolver(params string?[] assemblyPaths)
{
    var resolver = new DefaultAssemblyResolver();
    foreach (var assemblyPath in assemblyPaths)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
            continue;

        resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(assemblyPath))!);
    }

    return resolver;
}

static void VerifyApkCryptoPatches(string apkPath)
{
    if (!File.Exists(apkPath))
        throw new FileNotFoundException("APK not found", apkPath);

    var tempDir = Path.Combine(Path.GetTempPath(), "sts2-apk-crypto-verify-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempDir);
    var openedModules = new List<ModuleDefinition>();
    try
    {
        var steamKitPath = ExtractApkBclEntry(apkPath, "SteamKit2.dll", tempDir);
        var helperPath = ExtractApkBclEntry(apkPath, "STS2Mobile.dll", tempDir);
        var webSocketClientPath = ExtractApkBclEntry(apkPath, "System.Net.WebSockets.Client.dll", tempDir);
        var resolver = CreateResolver(steamKitPath, helperPath, webSocketClientPath);
        var reader = new ReaderParameters { AssemblyResolver = resolver, ReadWrite = false };
        var helper = ModuleDefinition.ReadModule(helperPath, reader);
        openedModules.Add(helper);
        var helperType = helper.GetType("STS2Mobile.Steam.AndroidJavaCrypto")
            ?? throw new InvalidOperationException("Could not find AndroidJavaCrypto helper type in APK");
        if (!helperType.IsPublic)
            throw new InvalidOperationException("AndroidJavaCrypto helper type is not public in APK");

        var sha1TryHashDataHelper = FindHelper(
            helperType,
            new("Sha1TryHashData", "System.Boolean", "System.ReadOnlySpan`1<System.Byte>", "System.Span`1<System.Byte>", "System.Int32&")
        );
        var steamKit = ModuleDefinition.ReadModule(steamKitPath, reader);
        openedModules.Add(steamKit);
        AssertNoForbiddenCryptoCallsites(steamKit);
        AssertReferencesHelperAssembly(steamKit, helperType);
        AssertContainsAnyHelperCallsite(steamKit, helperType, 1);

        var webSocketClient = ModuleDefinition.ReadModule(webSocketClientPath, reader);
        openedModules.Add(webSocketClient);
        AssertNoForbiddenWebSocketClientCryptoCallsites(webSocketClient);
        AssertReferencesHelperAssembly(webSocketClient, helperType);
        AssertContainsHelperCallsite(webSocketClient, webSocketClient.ImportReference(sha1TryHashDataHelper), 1);
        Console.WriteLine("Verified APK Android crypto patches: SteamKit2.dll and System.Net.WebSockets.Client.dll reference AndroidJavaCrypto helpers and contain no forbidden crypto callsites.");
    }
    finally
    {
        foreach (var module in openedModules)
            module.Dispose();

        try
        {
            Directory.Delete(tempDir, true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: failed to delete APK crypto verification temp directory {tempDir}: {ex.Message}");
        }
    }
}

static string ExtractApkBclEntry(string apkPath, string fileName, string outputDirectory)
{
    using var archive = ZipFile.OpenRead(apkPath);
    var entryName = "assets/dotnet_bcl/" + fileName;
    var entry = archive.GetEntry(entryName)
        ?? throw new InvalidOperationException($"APK does not contain {entryName}");
    var outputPath = Path.Combine(outputDirectory, fileName);
    entry.ExtractToFile(outputPath, true);
    return outputPath;
}

static void AssertContainsHelperCallsite(
    ModuleDefinition module,
    MethodReference helper,
    int minimumCount
)
{
    var count = CountHelperCallsites(module, helper);

    if (count < minimumCount)
    {
        throw new InvalidOperationException(
            $"{module.Name} expected at least {minimumCount} callsite(s) to {helper.FullName}, found {count}"
        );
    }
}

static void AssertContainsAnyHelperCallsite(
    ModuleDefinition module,
    TypeDefinition helperType,
    int minimumCount
)
{
    var count = 0;
    foreach (var type in module.Types.SelectMany(WalkTypes))
    {
        foreach (var method in type.Methods)
        {
            if (!method.HasBody)
                continue;

            foreach (var instruction in method.Body.Instructions)
            {
                if (
                    IsCallInstruction(instruction) &&
                    instruction.Operand is MethodReference mr &&
                    string.Equals(mr.DeclaringType.FullName, helperType.FullName, StringComparison.Ordinal)
                )
                {
                    count++;
                }
            }
        }
    }

    if (count < minimumCount)
    {
        throw new InvalidOperationException(
            $"{module.Name} expected at least {minimumCount} AndroidJavaCrypto helper callsite(s), found {count}"
        );
    }
}

static void AssertStagedTogether(string targetAssemblyPath, string helperAssemblyPath)
{
    var targetDirectory = Path.GetDirectoryName(Path.GetFullPath(targetAssemblyPath));
    var helperDirectory = Path.GetDirectoryName(Path.GetFullPath(helperAssemblyPath));

    if (
        string.IsNullOrWhiteSpace(targetDirectory) ||
        string.IsNullOrWhiteSpace(helperDirectory) ||
        !string.Equals(targetDirectory, helperDirectory, StringComparison.OrdinalIgnoreCase)
    )
    {
        throw new InvalidOperationException(
            $"{Path.GetFileName(targetAssemblyPath)} and {Path.GetFileName(helperAssemblyPath)} must be staged in the same directory for Android runtime resolution"
        );
    }
}

static IReadOnlyList<CryptoPatchRule> CreateCryptoPatchRules(TypeDefinition helperType, ModuleDefinition targetModule)
{
    var ruleSpecs = new CryptoPatchRuleSpec[]
    {
        new(
            "rng",
            "System.Security.Cryptography.RandomNumberGenerator",
            new("GetBytes", "System.Byte[]", "System.Int32"),
            new("GetRandomBytes", "System.Byte[]", "System.Int32")
        ),
        new(
            "fill",
            "System.Security.Cryptography.RandomNumberGenerator",
            new("Fill", "System.Void", "System.Span`1<System.Byte>"),
            new("FillRandom", "System.Void", "System.Span`1<System.Byte>")
        ),
        new(
            "spki",
            "System.Security.Cryptography.AsymmetricAlgorithm",
            new("ImportSubjectPublicKeyInfo", "System.Void", "System.ReadOnlySpan`1<System.Byte>", "System.Int32&"),
            new("ImportSubjectPublicKeyInfo", "System.Void", "System.Security.Cryptography.AsymmetricAlgorithm", "System.ReadOnlySpan`1<System.Byte>", "System.Int32&")
        ),
        new(
            "parameters",
            "System.Security.Cryptography.RSA",
            new("ImportParameters", "System.Void", "System.Security.Cryptography.RSAParameters"),
            new("ImportParameters", "System.Void", "System.Security.Cryptography.RSA", "System.Security.Cryptography.RSAParameters")
        ),
        new(
            "rsaEncrypt",
            "System.Security.Cryptography.RSA",
            new("Encrypt", "System.Byte[]", "System.Byte[]", "System.Security.Cryptography.RSAEncryptionPadding"),
            new("RsaEncrypt", "System.Byte[]", "System.Security.Cryptography.RSA", "System.Byte[]", "System.Security.Cryptography.RSAEncryptionPadding")
        ),
        new(
            "createRsa",
            "System.Security.Cryptography.RSA",
            new("Create", "System.Security.Cryptography.RSA"),
            new("CreateRsa", "System.Security.Cryptography.RSA")
        ),
        new(
            "hmacSha1Bytes",
            "System.Security.Cryptography.HMACSHA1",
            new("HashData", "System.Byte[]", "System.Byte[]", "System.Byte[]"),
            new("HmacSha1HashData", "System.Byte[]", "System.Byte[]", "System.Byte[]")
        ),
        new(
            "hmacSha1Span",
            "System.Security.Cryptography.HMACSHA1",
            new("HashData", "System.Byte[]", "System.ReadOnlySpan`1<System.Byte>", "System.ReadOnlySpan`1<System.Byte>"),
            new("HmacSha1HashData", "System.Byte[]", "System.ReadOnlySpan`1<System.Byte>", "System.ReadOnlySpan`1<System.Byte>")
        ),
        new(
            "hmacSha1SpanDestination",
            "System.Security.Cryptography.HMACSHA1",
            new("HashData", "System.Int32", "System.ReadOnlySpan`1<System.Byte>", "System.ReadOnlySpan`1<System.Byte>", "System.Span`1<System.Byte>"),
            new("HmacSha1HashData", "System.Int32", "System.ReadOnlySpan`1<System.Byte>", "System.ReadOnlySpan`1<System.Byte>", "System.Span`1<System.Byte>")
        ),
        new(
            "sha1Bytes",
            "System.Security.Cryptography.SHA1",
            new("HashData", "System.Byte[]", "System.Byte[]"),
            new("Sha1HashData", "System.Byte[]", "System.Byte[]")
        ),
        new(
            "sha1TryHashData",
            "System.Security.Cryptography.SHA1",
            new(
                "TryHashData",
                "System.Boolean",
                "System.ReadOnlySpan`1<System.Byte>",
                "System.Span`1<System.Byte>",
                "System.Int32&"
            ),
            new(
                "Sha1TryHashData",
                "System.Boolean",
                "System.ReadOnlySpan`1<System.Byte>",
                "System.Span`1<System.Byte>",
                "System.Int32&"
            )
        ),
        new(
            "createAes",
            "System.Security.Cryptography.Aes",
            new("Create", "System.Security.Cryptography.Aes"),
            new("CreateAes", "System.Security.Cryptography.Aes")
        ),
        new(
            "aesEncryptEcb",
            null,
            new("EncryptEcb", "System.Int32", "System.ReadOnlySpan`1<System.Byte>", "System.Span`1<System.Byte>", "System.Security.Cryptography.PaddingMode"),
            new("AesEncryptEcb", "System.Int32", "System.Security.Cryptography.SymmetricAlgorithm", "System.ReadOnlySpan`1<System.Byte>", "System.Span`1<System.Byte>", "System.Security.Cryptography.PaddingMode")
        ),
        new(
            "aesEncryptCbc",
            null,
            new("EncryptCbc", "System.Int32", "System.ReadOnlySpan`1<System.Byte>", "System.ReadOnlySpan`1<System.Byte>", "System.Span`1<System.Byte>", "System.Security.Cryptography.PaddingMode"),
            new("AesEncryptCbc", "System.Int32", "System.Security.Cryptography.SymmetricAlgorithm", "System.ReadOnlySpan`1<System.Byte>", "System.ReadOnlySpan`1<System.Byte>", "System.Span`1<System.Byte>", "System.Security.Cryptography.PaddingMode")
        ),
        new(
            "aesDecryptEcb",
            null,
            new("DecryptEcb", "System.Int32", "System.ReadOnlySpan`1<System.Byte>", "System.Span`1<System.Byte>", "System.Security.Cryptography.PaddingMode"),
            new("AesDecryptEcb", "System.Int32", "System.Security.Cryptography.SymmetricAlgorithm", "System.ReadOnlySpan`1<System.Byte>", "System.Span`1<System.Byte>", "System.Security.Cryptography.PaddingMode")
        ),
        new(
            "aesDecryptCbcSpanDestination",
            null,
            new("DecryptCbc", "System.Int32", "System.ReadOnlySpan`1<System.Byte>", "System.ReadOnlySpan`1<System.Byte>", "System.Span`1<System.Byte>", "System.Security.Cryptography.PaddingMode"),
            new("AesDecryptCbc", "System.Int32", "System.Security.Cryptography.SymmetricAlgorithm", "System.ReadOnlySpan`1<System.Byte>", "System.ReadOnlySpan`1<System.Byte>", "System.Span`1<System.Byte>", "System.Security.Cryptography.PaddingMode")
        ),
        new(
            "aesDecryptCbc",
            null,
            new("DecryptCbc", "System.Byte[]", "System.ReadOnlySpan`1<System.Byte>", "System.ReadOnlySpan`1<System.Byte>", "System.Security.Cryptography.PaddingMode"),
            new("AesDecryptCbc", "System.Byte[]", "System.Security.Cryptography.SymmetricAlgorithm", "System.ReadOnlySpan`1<System.Byte>", "System.ReadOnlySpan`1<System.Byte>", "System.Security.Cryptography.PaddingMode")
        ),
    };

    return ruleSpecs
        .Select(spec => new CryptoPatchRule(
            spec.CountName,
            spec.DeclaringType,
            spec.Target,
            targetModule.ImportReference(FindHelper(helperType, spec.Helper))
        ))
        .ToArray();
}

static Dictionary<string, int> PatchCryptoCalls(ModuleDefinition module, IReadOnlyList<CryptoPatchRule> rules)
{
    var counts = rules.ToDictionary(rule => rule.CountName, _ => 0);

    foreach (var type in module.Types.SelectMany(WalkTypes))
    {
        foreach (var method in type.Methods)
        {
            if (!method.HasBody)
                continue;

            foreach (var instruction in method.Body.Instructions)
            {
                if (!IsCallInstruction(instruction) || instruction.Operand is not MethodReference methodReference)
                    continue;

                var matchingRule = rules.FirstOrDefault(rule => rule.Matches(methodReference));
                if (matchingRule is null)
                    continue;

                instruction.OpCode = OpCodes.Call;
                instruction.Operand = matchingRule.ImportedHelper;
                counts[matchingRule.CountName]++;
            }
        }
    }

    return counts;
}

static void AssertRequiredCryptoPatches(IReadOnlyDictionary<string, int> counts)
{
    RequirePatch(counts, "rng", "No RandomNumberGenerator.GetBytes(int) calls were patched");
    RequirePatch(counts, "fill", "No RandomNumberGenerator.Fill(Span<byte>) calls were patched");
    RequirePatch(counts, "spki", "No AsymmetricAlgorithm.ImportSubjectPublicKeyInfo calls were patched");
    RequirePatch(counts, "parameters", "No RSA.ImportParameters calls were patched");
    RequirePatch(counts, "rsaEncrypt", "No RSA.Encrypt calls were patched");
    RequirePatch(counts, "createRsa", "No RSA.Create() calls were patched");
    RequirePatch(counts, "sha1Bytes", "No SHA1.HashData(byte[]) calls were patched");
    RequirePatch(counts, "createAes", "No Aes.Create() calls were patched");
    RequirePatch(counts, "aesEncryptEcb", "No AES EncryptEcb calls were patched");
    RequirePatch(counts, "aesEncryptCbc", "No AES EncryptCbc calls were patched");
    RequirePatch(counts, "aesDecryptEcb", "No AES DecryptEcb calls were patched");

    if (Sum(counts, "hmacSha1Bytes", "hmacSha1Span", "hmacSha1SpanDestination") == 0)
        throw new InvalidOperationException("No HMACSHA1.HashData calls were patched");
    if (Sum(counts, "aesDecryptCbc", "aesDecryptCbcSpanDestination") == 0)
        throw new InvalidOperationException("No AES DecryptCbc calls were patched");
}

static string FormatCryptoPatchSummary(IReadOnlyDictionary<string, int> counts)
{
    var orderedNames = new[]
    {
        "rng",
        "fill",
        "spki",
        "parameters",
        "rsaEncrypt",
        "createRsa",
        "hmacSha1Bytes",
        "hmacSha1Span",
        "hmacSha1SpanDestination",
        "sha1Bytes",
        "sha1TryHashData",
        "createAes",
        "aesEncryptEcb",
        "aesEncryptCbc",
        "aesDecryptEcb",
        "aesDecryptCbcSpanDestination",
        "aesDecryptCbc"
    };

    return "Patched SteamKit2.dll: " + string.Join(", ", orderedNames.Select(name => $"{name}={counts[name]}"));
}

static void PatchWebSocketClientCrypto(string webSocketClientPath, TypeDefinition helperType, IAssemblyResolver resolver)
{
    var reader = new ReaderParameters { AssemblyResolver = resolver, ReadWrite = false };
    var webSocketClient = ModuleDefinition.ReadModule(webSocketClientPath, reader);
    var cryptoPatchRules = CreateWebSocketClientCryptoPatchRules(helperType, webSocketClient);
    var cryptoPatchCounts = PatchCryptoCalls(webSocketClient, cryptoPatchRules);
    AssertRequiredWebSocketClientCryptoPatches(cryptoPatchCounts);
    AssertNoForbiddenWebSocketClientCryptoCallsites(webSocketClient);
    AssertPatchedHelperCallsites(webSocketClient, cryptoPatchRules, cryptoPatchCounts);
    AssertReferencesHelperAssembly(webSocketClient, helperType);

    WriteModuleOverOriginal(webSocketClient, webSocketClientPath);
    Console.WriteLine(FormatWebSocketClientCryptoPatchSummary(cryptoPatchCounts));
}

static IReadOnlyList<CryptoPatchRule> CreateWebSocketClientCryptoPatchRules(
    TypeDefinition helperType,
    ModuleDefinition targetModule
)
{
    var ruleSpecs = new CryptoPatchRuleSpec[]
    {
        new(
            "sha1TryHashData",
            "System.Security.Cryptography.SHA1",
            new(
                "TryHashData",
                "System.Boolean",
                "System.ReadOnlySpan`1<System.Byte>",
                "System.Span`1<System.Byte>",
                "System.Int32&"
            ),
            new(
                "Sha1TryHashData",
                "System.Boolean",
                "System.ReadOnlySpan`1<System.Byte>",
                "System.Span`1<System.Byte>",
                "System.Int32&"
            )
        ),
    };

    return ruleSpecs
        .Select(spec => new CryptoPatchRule(
            spec.CountName,
            spec.DeclaringType,
            spec.Target,
            targetModule.ImportReference(FindHelper(helperType, spec.Helper))
        ))
        .ToArray();
}

static void AssertRequiredWebSocketClientCryptoPatches(IReadOnlyDictionary<string, int> counts)
{
    RequirePatch(
        counts,
        "sha1TryHashData",
        "No System.Net.WebSockets.Client.dll SHA1.TryHashData calls were patched"
    );
}

static string FormatWebSocketClientCryptoPatchSummary(IReadOnlyDictionary<string, int> counts)
{
    return "Patched System.Net.WebSockets.Client.dll: sha1TryHashData=" + counts["sha1TryHashData"];
}

static MethodDefinition FindHelper(TypeDefinition helperType, MethodSignature signature)
{
    var method = helperType.Methods.FirstOrDefault(signature.Matches)
        ?? throw new InvalidOperationException($"Could not find AndroidJavaCrypto.{signature.Name}");

    if (!method.IsPublic || !method.IsStatic)
        throw new InvalidOperationException($"AndroidJavaCrypto.{signature.Name} must be public static for patched external assemblies");

    return method;
}

static void RequirePatch(IReadOnlyDictionary<string, int> counts, string name, string message)
{
    if (counts[name] == 0)
        throw new InvalidOperationException(message);
}

static int Sum(IReadOnlyDictionary<string, int> counts, params string[] names)
{
    return names.Sum(name => counts[name]);
}

static void AssertPatchedHelperCallsites(
    ModuleDefinition module,
    IReadOnlyList<CryptoPatchRule> rules,
    IReadOnlyDictionary<string, int> expectedCounts
)
{
    foreach (var rule in rules)
    {
        var expectedCount = expectedCounts[rule.CountName];
        if (expectedCount == 0)
            continue;

        var actualCount = CountHelperCallsites(module, rule.ImportedHelper);
        if (actualCount < expectedCount)
        {
            throw new InvalidOperationException(
                $"{module.Name} expected at least {expectedCount} patched callsite(s) to {rule.ImportedHelper.FullName}, found {actualCount}"
            );
        }
    }
}

static int CountHelperCallsites(ModuleDefinition module, MethodReference helper)
{
    var count = 0;
    foreach (var type in module.Types.SelectMany(WalkTypes))
    {
        foreach (var method in type.Methods)
        {
            if (!method.HasBody)
                continue;

            foreach (var instruction in method.Body.Instructions)
            {
                if (
                    IsCallInstruction(instruction) &&
                    instruction.Operand is MethodReference mr &&
                    string.Equals(mr.FullName, helper.FullName, StringComparison.Ordinal)
                )
                {
                    count++;
                }
            }
        }
    }

    return count;
}

static bool IsCallInstruction(Instruction instruction)
{
    return instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt;
}

static void AssertReferencesHelperAssembly(ModuleDefinition targetModule, TypeDefinition helperType)
{
    var helperAssemblyName = helperType.Module.Assembly?.Name.Name
        ?? throw new InvalidOperationException("Could not resolve AndroidJavaCrypto helper assembly name");

    if (
        !targetModule.AssemblyReferences.Any(reference =>
            string.Equals(reference.Name, helperAssemblyName, StringComparison.Ordinal)
        )
    )
    {
        throw new InvalidOperationException(
            $"{targetModule.Name} does not reference {helperAssemblyName} after Android crypto patching"
        );
    }
}

static void WriteModuleOverOriginal(ModuleDefinition module, string path)
{
    var tmpPath = path + ".patched";
    module.Write(tmpPath);
    module.Dispose();
    File.Copy(tmpPath, path, true);
    File.Delete(tmpPath);
}

static IEnumerable<TypeDefinition> WalkTypes(TypeDefinition type)
{
    yield return type;
    foreach (var nested in type.NestedTypes.SelectMany(WalkTypes))
        yield return nested;
}

static void AssertNoForbiddenCryptoCallsites(ModuleDefinition module)
{
    var remaining = new Dictionary<string, int>();

    foreach (var type in module.Types.SelectMany(WalkTypes))
    {
        foreach (var method in type.Methods)
        {
            if (!method.HasBody)
                continue;

            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.Operand is not MethodReference mr)
                    continue;

                if (!IsForbiddenCryptoCallsite(mr))
                    continue;

                var signature =
                    $"{mr.DeclaringType.FullName}::{mr.Name}(" +
                    string.Join(",", mr.Parameters.Select(p => p.ParameterType.FullName)) +
                    $")->{mr.ReturnType.FullName}";
                remaining.TryGetValue(signature, out var count);
                remaining[signature] = count + 1;
            }
        }
    }

    if (remaining.Count > 0)
    {
        throw new InvalidOperationException(
            "Forbidden Android SteamKit crypto callsites remain after patching: " +
            string.Join("; ", remaining.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Value}x {kvp.Key}"))
        );
    }
}

static bool IsForbiddenCryptoCallsite(MethodReference mr)
{
    var typeName = mr.DeclaringType.FullName;
    var methodName = mr.Name;

    if (typeName == "System.Security.Cryptography.RandomNumberGenerator" &&
        (methodName == "GetBytes" || methodName == "Fill"))
    {
        return true;
    }

    if (typeName == "System.Security.Cryptography.AsymmetricAlgorithm" &&
        methodName == "ImportSubjectPublicKeyInfo")
    {
        return true;
    }

    if (typeName == "System.Security.Cryptography.RSA" &&
        (methodName == "Create" || methodName == "ImportParameters" || methodName == "Encrypt"))
    {
        return true;
    }

    if (typeName == "System.Security.Cryptography.HMACSHA1" && methodName == "HashData")
    {
        return true;
    }

    if (typeName == "System.Security.Cryptography.SHA1" &&
        (methodName == "HashData" || methodName == "TryHashData"))
    {
        return true;
    }

    if (typeName == "System.Security.Cryptography.Aes" && methodName == "Create")
    {
        return true;
    }

    if (typeName.StartsWith("System.Security.Cryptography.", StringComparison.Ordinal) &&
        (methodName == "EncryptEcb" ||
         methodName == "EncryptCbc" ||
         methodName == "DecryptEcb" ||
         methodName == "DecryptCbc"))
    {
        return true;
    }

    return false;
}

static void AssertNoForbiddenWebSocketClientCryptoCallsites(ModuleDefinition module)
{
    var remaining = new Dictionary<string, int>();

    foreach (var type in module.Types.SelectMany(WalkTypes))
    {
        foreach (var method in type.Methods)
        {
            if (!method.HasBody)
                continue;

            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.Operand is not MethodReference mr)
                    continue;

                if (!IsForbiddenWebSocketClientCryptoCallsite(mr))
                    continue;

                var signature =
                    $"{mr.DeclaringType.FullName}::{mr.Name}(" +
                    string.Join(",", mr.Parameters.Select(p => p.ParameterType.FullName)) +
                    $")->{mr.ReturnType.FullName}";
                remaining.TryGetValue(signature, out var count);
                remaining[signature] = count + 1;
            }
        }
    }

    if (remaining.Count > 0)
    {
        throw new InvalidOperationException(
            "Forbidden Android WebSocket client crypto callsites remain after patching: " +
            string.Join("; ", remaining.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Value}x {kvp.Key}"))
        );
    }
}

static bool IsForbiddenWebSocketClientCryptoCallsite(MethodReference mr)
{
    return mr.DeclaringType.FullName == "System.Security.Cryptography.SHA1" &&
        mr.Name == "TryHashData" &&
        mr.Parameters.Count == 3 &&
        mr.Parameters[0].ParameterType.FullName == "System.ReadOnlySpan`1<System.Byte>" &&
        mr.Parameters[1].ParameterType.FullName == "System.Span`1<System.Byte>" &&
        mr.Parameters[2].ParameterType.FullName == "System.Int32&" &&
        mr.ReturnType.FullName == "System.Boolean";
}

static void PatchGamePlatformUtil(string gamePath, IAssemblyResolver resolver)
{
    var reader = new ReaderParameters { AssemblyResolver = resolver, ReadWrite = false };
    var game = ModuleDefinition.ReadModule(gamePath, reader);
    var platformUtil = game.GetType("MegaCrit.Sts2.Core.Platform.PlatformUtil")
        ?? throw new InvalidOperationException("Could not find PlatformUtil in sts2.dll");
    var nullStrategy = game.Types
        .SelectMany(WalkTypes)
        .FirstOrDefault(t => t.Name == "NullPlatformUtilStrategy")
        ?? throw new InvalidOperationException("Could not find NullPlatformUtilStrategy in sts2.dll");
    var nullStrategyCtor = nullStrategy.Methods.FirstOrDefault(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0)
        ?? throw new InvalidOperationException("Could not find NullPlatformUtilStrategy constructor in sts2.dll");
    var objectCtor = game.ImportReference(typeof(object).GetConstructor(Type.EmptyTypes)!);
    var nullField = platformUtil.Fields.FirstOrDefault(f => f.Name == "_null")
        ?? throw new InvalidOperationException("Could not find PlatformUtil._null field in sts2.dll");
    var steamField = platformUtil.Fields.FirstOrDefault(f => f.Name == "_steam");

    ReplaceBody(nullStrategyCtor, il =>
    {
        il.Append(il.Create(OpCodes.Ldarg_0));
        il.Append(il.Create(OpCodes.Call, objectCtor));
        il.Append(il.Create(OpCodes.Ret));
    });

    var staticCtor = platformUtil.Methods.FirstOrDefault(m => m.IsConstructor && m.IsStatic)
        ?? throw new InvalidOperationException("Could not find PlatformUtil static constructor in sts2.dll");
    ReplaceBody(staticCtor, il =>
    {
        il.Append(il.Create(OpCodes.Newobj, nullStrategyCtor));
        il.Append(il.Create(OpCodes.Stsfld, nullField));
        if (steamField != null)
        {
            il.Append(il.Create(OpCodes.Ldnull));
            il.Append(il.Create(OpCodes.Stsfld, steamField));
        }
        il.Append(il.Create(OpCodes.Ret));
    });

    var primaryPlatform = platformUtil.Methods.FirstOrDefault(m => m.Name == "get_PrimaryPlatform" && m.Parameters.Count == 0)
        ?? throw new InvalidOperationException("Could not find PlatformUtil.PrimaryPlatform getter in sts2.dll");
    ReplaceBody(primaryPlatform, il =>
    {
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ret));
    });

    var getPlatformUtil = platformUtil.Methods.FirstOrDefault(m => m.Name == "GetPlatformUtil" && m.Parameters.Count == 1)
        ?? throw new InvalidOperationException("Could not find PlatformUtil.GetPlatformUtil in sts2.dll");
    ReplaceBody(getPlatformUtil, il =>
    {
        il.Append(il.Create(OpCodes.Ldsfld, nullField));
        il.Append(il.Create(OpCodes.Ret));
    });

    WriteModuleOverOriginal(game, gamePath);
    Console.WriteLine("Patched sts2.dll PlatformUtil: NullPlatformUtilStrategy constructor no-op, static constructor uses NullPlatformUtilStrategy, PrimaryPlatform=None, GetPlatformUtil returns _null");
}

static void ReplaceBody(MethodDefinition method, Action<ILProcessor> emit)
{
    if (!method.HasBody)
        method.Body = new MethodBody(method);

    method.Body.ExceptionHandlers.Clear();
    method.Body.Variables.Clear();
    method.Body.Instructions.Clear();
    method.Body.InitLocals = false;
    method.Body.MaxStackSize = 8;
    emit(method.Body.GetILProcessor());
}

sealed record MethodSignature(string Name, string ReturnType, params string[] ParameterTypes)
{
    public bool Matches(MethodReference method)
    {
        return method.Name == Name &&
            method.ReturnType.FullName == ReturnType &&
            method.Parameters.Count == ParameterTypes.Length &&
            method.Parameters.Select(parameter => parameter.ParameterType.FullName).SequenceEqual(ParameterTypes);
    }
}

sealed record CryptoPatchRuleSpec(
    string CountName,
    string? DeclaringType,
    MethodSignature Target,
    MethodSignature Helper
);

sealed record CryptoPatchRule(
    string CountName,
    string? DeclaringType,
    MethodSignature Target,
    MethodReference ImportedHelper
)
{
    public bool Matches(MethodReference method)
    {
        return (DeclaringType is null || method.DeclaringType.FullName == DeclaringType) && Target.Matches(method);
    }
}
