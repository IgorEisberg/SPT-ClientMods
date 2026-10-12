using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Generic;

internal static class Verify
{
    private static string Root;
    private static string Output;
    private static int Patches;
    private static int Bindings;
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name + ".dll";
        foreach (string folder in new[] { Output, Path.Combine(Root, "EscapeFromTarkov_Data/Managed"), Path.Combine(Root, "BepInEx/core"), Path.Combine(Root, "BepInEx/plugins/spt") })
        {
            string path = Path.Combine(folder, name);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    private static Type ValueType(Type type) { return type.IsByRef ? type.GetElementType() : type; }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    public static int Main(string[] args)
    {
        try
        {
            Root = Path.GetFullPath(args[0]);
            Output = Path.GetFullPath(args[1]);
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            foreach (string path in Directory.GetFiles(Output, "IcyClawz.*.dll"))
            {
                Assembly assembly = Assembly.LoadFrom(path);
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.ContainsGenericParameters) continue;
                    foreach (FieldInfo field in type.GetFields(All).Where(f => f.IsStatic && typeof(MemberInfo).IsAssignableFrom(f.FieldType)))
                    {
                        Require(field.GetValue(null) != null, "Unresolved reflection binding: " + type.FullName + "." + field.Name);
                        Bindings++;
                    }
                    MethodInfo targetGetter = type.GetMethod("GetTargetMethod", All | BindingFlags.DeclaredOnly);
                    if (targetGetter == null) continue;
                    MethodBase target = (MethodBase)targetGetter.Invoke(FormatterServices.GetUninitializedObject(type), null);
                    Require(target != null, "Missing target for " + type.FullName);
                    foreach (MethodInfo patch in type.GetMethods(All | BindingFlags.DeclaredOnly).Where(m => m.GetCustomAttributesData().Any(a => a.AttributeType.Name.StartsWith("Patch"))))
                    {
                        foreach (ParameterInfo parameter in patch.GetParameters())
                        {
                            Type expected;
                            if (parameter.Name == "__instance") expected = target.DeclaringType;
                            else if (parameter.Name == "__result") expected = ((MethodInfo)target).ReturnType;
                            else if (parameter.Name == "__state" || parameter.Name == "__runOriginal") continue;
                            else
                            {
                                ParameterInfo original = target.GetParameters().SingleOrDefault(p => p.Name == parameter.Name);
                                Require(original != null, "Missing argument " + parameter.Name + " in " + target);
                                expected = ValueType(original.ParameterType);
                            }
                            Require(ValueType(parameter.ParameterType) == expected, "Argument type mismatch: " + patch + " / " + parameter.Name);
                        }
                    }
                    Patches++;
                    Console.WriteLine("PASS target " + type.FullName + " -> " + target.DeclaringType.FullName + "." + target.Name);
                }
                if (assembly.GetName().Name == "IcyClawz.MunitionsExpert")
                {
                    foreach (string name in new[] { "ArmorDamage", "FragmentationChance", "RicochetChance" })
                    {
                        using (Stream stream = assembly.GetManifestResourceStream("IcyClawz.MunitionsExpert.Resources." + name + ".png"))
                        {
                            Require(stream != null && stream.Length > 8, "Missing icon " + name);
                            byte[] bytes = new byte[stream.Length];
                            stream.Read(bytes, 0, bytes.Length);
                            byte[] source = File.ReadAllBytes(Path.Combine(Output, "../MunitionsExpert/Resources/" + name + ".png"));
                            Require(bytes.SequenceEqual(source), "Incorrect icon content: " + name);
                        }
                    }
                    Console.WriteLine("PASS all three embedded PNG resources");
                }
            }
            Require(Patches == 13, "Unexpected patch count " + Patches);
            Console.WriteLine("PASS " + Patches + " patch targets and signatures; " + Bindings + " reflection bindings.");
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            ReflectionTypeLoadException load = e as ReflectionTypeLoadException;
            if (load != null) foreach (Exception inner in load.LoaderExceptions) Console.Error.WriteLine(inner);
            return 1;
        }
    }
}
