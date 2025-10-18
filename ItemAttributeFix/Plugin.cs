using BepInEx;
using EFT.UI;
using SPT.Reflection.Patching;
using System.Reflection;

namespace IcyClawz.ItemAttributeFix;

[BepInPlugin("com.IcyClawz.ItemAttributeFix", "IcyClawz.ItemAttributeFix", "1.5.0")]
public class Plugin : BaseUnityPlugin
{
    private void Awake() =>
        new CompactCharacteristicPanelPatch().Enable();
}

internal class CompactCharacteristicPanelPatch : ModulePatch
{
    private static readonly FieldInfo ItemAttributeField =
        typeof(CompactCharacteristicPanel).GetField("ItemAttribute", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo StringField =
        typeof(CompactCharacteristicPanel).GetField("string_0", BindingFlags.NonPublic | BindingFlags.Instance);

    protected override MethodBase GetTargetMethod() =>
        typeof(CompactCharacteristicPanel).GetMethod("SetValues", BindingFlags.Public | BindingFlags.Instance);
    
    [PatchPostfix]
    private static void PatchPostfix(ref CompactCharacteristicPanel __instance)
    {
        if (ItemAttributeField.GetValue(__instance) is ItemAttributeClass attribute)
            StringField.SetValue(__instance, attribute.FullStringValue());
    }
}
