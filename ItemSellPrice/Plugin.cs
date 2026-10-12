using BepInEx;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System.Reflection;

namespace IcyClawz.ItemSellPrice;

[BepInPlugin("com.IcyClawz.ItemSellPrice", "IcyClawz.ItemSellPrice", "1.7.1")]
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
        typeof(EFT.Trading.Trader).GetConstructors()[0];

    [PatchPostfix]
    private static void PatchPostfix(ref EFT.Trading.Trader __instance) =>
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
        typeof(EFT.InventoryLogic.Ammo).GetConstructors()[0];

    [PatchPostfix]
    private static void PatchPostfix(ref EFT.InventoryLogic.Ammo __instance) =>
        __instance.AddTraderOfferAttribute();
}

internal class ThrowWeapItemPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(EFT.InventoryLogic.ThrowWeap).GetConstructors()[0];

    [PatchPostfix]
    private static void PatchPostfix(ref EFT.InventoryLogic.ThrowWeap __instance) =>
        __instance.AddTraderOfferAttribute();
}
