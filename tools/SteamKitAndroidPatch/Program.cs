using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length < 2 || args.Length > 3)
{
    Console.Error.WriteLine("Usage: SteamKitPatch <SteamKit2.dll> <STS2Mobile.dll> [sts2.dll]");
    Environment.Exit(2);
}

var steamKitPath = args[0];
var helperPath = args[1];
var gamePath = args.Length == 3 ? args[2] : null;

var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(steamKitPath))!);
resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(helperPath))!);
if (!string.IsNullOrWhiteSpace(gamePath))
    resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(gamePath))!);

var reader = new ReaderParameters { AssemblyResolver = resolver, ReadWrite = false };
var steamKit = ModuleDefinition.ReadModule(steamKitPath, reader);
var helper = ModuleDefinition.ReadModule(helperPath, reader);

var helperType = helper.GetType("STS2Mobile.Steam.AndroidJavaCrypto")
    ?? throw new InvalidOperationException("Could not find AndroidJavaCrypto helper type");

var randomHelper = FindHelper("GetRandomBytes", "System.Byte[]", "System.Int32");
var fillRandomHelper = FindHelper("FillRandom", "System.Void", "System.Span`1<System.Byte>");
var importSpkiHelper = FindHelper(
    "ImportSubjectPublicKeyInfo",
    "System.Void",
    "System.Security.Cryptography.AsymmetricAlgorithm",
    "System.ReadOnlySpan`1<System.Byte>",
    "System.Int32&"
);
var importParametersHelper = FindHelper(
    "ImportParameters",
    "System.Void",
    "System.Security.Cryptography.RSA",
    "System.Security.Cryptography.RSAParameters"
);
var rsaEncryptHelper = FindHelper(
    "RsaEncrypt",
    "System.Byte[]",
    "System.Security.Cryptography.RSA",
    "System.Byte[]",
    "System.Security.Cryptography.RSAEncryptionPadding"
);
var createRsaHelper = FindHelper(
    "CreateRsa",
    "System.Security.Cryptography.RSA"
);
var hmacSha1BytesHelper = FindHelper(
    "HmacSha1HashData",
    "System.Byte[]",
    "System.Byte[]",
    "System.Byte[]"
);
var hmacSha1SpanHelper = FindHelper(
    "HmacSha1HashData",
    "System.Byte[]",
    "System.ReadOnlySpan`1<System.Byte>",
    "System.ReadOnlySpan`1<System.Byte>"
);
var hmacSha1SpanDestinationHelper = FindHelper(
    "HmacSha1HashData",
    "System.Int32",
    "System.ReadOnlySpan`1<System.Byte>",
    "System.ReadOnlySpan`1<System.Byte>",
    "System.Span`1<System.Byte>"
);
var sha1BytesHelper = FindHelper(
    "Sha1HashData",
    "System.Byte[]",
    "System.Byte[]"
);
var createAesHelper = FindHelper(
    "CreateAes",
    "System.Security.Cryptography.Aes"
);
var aesEncryptEcbHelper = FindHelper(
    "AesEncryptEcb",
    "System.Int32",
    "System.Security.Cryptography.SymmetricAlgorithm",
    "System.ReadOnlySpan`1<System.Byte>",
    "System.Span`1<System.Byte>",
    "System.Security.Cryptography.PaddingMode"
);
var aesEncryptCbcHelper = FindHelper(
    "AesEncryptCbc",
    "System.Int32",
    "System.Security.Cryptography.SymmetricAlgorithm",
    "System.ReadOnlySpan`1<System.Byte>",
    "System.ReadOnlySpan`1<System.Byte>",
    "System.Span`1<System.Byte>",
    "System.Security.Cryptography.PaddingMode"
);
var aesDecryptEcbHelper = FindHelper(
    "AesDecryptEcb",
    "System.Int32",
    "System.Security.Cryptography.SymmetricAlgorithm",
    "System.ReadOnlySpan`1<System.Byte>",
    "System.Span`1<System.Byte>",
    "System.Security.Cryptography.PaddingMode"
);
var aesDecryptCbcSpanDestinationHelper = FindHelper(
    "AesDecryptCbc",
    "System.Int32",
    "System.Security.Cryptography.SymmetricAlgorithm",
    "System.ReadOnlySpan`1<System.Byte>",
    "System.ReadOnlySpan`1<System.Byte>",
    "System.Span`1<System.Byte>",
    "System.Security.Cryptography.PaddingMode"
);
var aesDecryptCbcHelper = FindHelper(
    "AesDecryptCbc",
    "System.Byte[]",
    "System.Security.Cryptography.SymmetricAlgorithm",
    "System.ReadOnlySpan`1<System.Byte>",
    "System.ReadOnlySpan`1<System.Byte>",
    "System.Security.Cryptography.PaddingMode"
);

var importedRandomHelper = steamKit.ImportReference(randomHelper);
var importedFillRandomHelper = steamKit.ImportReference(fillRandomHelper);
var importedImportSpkiHelper = steamKit.ImportReference(importSpkiHelper);
var importedImportParametersHelper = steamKit.ImportReference(importParametersHelper);
var importedRsaEncryptHelper = steamKit.ImportReference(rsaEncryptHelper);
var importedCreateRsaHelper = steamKit.ImportReference(createRsaHelper);
var importedHmacSha1BytesHelper = steamKit.ImportReference(hmacSha1BytesHelper);
var importedHmacSha1SpanHelper = steamKit.ImportReference(hmacSha1SpanHelper);
var importedHmacSha1SpanDestinationHelper = steamKit.ImportReference(hmacSha1SpanDestinationHelper);
var importedSha1BytesHelper = steamKit.ImportReference(sha1BytesHelper);
var importedCreateAesHelper = steamKit.ImportReference(createAesHelper);
var importedAesEncryptEcbHelper = steamKit.ImportReference(aesEncryptEcbHelper);
var importedAesEncryptCbcHelper = steamKit.ImportReference(aesEncryptCbcHelper);
var importedAesDecryptEcbHelper = steamKit.ImportReference(aesDecryptEcbHelper);
var importedAesDecryptCbcSpanDestinationHelper = steamKit.ImportReference(aesDecryptCbcSpanDestinationHelper);
var importedAesDecryptCbcHelper = steamKit.ImportReference(aesDecryptCbcHelper);

var randomPatched = 0;
var fillRandomPatched = 0;
var spkiPatched = 0;
var parametersPatched = 0;
var rsaEncryptPatched = 0;
var createRsaPatched = 0;
var hmacSha1BytesPatched = 0;
var hmacSha1SpanPatched = 0;
var hmacSha1SpanDestinationPatched = 0;
var sha1BytesPatched = 0;
var createAesPatched = 0;
var aesEncryptEcbPatched = 0;
var aesEncryptCbcPatched = 0;
var aesDecryptEcbPatched = 0;
var aesDecryptCbcSpanDestinationPatched = 0;
var aesDecryptCbcPatched = 0;

foreach (var type in steamKit.Types.SelectMany(WalkTypes))
{
    foreach (var method in type.Methods)
    {
        if (!method.HasBody)
            continue;

        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.Operand is not MethodReference mr)
                continue;

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                mr.DeclaringType.FullName == "System.Security.Cryptography.RandomNumberGenerator" &&
                mr.Name == "GetBytes" &&
                mr.Parameters.Count == 1 &&
                mr.Parameters[0].ParameterType.FullName == "System.Int32" &&
                mr.ReturnType.FullName == "System.Byte[]")
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = importedRandomHelper;
                randomPatched++;
                continue;
            }
            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                mr.DeclaringType.FullName == "System.Security.Cryptography.RandomNumberGenerator" &&
                mr.Name == "Fill" &&
                mr.Parameters.Count == 1 &&
                mr.Parameters[0].ParameterType.FullName == "System.Span`1<System.Byte>" &&
                mr.ReturnType.FullName == "System.Void")
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = importedFillRandomHelper;
                fillRandomPatched++;
                continue;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                mr.DeclaringType.FullName == "System.Security.Cryptography.AsymmetricAlgorithm" &&
                mr.Name == "ImportSubjectPublicKeyInfo" &&
                mr.Parameters.Count == 2 &&
                mr.Parameters[0].ParameterType.FullName == "System.ReadOnlySpan`1<System.Byte>" &&
                mr.Parameters[1].ParameterType.FullName == "System.Int32&" &&
                mr.ReturnType.FullName == "System.Void")
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = importedImportSpkiHelper;
                spkiPatched++;
                continue;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                mr.DeclaringType.FullName == "System.Security.Cryptography.RSA" &&
                mr.Name == "ImportParameters" &&
                mr.Parameters.Count == 1 &&
                mr.Parameters[0].ParameterType.FullName == "System.Security.Cryptography.RSAParameters" &&
                mr.ReturnType.FullName == "System.Void")
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = importedImportParametersHelper;
                parametersPatched++;
                continue;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                mr.DeclaringType.FullName == "System.Security.Cryptography.RSA" &&
                mr.Name == "Encrypt" &&
                mr.Parameters.Count == 2 &&
                mr.Parameters[0].ParameterType.FullName == "System.Byte[]" &&
                mr.Parameters[1].ParameterType.FullName == "System.Security.Cryptography.RSAEncryptionPadding" &&
                mr.ReturnType.FullName == "System.Byte[]")
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = importedRsaEncryptHelper;
                rsaEncryptPatched++;
                continue;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                mr.DeclaringType.FullName == "System.Security.Cryptography.RSA" &&
                mr.Name == "Create" &&
                mr.Parameters.Count == 0 &&
                mr.ReturnType.FullName == "System.Security.Cryptography.RSA")
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = importedCreateRsaHelper;
                createRsaPatched++;
                continue;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                mr.DeclaringType.FullName == "System.Security.Cryptography.HMACSHA1" &&
                mr.Name == "HashData" &&
                mr.Parameters.Count == 2 &&
                mr.Parameters[0].ParameterType.FullName == "System.Byte[]" &&
                mr.Parameters[1].ParameterType.FullName == "System.Byte[]" &&
                mr.ReturnType.FullName == "System.Byte[]")
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = importedHmacSha1BytesHelper;
                hmacSha1BytesPatched++;
                continue;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                mr.DeclaringType.FullName == "System.Security.Cryptography.HMACSHA1" &&
                mr.Name == "HashData" &&
                mr.Parameters.Count == 2 &&
                mr.Parameters[0].ParameterType.FullName == "System.ReadOnlySpan`1<System.Byte>" &&
                mr.Parameters[1].ParameterType.FullName == "System.ReadOnlySpan`1<System.Byte>" &&
                mr.ReturnType.FullName == "System.Byte[]")
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = importedHmacSha1SpanHelper;
                hmacSha1SpanPatched++;
                continue;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                mr.DeclaringType.FullName == "System.Security.Cryptography.HMACSHA1" &&
                mr.Name == "HashData" &&
                mr.Parameters.Count == 3 &&
                mr.Parameters[0].ParameterType.FullName == "System.ReadOnlySpan`1<System.Byte>" &&
                mr.Parameters[1].ParameterType.FullName == "System.ReadOnlySpan`1<System.Byte>" &&
                mr.Parameters[2].ParameterType.FullName == "System.Span`1<System.Byte>" &&
                mr.ReturnType.FullName == "System.Int32")
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = importedHmacSha1SpanDestinationHelper;
                hmacSha1SpanDestinationPatched++;
                continue;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                mr.DeclaringType.FullName == "System.Security.Cryptography.SHA1" &&
                mr.Name == "HashData" &&
                mr.Parameters.Count == 1 &&
                mr.Parameters[0].ParameterType.FullName == "System.Byte[]" &&
                mr.ReturnType.FullName == "System.Byte[]")
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = importedSha1BytesHelper;
                sha1BytesPatched++;
                continue;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                mr.DeclaringType.FullName == "System.Security.Cryptography.Aes" &&
                mr.Name == "Create" &&
                mr.Parameters.Count == 0 &&
                mr.ReturnType.FullName == "System.Security.Cryptography.Aes")
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = importedCreateAesHelper;
                createAesPatched++;
                continue;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                mr.Name == "EncryptEcb" &&
                mr.Parameters.Count == 3 &&
                mr.Parameters[0].ParameterType.FullName == "System.ReadOnlySpan`1<System.Byte>" &&
                mr.Parameters[1].ParameterType.FullName == "System.Span`1<System.Byte>" &&
                mr.Parameters[2].ParameterType.FullName == "System.Security.Cryptography.PaddingMode" &&
                mr.ReturnType.FullName == "System.Int32")
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = importedAesEncryptEcbHelper;
                aesEncryptEcbPatched++;
                continue;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                mr.Name == "EncryptCbc" &&
                mr.Parameters.Count == 4 &&
                mr.Parameters[0].ParameterType.FullName == "System.ReadOnlySpan`1<System.Byte>" &&
                mr.Parameters[1].ParameterType.FullName == "System.ReadOnlySpan`1<System.Byte>" &&
                mr.Parameters[2].ParameterType.FullName == "System.Span`1<System.Byte>" &&
                mr.Parameters[3].ParameterType.FullName == "System.Security.Cryptography.PaddingMode" &&
                mr.ReturnType.FullName == "System.Int32")
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = importedAesEncryptCbcHelper;
                aesEncryptCbcPatched++;
                continue;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                mr.Name == "DecryptEcb" &&
                mr.Parameters.Count == 3 &&
                mr.Parameters[0].ParameterType.FullName == "System.ReadOnlySpan`1<System.Byte>" &&
                mr.Parameters[1].ParameterType.FullName == "System.Span`1<System.Byte>" &&
                mr.Parameters[2].ParameterType.FullName == "System.Security.Cryptography.PaddingMode" &&
                mr.ReturnType.FullName == "System.Int32")
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = importedAesDecryptEcbHelper;
                aesDecryptEcbPatched++;
                continue;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                mr.Name == "DecryptCbc" &&
                mr.Parameters.Count == 4 &&
                mr.Parameters[0].ParameterType.FullName == "System.ReadOnlySpan`1<System.Byte>" &&
                mr.Parameters[1].ParameterType.FullName == "System.ReadOnlySpan`1<System.Byte>" &&
                mr.Parameters[2].ParameterType.FullName == "System.Span`1<System.Byte>" &&
                mr.Parameters[3].ParameterType.FullName == "System.Security.Cryptography.PaddingMode" &&
                mr.ReturnType.FullName == "System.Int32")
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = importedAesDecryptCbcSpanDestinationHelper;
                aesDecryptCbcSpanDestinationPatched++;
                continue;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                mr.Name == "DecryptCbc" &&
                mr.Parameters.Count == 3 &&
                mr.Parameters[0].ParameterType.FullName == "System.ReadOnlySpan`1<System.Byte>" &&
                mr.Parameters[1].ParameterType.FullName == "System.ReadOnlySpan`1<System.Byte>" &&
                mr.Parameters[2].ParameterType.FullName == "System.Security.Cryptography.PaddingMode" &&
                mr.ReturnType.FullName == "System.Byte[]")
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = importedAesDecryptCbcHelper;
                aesDecryptCbcPatched++;
            }
        }
    }
}

if (randomPatched == 0)
    throw new InvalidOperationException("No RandomNumberGenerator.GetBytes(int) calls were patched");
if (fillRandomPatched == 0)
    throw new InvalidOperationException("No RandomNumberGenerator.Fill(Span<byte>) calls were patched");
if (spkiPatched == 0)
    throw new InvalidOperationException("No AsymmetricAlgorithm.ImportSubjectPublicKeyInfo calls were patched");
if (parametersPatched == 0)
    throw new InvalidOperationException("No RSA.ImportParameters calls were patched");
if (rsaEncryptPatched == 0)
    throw new InvalidOperationException("No RSA.Encrypt calls were patched");
if (createRsaPatched == 0)
    throw new InvalidOperationException("No RSA.Create() calls were patched");
if (hmacSha1BytesPatched + hmacSha1SpanPatched + hmacSha1SpanDestinationPatched == 0)
    throw new InvalidOperationException("No HMACSHA1.HashData calls were patched");
if (sha1BytesPatched == 0)
    throw new InvalidOperationException("No SHA1.HashData(byte[]) calls were patched");
if (createAesPatched == 0)
    throw new InvalidOperationException("No Aes.Create() calls were patched");
if (aesEncryptEcbPatched == 0)
    throw new InvalidOperationException("No AES EncryptEcb calls were patched");
if (aesEncryptCbcPatched == 0)
    throw new InvalidOperationException("No AES EncryptCbc calls were patched");
if (aesDecryptEcbPatched == 0)
    throw new InvalidOperationException("No AES DecryptEcb calls were patched");
if (aesDecryptCbcPatched + aesDecryptCbcSpanDestinationPatched == 0)
    throw new InvalidOperationException("No AES DecryptCbc calls were patched");

AssertNoForbiddenCryptoCallsites(steamKit);

var tmpPath = steamKitPath + ".patched";
steamKit.Write(tmpPath);
steamKit.Dispose();
helper.Dispose();
File.Copy(tmpPath, steamKitPath, true);
File.Delete(tmpPath);
Console.WriteLine($"Patched SteamKit2.dll: rng={randomPatched}, fill={fillRandomPatched}, spki={spkiPatched}, parameters={parametersPatched}, rsaEncrypt={rsaEncryptPatched}, createRsa={createRsaPatched}, hmacSha1Bytes={hmacSha1BytesPatched}, hmacSha1Span={hmacSha1SpanPatched}, hmacSha1SpanDestination={hmacSha1SpanDestinationPatched}, sha1Bytes={sha1BytesPatched}, createAes={createAesPatched}, aesEncryptEcb={aesEncryptEcbPatched}, aesEncryptCbc={aesEncryptCbcPatched}, aesDecryptEcb={aesDecryptEcbPatched}, aesDecryptCbcSpanDestination={aesDecryptCbcSpanDestinationPatched}, aesDecryptCbc={aesDecryptCbcPatched}");

if (!string.IsNullOrWhiteSpace(gamePath))
    PatchGamePlatformUtil(gamePath, resolver);

MethodDefinition FindHelper(string name, string returnType, params string[] parameters)
{
    return helperType.Methods.FirstOrDefault(m =>
        m.Name == name &&
        m.ReturnType.FullName == returnType &&
        m.Parameters.Count == parameters.Length &&
        m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters))
        ?? throw new InvalidOperationException($"Could not find AndroidJavaCrypto.{name}");
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

    if ((typeName == "System.Security.Cryptography.HMACSHA1" ||
         typeName == "System.Security.Cryptography.SHA1") &&
        methodName == "HashData")
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

    var tmpPath = gamePath + ".patched";
    game.Write(tmpPath);
    game.Dispose();
    File.Copy(tmpPath, gamePath, true);
    File.Delete(tmpPath);
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

