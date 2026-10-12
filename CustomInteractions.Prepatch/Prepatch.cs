using System.Collections.Generic;
using System.Linq;
using System;
using Mono.Cecil;

namespace IcyClawz.CustomInteractions;

public static class Prepatch
{
    public static IEnumerable<string> TargetDLLs => ["Assembly-CSharp.dll"];

    public static void Patch(AssemblyDefinition assembly)
    {
        TypeDefinition type = assembly.MainModule.GetType("EFT.UI.DynamicContextInteraction")
            ?? throw new InvalidOperationException("CustomInteractions requires the SPT 4.1 client.");
        FieldDefinition field = type.Fields.Single(c => c.Name is "_callback");
        // SPT 4.1 already exposes this field. Preserve public access for other mods.
        field.IsPublic = true;
        field.IsInitOnly = false;
    }
}
