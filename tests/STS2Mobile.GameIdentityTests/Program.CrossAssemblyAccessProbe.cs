using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace STS2Mobile.GameIdentityTests;

internal static class CrossAssemblyAccessContract
{
    internal static readonly string[] Assemblies =
    {
        "AndroidAccessTarget", "AndroidAccessAllowed", "AndroidAccessMixedCase", "AndroidAccessDenied", "AndroidAccessWrongTarget", "AndroidAccessFriend"
    };
    private static readonly string[] Labels = { "Private", "Protected", "Internal", "PrivateProtected", "ProtectedInternal" };
    private static readonly Mono.Cecil.FieldAttributes[] FieldAccess =
    {
        Mono.Cecil.FieldAttributes.Private, Mono.Cecil.FieldAttributes.Family, Mono.Cecil.FieldAttributes.Assembly,
        Mono.Cecil.FieldAttributes.FamANDAssem, Mono.Cecil.FieldAttributes.FamORAssem
    };
    private static readonly int[] FieldValues = { 11, 13, 17, 19, 23 };
    private static readonly int[] MethodValues = { 31, 37, 41, 43, 47 };
    private static readonly int[] NestedValues = { 61, 67, 71, 73, 79 };

    internal static bool Run(Action<string> log, string directory)
    {
        var passed = true;
        var checks = 0;
        log("ASSEMBLY_ACCESS_BEGIN " + RuntimeInformation.FrameworkDescription);
        var target = Assembly.LoadFrom(Path.Combine(directory, Assemblies[0] + ".dll"));
        var vault = target.GetType("RuntimeAccessFixture.Vault", true)!;
        VerifyVisibility(vault);
        for (var callerIndex = 1; callerIndex < Assemblies.Length; callerIndex++)
        {
            var allowed = callerIndex is 1 or 2;
            var caller = Assembly.LoadFrom(Path.Combine(directory, Assemblies[callerIndex] + ".dll"));
            var bridge = caller.GetType("RuntimeAccessFixture.Bridge", true)!;
            var methods = bridge.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (methods.Length != 18) throw new Exception("The complete ordinary-member access matrix was not loaded.");
            foreach (var method in methods)
            {
                checks++;
                var expected = ExpectedValue(method.Name);
                var friendInternal = callerIndex == 5 && method.Name is "FieldInternal" or "MethodInternal" or "NestedInternal"
                    or "FieldProtectedInternal" or "MethodProtectedInternal" or "NestedProtectedInternal";
                var shouldAllow = allowed || method.Name == "PublicControl" || friendInternal;
                try
                {
                    var args = method.GetParameters().Length == 0 ? null : new object[] { 53 };
                    var actual = (int)method.Invoke(null, args)!;
                    var direct = args == null
                        ? ((Func<int>)method.CreateDelegate(typeof(Func<int>)))()
                        : ((Func<int, int>)method.CreateDelegate(typeof(Func<int, int>)))(53);
                    if (!shouldAllow || actual != expected || direct != expected)
                        throw new Exception($"Access result mismatch: allowed={shouldAllow} expected={expected} reflection={actual} delegate={direct}");
                    log($"ASSEMBLY_ACCESS {Assemblies[callerIndex]} {method.Name} allowed value={actual}");
                }
                catch (Exception error)
                {
                    while (error is TargetInvocationException { InnerException: not null } invocation)
                        error = invocation.InnerException!;
                    if (!shouldAllow && error is FieldAccessException or MethodAccessException or TypeAccessException)
                        log($"ASSEMBLY_ACCESS {Assemblies[callerIndex]} {method.Name} denied {error.GetType().Name}");
                    else
                    {
                        passed = false;
                        log($"ASSEMBLY_ACCESS_FAIL {Assemblies[callerIndex]} {method.Name} {error}");
                    }
                }
            }
            if (callerIndex == 3)
            {
                var derived = caller.GetType("RuntimeAccessFixture.Derived", true)!;
                foreach (var label in new[] { "Protected", "ProtectedInternal" })
                {
                    checks++;
                    var actual = (int)derived.GetMethod("Read" + label)!.Invoke(null, null)!;
                    if (actual != FieldValues[Array.IndexOf(Labels, label)])
                        throw new Exception("Legitimate family access was changed: " + label);
                }
                log("ASSEMBLY_ACCESS legitimate-derived-family-pass");
            }
        }
        VerifyVisibility(vault);
        if (checks != 92) throw new Exception("The complete assembly access contract was not exercised.");
        log("ASSEMBLY_ACCESS_END passed=" + passed + " checks=" + checks + " metadata-visibility=unchanged");
        return passed;
    }

    private static int ExpectedValue(string name)
    {
        if (name == "PublicControl") return 11;
        if (name is "WriteProtected" or "ConstructPrivate") return 53;
        var index = Array.FindIndex(Labels, label => name.EndsWith(label, StringComparison.Ordinal));
        // Match longer labels before their common suffix.
        if (name.EndsWith("PrivateProtected", StringComparison.Ordinal)) index = 3;
        if (name.EndsWith("ProtectedInternal", StringComparison.Ordinal)) index = 4;
        return name.StartsWith("Field", StringComparison.Ordinal) ? FieldValues[index]
            : name.StartsWith("Method", StringComparison.Ordinal) ? MethodValues[index] : NestedValues[index];
    }

    private static void VerifyVisibility(Type vault)
    {
        if (vault.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Length != 0
            || !vault.GetField("ProtectedField", BindingFlags.Instance | BindingFlags.NonPublic)!.IsFamily
            || !vault.GetField("PrivateField", BindingFlags.Instance | BindingFlags.NonPublic)!.IsPrivate
            || !vault.GetNestedType("ProtectedNested", BindingFlags.NonPublic)!.IsNestedFamily
            || vault.GetConstructor(new[] { typeof(int) }) != null)
            throw new Exception("Access policy must preserve the original metadata visibility.");
    }

    internal static void WriteFixtures(string directory)
    {
        Directory.CreateDirectory(directory);
        using var target = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(Assemblies[0], new Version(1, 0)), Assemblies[0], ModuleKind.Dll);
        var module = target.MainModule;
        var friend = new CustomAttribute(module.ImportReference(typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute).GetConstructor(new[] { typeof(string) })!));
        friend.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, Assemblies[5]));
        target.CustomAttributes.Add(friend);
        var vault = new TypeDefinition("RuntimeAccessFixture", "Vault", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.BeforeFieldInit, module.TypeSystem.Object);
        module.Types.Add(vault);
        var fields = Labels.Select((label, i) => new FieldDefinition(label + "Field", FieldAccess[i], module.TypeSystem.Int32)).ToArray();
        foreach (var field in fields) vault.Fields.Add(field);
        var constructor = AddMethod(vault, ".ctor", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName, module.TypeSystem.Void);
        var il = constructor.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, module.ImportReference(typeof(object).GetConstructor(Type.EmptyTypes)!));
        foreach (var (field, value) in fields.Zip(FieldValues)) { il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldc_I4, value); il.Emit(OpCodes.Stfld, field); }
        il.Emit(OpCodes.Ret);
        var publicRead = AddMethod(vault, "PublicRead", Mono.Cecil.MethodAttributes.Public, module.TypeSystem.Int32);
        il = publicRead.Body.GetILProcessor(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, fields[0]); il.Emit(OpCodes.Ret);
        var privateConstructor = AddMethod(vault, ".ctor", Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName, module.TypeSystem.Void, module.TypeSystem.Int32);
        il = privateConstructor.Body.GetILProcessor(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, constructor); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Stfld, fields[0]); il.Emit(OpCodes.Ret);
        var methods = Labels.Select((label, i) => AddMethod(vault, label + "Method", (Mono.Cecil.MethodAttributes)(int)FieldAccess[i], module.TypeSystem.Int32)).ToArray();
        for (var i = 0; i < methods.Length; i++) { il = methods[i].Body.GetILProcessor(); il.Emit(OpCodes.Ldc_I4, MethodValues[i]); il.Emit(OpCodes.Ret); }
        var nestedAccess = new[] { Mono.Cecil.TypeAttributes.NestedPrivate, Mono.Cecil.TypeAttributes.NestedFamily, Mono.Cecil.TypeAttributes.NestedAssembly, Mono.Cecil.TypeAttributes.NestedFamANDAssem, Mono.Cecil.TypeAttributes.NestedFamORAssem };
        var nested = new List<MethodDefinition>();
        for (var i = 0; i < Labels.Length; i++)
        {
            var type = new TypeDefinition("", Labels[i] + "Nested", nestedAccess[i] | Mono.Cecil.TypeAttributes.Abstract | Mono.Cecil.TypeAttributes.Sealed, module.TypeSystem.Object);
            vault.NestedTypes.Add(type);
            var read = AddMethod(type, "Read", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, module.TypeSystem.Int32);
            il = read.Body.GetILProcessor(); il.Emit(OpCodes.Ldc_I4, NestedValues[i]); il.Emit(OpCodes.Ret); nested.Add(read);
        }
        target.Write(Path.Combine(directory, Assemblies[0] + ".dll"));
        for (var callerIndex = 1; callerIndex < Assemblies.Length; callerIndex++)
        {
            using var caller = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(Assemblies[callerIndex], new Version(1, 0)), Assemblies[callerIndex], ModuleKind.Dll);
            var cm = caller.MainModule;
            if (callerIndex is 1 or 2 or 4) AddIgnoresAttribute(caller, callerIndex == 4 ? "UnrelatedAssembly" : callerIndex == 2 ? Assemblies[0].ToUpperInvariant() : Assemblies[0]);
            var bridge = new TypeDefinition("RuntimeAccessFixture", "Bridge", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Abstract | Mono.Cecil.TypeAttributes.Sealed, cm.TypeSystem.Object);
            cm.Types.Add(bridge);
            for (var i = 0; i < Labels.Length; i++)
            {
                var fieldRead = AddMethod(bridge, "Field" + Labels[i], Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, cm.TypeSystem.Int32);
                il = fieldRead.Body.GetILProcessor(); il.Emit(OpCodes.Newobj, cm.ImportReference(constructor)); il.Emit(OpCodes.Ldfld, cm.ImportReference(fields[i])); il.Emit(OpCodes.Ret);
                var methodRead = AddMethod(bridge, "Method" + Labels[i], Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, cm.TypeSystem.Int32);
                il = methodRead.Body.GetILProcessor(); il.Emit(OpCodes.Newobj, cm.ImportReference(constructor)); il.Emit(OpCodes.Call, cm.ImportReference(methods[i])); il.Emit(OpCodes.Ret);
                var nestedRead = AddMethod(bridge, "Nested" + Labels[i], Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, cm.TypeSystem.Int32);
                il = nestedRead.Body.GetILProcessor(); il.Emit(OpCodes.Call, cm.ImportReference(nested[i])); il.Emit(OpCodes.Ret);
            }
            var control = AddMethod(bridge, "PublicControl", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, cm.TypeSystem.Int32);
            il = control.Body.GetILProcessor(); il.Emit(OpCodes.Newobj, cm.ImportReference(constructor)); il.Emit(OpCodes.Call, cm.ImportReference(publicRead)); il.Emit(OpCodes.Ret);
            var write = AddMethod(bridge, "WriteProtected", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, cm.TypeSystem.Int32, cm.TypeSystem.Int32);
            il = write.Body.GetILProcessor(); il.Emit(OpCodes.Newobj, cm.ImportReference(constructor)); il.Emit(OpCodes.Dup); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Stfld, cm.ImportReference(fields[1])); il.Emit(OpCodes.Ldfld, cm.ImportReference(fields[1])); il.Emit(OpCodes.Ret);
            var construct = AddMethod(bridge, "ConstructPrivate", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, cm.TypeSystem.Int32, cm.TypeSystem.Int32);
            il = construct.Body.GetILProcessor(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Newobj, cm.ImportReference(privateConstructor)); il.Emit(OpCodes.Call, cm.ImportReference(publicRead)); il.Emit(OpCodes.Ret);
            if (callerIndex == 3)
            {
                var derived = new TypeDefinition("RuntimeAccessFixture", "Derived", Mono.Cecil.TypeAttributes.Public, cm.ImportReference(vault));
                cm.Types.Add(derived);
                var derivedCtor = AddMethod(derived, ".ctor", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName, cm.TypeSystem.Void);
                il = derivedCtor.Body.GetILProcessor(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, cm.ImportReference(constructor)); il.Emit(OpCodes.Ret);
                foreach (var i in new[] { 1, 4 })
                {
                    var read = AddMethod(derived, "Read" + Labels[i], Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, cm.TypeSystem.Int32);
                    il = read.Body.GetILProcessor(); il.Emit(OpCodes.Newobj, derivedCtor); il.Emit(OpCodes.Ldfld, cm.ImportReference(fields[i])); il.Emit(OpCodes.Ret);
                }
            }
            caller.Write(Path.Combine(directory, Assemblies[callerIndex] + ".dll"));
        }
    }

    private static MethodDefinition AddMethod(TypeDefinition owner, string name, Mono.Cecil.MethodAttributes access, TypeReference result, params TypeReference[] parameters)
    {
        var method = new MethodDefinition(name, access, result) { ImplAttributes = Mono.Cecil.MethodImplAttributes.NoInlining };
        foreach (var parameter in parameters) method.Parameters.Add(new ParameterDefinition(parameter));
        owner.Methods.Add(method);
        return method;
    }

    private static void AddIgnoresAttribute(AssemblyDefinition assembly, string target)
    {
        var module = assembly.MainModule;
        var attribute = new TypeDefinition("System.Runtime.CompilerServices", "IgnoresAccessChecksToAttribute", Mono.Cecil.TypeAttributes.NotPublic | Mono.Cecil.TypeAttributes.Sealed, module.ImportReference(typeof(Attribute)));
        module.Types.Add(attribute);
        var constructor = AddMethod(attribute, ".ctor", Mono.Cecil.MethodAttributes.Assembly | Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName, module.TypeSystem.Void, module.TypeSystem.String);
        var il = constructor.Body.GetILProcessor(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, module.ImportReference(typeof(Attribute).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null)!)); il.Emit(OpCodes.Ret);
        var usage = new CustomAttribute(module.ImportReference(typeof(AttributeUsageAttribute).GetConstructor(new[] { typeof(AttributeTargets) })!));
        usage.ConstructorArguments.Add(new CustomAttributeArgument(module.ImportReference(typeof(AttributeTargets)), (int)AttributeTargets.Assembly));
        usage.Properties.Add(new Mono.Cecil.CustomAttributeNamedArgument(nameof(AttributeUsageAttribute.AllowMultiple), new CustomAttributeArgument(module.TypeSystem.Boolean, true)));
        attribute.CustomAttributes.Add(usage);
        var ignore = new CustomAttribute(constructor);
        ignore.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, target));
        assembly.CustomAttributes.Add(ignore);
    }
}
