using BepInEx;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System.Reflection;
using EFT.Trading;

namespace IcyClawz.ItemSellPrice;

[BepInPlugin("com.IcyClawz.ItemSellPrice", "IcyClawz.ItemSellPrice", "1.8.0")]
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
        typeof(Trader).GetConstructors()[0];

    [PatchPostfix]
    private static void PatchPostfix(ref Trader __instance) =>
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
        typeof(Ammo).GetConstructors()[0];

    [PatchPostfix]
    private static void PatchPostfix(ref Ammo __instance) =>
        __instance.AddTraderOfferAttribute();
}

internal class ThrowWeapItemPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(ThrowWeap).GetConstructors()[0];

    [PatchPostfix]
    private static void PatchPostfix(ref ThrowWeap __instance) =>
        __instance.AddTraderOfferAttribute();
}
