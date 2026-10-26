using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Sts2Launcher.ModFrameworkFixtures;

[ModInitializer(nameof(Initialize))]
public static class EntryInitializer
{
    public const string HarmonyOwner = "fixture.owner.has.no.manifest.id";
    public static bool InitializerReturned;
    public static int InitializerCalls;
    public static string[] LoadedBeforeInitializer;
    public static object[] NativeObjects;
    public static readonly Dictionary<string, object> Registry = new();

    public static void Initialize()
    {
        InitializerCalls++;
        LoadedBeforeInitializer = ModManager.Mods.Where(mod => mod.state == ModLoadState.Loaded)
            .Select(mod => mod.manifest.id).ToArray();
        NativeObjects = ModManager.Mods.Cast<object>().ToArray();
        if (!LoadedBeforeInitializer.Contains("FixtureResources") || !LoadedBeforeInitializer.Contains("FixtureManual"))
            throw new InvalidOperationException("The original dependency order was not applied before the entry initializer.");
        Registry.Add("resources", Godot.FileAccess.GetFileAsString("res://FixtureResources/marker.txt"));
        Registry.Add("manual", Godot.FileAccess.GetFileAsString("res://FixtureManual/marker.txt"));
        var libraryRoot = ModManager.Mods.Single(mod => mod.manifest.id == "FixtureManual").path;
        var context = AssemblyLoadContext.GetLoadContext(typeof(EntryInitializer).Assembly)!;
        var secondary = context.LoadFromAssemblyPath(Path.Combine(libraryRoot, "nested", "FixtureSecondary.dll"));
        Registry.Add("secondary", secondary);
        var scene = ResourceLoader.Load<PackedScene>("res://FixtureEntry/early.tscn")
            ?? throw new InvalidOperationException("The combined mod's PackedScene did not load.");
        var instance = scene.Instantiate();
        if (instance.GetType().Assembly != secondary)
        {
            instance.Free();
            throw new InvalidOperationException("The nested DLL's C# script was not registered before the initializer used it.");
        }
        Registry.Add("instance", instance);
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(secondary);
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(secondary);
        Registry.Add("duplicate-lookups", 2);
        var harmony = new Harmony(HarmonyOwner);
        harmony.Patch(typeof(EntryInitializer).GetMethod(nameof(OriginalValue)),
            postfix: new HarmonyMethod(typeof(EntryInitializer).GetMethod(nameof(ValuePostfix), BindingFlags.Static | BindingFlags.NonPublic)));
        Registry.Add("patched-value", OriginalValue());
        InitializerReturned = true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int OriginalValue() => 17;
    private static void ValuePostfix(ref int __result) => __result += 15;
}
