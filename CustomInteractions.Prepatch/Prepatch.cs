using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace IcyClawz.CustomInteractions;

public static class Prepatch
{
    public static IEnumerable<string> TargetDLLs => ["Assembly-CSharp.dll"];

    public static void Patch(AssemblyDefinition assembly)
    {
        TypeDefinition type = assembly.MainModule.GetType("DynamicInteractionClass");
        FieldDefinition field = type.Fields.SingleOrDefault(c => c.Name is "Action_0");
        field.IsFamily = true;
        field.IsInitOnly = false;
        MethodDefinition ctor = type.Methods.SingleOrDefault(c => c.Name is ".ctor");
        ParameterDefinition param = ctor.Parameters.SingleOrDefault(c => c.Name is "callback");
        param.IsOptional = true;
        param.HasDefault = true;
        param.Constant = null;
        //assembly.Write("Assembly-CSharp-CustomInteractions.dll");
    }
}
