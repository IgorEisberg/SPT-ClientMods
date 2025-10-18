using BepInEx;
using SPT.Reflection.Patching;
using System.Reflection;

namespace IcyClawz.MagazineInspector;

[BepInPlugin("com.IcyClawz.MagazineInspector", "IcyClawz.MagazineInspector", "1.6.0")]
public class Plugin : BaseUnityPlugin
{
    private void Awake() =>
        new MagazinePatch().Enable();
}

internal class MagazinePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(MagazineItemClass).GetConstructors()[0];

    [PatchPostfix]
    private static void PatchPostfix(ref MagazineItemClass __instance) =>
        __instance.AddAmmoCountAttribute();
}
