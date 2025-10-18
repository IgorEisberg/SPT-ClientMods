using BepInEx;
using EFT.UI;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace IcyClawz.CustomInteractions;

[BepInPlugin("com.IcyClawz.CustomInteractions", "IcyClawz.CustomInteractions", "1.6.0")]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        new ItemUiContextPatch().Enable();
        new InteractionButtonsContainerPatch().Enable();
    }
}

internal class ItemUiContextPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(ItemUiContext).GetMethod("GetItemContextInteractions", BindingFlags.Public | BindingFlags.Instance);

    [PatchPostfix]
    private static void Postfix(ref ItemInfoInteractionsAbstractClass<EFT.InventoryLogic.EItemInfoButton> __result,
        ref ItemUiContext __instance, ItemContextClass itemContext)
    {
        foreach (var provider in CustomInteractionsManager.Providers)
        {
            var interactions = provider.GetCustomInteractions(__instance, itemContext.ViewType, itemContext.Item);
            if (interactions is null)
                continue;
            foreach (CustomInteractionImpl impl in interactions.Select(interaction => interaction.Impl))
                __result.AddCustomInteraction(impl);
        }
    }
}

internal class InteractionButtonsContainerPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(InteractionButtonsContainer).GetMethod("method_3", BindingFlags.Public | BindingFlags.Instance);

    [PatchPrefix]
    private static bool Prefix(ref InteractionButtonsContainer __instance, DynamicInteractionClass interaction)
    {
        if (interaction is CustomInteractionImpl impl)
        {
            __instance.AddCustomButton(impl);
            return false;
        }
        return true;
    }
}
