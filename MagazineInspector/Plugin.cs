using BepInEx;
using SPT.Reflection.Patching;
using System.Reflection;

namespace IcyClawz.MagazineInspector;

[BepInPlugin("com.IcyClawz.MagazineInspector", "IcyClawz.MagazineInspector", "1.7.1")]
public class Plugin : BaseUnityPlugin
{
    private void Awake() =>
        new MagazinePatch().Enable();
}

internal class MagazinePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(EFT.InventoryLogic.Magazine).GetConstructors()[0];

    [PatchPostfix]
    private static void PatchPostfix(ref EFT.InventoryLogic.Magazine __instance) =>
        __instance.AddAmmoCountAttribute();
}
