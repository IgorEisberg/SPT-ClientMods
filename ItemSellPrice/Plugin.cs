using BepInEx;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System.Reflection;

namespace IcyClawz.ItemSellPrice;

[BepInPlugin("com.IcyClawz.ItemSellPrice", "IcyClawz.ItemSellPrice", "1.7.0")]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        new TraderPatch().Enable();
        new ItemPatch().Enable();
        new AmmoItemPatch().Enable();
        new ThrowWeapItemPatch().Enable();
    }
}

internal class TraderPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(TraderClass).GetConstructors()[0];

    [PatchPostfix]
    private static void PatchPostfix(ref TraderClass __instance) =>
        __instance.UpdateSupplyData();
}

internal class ItemPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(Item).GetConstructors()[0];

    [PatchPostfix]
    private static void PatchPostfix(ref Item __instance) =>
        __instance.AddTraderOfferAttribute();
}

internal class AmmoItemPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(AmmoItemClass).GetConstructors()[0];

    [PatchPostfix]
    private static void PatchPostfix(ref AmmoItemClass __instance) =>
        __instance.AddTraderOfferAttribute();
}

internal class ThrowWeapItemPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(ThrowWeapItemClass).GetConstructors()[0];

    [PatchPostfix]
    private static void PatchPostfix(ref ThrowWeapItemClass __instance) =>
        __instance.AddTraderOfferAttribute();
}
