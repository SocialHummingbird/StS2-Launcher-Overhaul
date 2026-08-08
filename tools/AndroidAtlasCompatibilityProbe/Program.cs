using System.Reflection;
using HarmonyLib;
using Mono.Cecil;

if (args.Length != 3)
{
    Console.Error.WriteLine(
        "Usage: AndroidAtlasCompatibilityProbe <STS2Mobile.dll> <reporter sts2.dll> <reporter GodotSharp.dll>"
    );
    return 2;
}

string patchAssemblyPath = Path.GetFullPath(args[0]);
string gameAssemblyPath = Path.GetFullPath(args[1]);
string godotAssemblyPath = Path.GetFullPath(args[2]);
RequireFile(patchAssemblyPath);
RequireFile(gameAssemblyPath);
RequireFile(godotAssemblyPath);

var godotAssembly = Assembly.LoadFrom(godotAssemblyPath);
var gameAssembly = Assembly.LoadFrom(gameAssemblyPath);
var patchAssembly = Assembly.LoadFrom(patchAssemblyPath);

const string harmonyId = "com.sts2launcher.android-atlas-compatibility-probe";
var harmony = new Harmony(harmonyId);
try
{
    AssertStandardModPackBoundary(gameAssemblyPath);
    var patchType = patchAssembly.GetType(
        "STS2Mobile.Patches.AndroidAtlasCompatibilityPatches",
        throwOnError: true
    )!;
    AssertFallbackPolicy(patchType);
    AttachAndAssertPrefix(
        harmony,
        patchType,
        "LoadAllAtlasesPrefix",
        gameAssembly,
        "MegaCrit.Sts2.Core.Assets.AtlasManager",
        "LoadAllAtlases",
        0,
        harmonyId
    );
    AttachAndAssertPrefix(
        harmony,
        patchType,
        "AtlasResourceExistsPrefix",
        gameAssembly,
        "MegaCrit.Sts2.Core.Assets.AtlasResourceLoader",
        "_Exists",
        1,
        harmonyId
    );
    AttachAndAssertPrefix(
        harmony,
        patchType,
        "AtlasResourceLoadPrefix",
        gameAssembly,
        "MegaCrit.Sts2.Core.Assets.AtlasResourceLoader",
        "_Load",
        4,
        harmonyId
    );
    AttachAndAssertPostfix(
        harmony,
        patchType,
        "LoadResourcePackPostfix",
        godotAssembly,
        "Godot.ProjectSettings",
        "LoadResourcePack",
        3,
        harmonyId
    );

    Console.WriteLine(
        "PASS: Android atlas compatibility prefixes and global resource-pack postfix attached to the reporter runtime"
    );
    return 0;
}
finally
{
    harmony.UnpatchAll(harmonyId);
}

static void RequireFile(string path)
{
    if (!File.Exists(path))
        throw new FileNotFoundException("Required probe input is missing", path);
}

static void AssertFallbackPolicy(Type patchType)
{
    var parse = patchType.GetMethod(
        "TryParseSpritePath",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
    ) ?? throw new MissingMethodException(patchType.FullName, "TryParseSpritePath");
    object?[] parseArguments =
    {
        "res://images/atlases/card_atlas.sprites/colorless/alchemize.tres",
        null,
        null,
    };
    bool parsed = (bool)(parse.Invoke(null, parseArguments) ?? false);
    if (!parsed
        || !string.Equals(parseArguments[1] as string, "card_atlas", StringComparison.Ordinal)
        || !string.Equals(parseArguments[2] as string, "colorless/alchemize", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("Compiled atlas sprite path parser returned the wrong result");
    }

    var candidatesMethod = patchType.GetMethod(
        "FallbackCandidates",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
    ) ?? throw new MissingMethodException(patchType.FullName, "FallbackCandidates");
    var candidates = (IEnumerable<string>)(
        candidatesMethod.Invoke(null, new object[] { "card_atlas", "colorless/alchemize" })
        ?? throw new InvalidOperationException("Compiled fallback policy returned null")
    );
    string[] actual = candidates.ToArray();
    string[] expected =
    {
        "res://images/packed/card_portraits/colorless/alchemize.png",
        "res://images/packed/card_portraits/colorless/beta/alchemize.png",
    };
    if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
    {
        throw new InvalidOperationException(
            "Compiled card fallback mapping mismatch: " + string.Join(", ", actual)
        );
    }

    Console.WriteLine("PASS: compiled atlas path parser and card fallback mapping");
}

static void AssertStandardModPackBoundary(string gameAssemblyPath)
{
    using var definition = AssemblyDefinition.ReadAssembly(gameAssemblyPath);
    var modManager = definition.MainModule.GetType("MegaCrit.Sts2.Core.Modding.ModManager")
        ?? throw new TypeLoadException("MegaCrit.Sts2.Core.Modding.ModManager");
    var callers = modManager.Methods
        .Where(method => method.HasBody)
        .Where(method => method.Body.Instructions.Any(instruction =>
            instruction.Operand is MethodReference called
            && called.DeclaringType.FullName == "Godot.ProjectSettings"
            && called.Name == "LoadResourcePack"
        ))
        .Select(method => method.Name)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(name => name, StringComparer.Ordinal)
        .ToArray();
    if (callers.Length == 0)
    {
        throw new InvalidOperationException(
            "Standard ModManager does not call Godot.ProjectSettings.LoadResourcePack"
        );
    }

    Console.WriteLine(
        "PASS: standard ModManager resource packs cross the global Godot boundary via "
        + string.Join(", ", callers)
    );
}

static void AttachAndAssertPrefix(
    Harmony harmony,
    Type patchType,
    string prefixName,
    Assembly assembly,
    string typeName,
    string methodName,
    int parameterCount,
    string harmonyId
)
{
    var prefix = patchType.GetMethod(
        prefixName,
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
    ) ?? throw new MissingMethodException(patchType.FullName, prefixName);
    var type = assembly.GetType(typeName, throwOnError: true)!;
    var methods = type.GetMethods(
        BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
    ).Where(method => method.Name == methodName && method.GetParameters().Length == parameterCount)
        .ToArray();
    if (methods.Length != 1)
    {
        throw new InvalidOperationException(
            $"Expected one {typeName}.{methodName}/{parameterCount}; found {methods.Length}"
        );
    }

    harmony.Patch(methods[0], prefix: new HarmonyMethod(prefix));
    var patchInfo = Harmony.GetPatchInfo(methods[0]);
    bool attached = patchInfo?.Prefixes.Any(prefix => prefix.owner == harmonyId) == true;
    if (!attached)
        throw new InvalidOperationException($"Harmony prefix was not attached to {typeName}.{methodName}");

    Console.WriteLine($"PASS: prefix attached to {typeName}.{methodName}");
}

static void AttachAndAssertPostfix(
    Harmony harmony,
    Type patchType,
    string postfixName,
    Assembly assembly,
    string typeName,
    string methodName,
    int parameterCount,
    string harmonyId
)
{
    var postfix = patchType.GetMethod(
        postfixName,
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
    ) ?? throw new MissingMethodException(patchType.FullName, postfixName);
    var type = assembly.GetType(typeName, throwOnError: true)!;
    var methods = type.GetMethods(
        BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
    ).Where(method => method.Name == methodName && method.GetParameters().Length == parameterCount)
        .ToArray();
    if (methods.Length != 1)
    {
        throw new InvalidOperationException(
            $"Expected one {typeName}.{methodName}/{parameterCount}; found {methods.Length}"
        );
    }

    harmony.Patch(methods[0], postfix: new HarmonyMethod(postfix));
    var patchInfo = Harmony.GetPatchInfo(methods[0]);
    bool attached = patchInfo?.Postfixes.Any(postfix => postfix.owner == harmonyId) == true;
    if (!attached)
        throw new InvalidOperationException($"Harmony postfix was not attached to {typeName}.{methodName}");

    Console.WriteLine($"PASS: postfix attached to {typeName}.{methodName}");
}
