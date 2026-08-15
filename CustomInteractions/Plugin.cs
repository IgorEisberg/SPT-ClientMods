using BepInEx;
using EFT.InventoryLogic;
using EFT.UI;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace IcyClawz.CustomInteractions;

[BepInPlugin("com.IcyClawz.CustomInteractions", "IcyClawz.CustomInteractions", "1.8.1")]
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
    private static void Postfix(ref ContextInteractions<EItemInfoButton> __result,
        ref ItemUiContext __instance, ItemContext itemContext)
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

// SPT 4.1's client deobfuscation gave this method its real name (it was the obfuscated "method_3" before),
// which is what this whole mod's target-method lookup previously had to guess at across client updates.
internal class InteractionButtonsContainerPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(InteractionButtonsContainer).GetMethod("CreateDynamicContextButton", BindingFlags.Public | BindingFlags.Instance);

    [PatchPrefix]
    private static bool Prefix(ref InteractionButtonsContainer __instance, DynamicContextInteraction interaction)
    {
        if (interaction is CustomInteractionImpl impl)
        {
            __instance.AddCustomButton(impl);
            return false;
        }
        return true;
    }
}
