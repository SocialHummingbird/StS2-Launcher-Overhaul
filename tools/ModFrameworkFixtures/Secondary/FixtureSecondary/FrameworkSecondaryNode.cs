using System;
using System.Linq;
using Godot;

namespace Sts2Launcher.ModFrameworkFixtures.Secondary;

public partial class FrameworkSecondaryNode : Node
{
    public static int ConstructorCalls;
    public static bool ConstructedBeforeInitializerReturned;

    public FrameworkSecondaryNode()
    {
        ConstructorCalls++;
        var entry = AppDomain.CurrentDomain.GetAssemblies().Single(assembly => assembly.GetName().Name == "FixtureEntry")
            .GetType("Sts2Launcher.ModFrameworkFixtures.EntryInitializer", throwOnError: true)!;
        ConstructedBeforeInitializerReturned = !(bool)entry.GetField("InitializerReturned")!.GetValue(null)!;
    }
}
