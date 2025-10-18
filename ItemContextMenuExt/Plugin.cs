using BepInEx;
using BepInEx.Configuration;
using EFT.UI;
using IcyClawz.CustomInteractions;
using SPT.Reflection.Patching;
using System.Reflection;

namespace IcyClawz.ItemContextMenuExt;

[BepInPlugin("com.IcyClawz.ItemContextMenuExt", "IcyClawz.ItemContextMenuExt", "1.7.0")]
[BepInDependency("com.IcyClawz.CustomInteractions")]
public class Plugin : BaseUnityPlugin
{
    private static ConfigEntry<float> ScaleConfig { get; set; }

    private void Awake()
    {
        const string SECTION = "Item Context Menu";
        ScaleConfig = Config.Bind(SECTION, "Scale", 1f);
        new ItemUiContextPatch().Enable();
        CustomInteractionsManager.Register(new CustomInteractionsProvider());
    }

    internal static float Scale => ScaleConfig.Value;
}

internal class ItemUiContextPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(ItemUiContext).GetMethod("ShowContextMenu", BindingFlags.Public | BindingFlags.Instance);

    [PatchPostfix]
    private static void Postfix(ref ItemUiContext __instance) =>
        __instance.ContextMenu.Transform.localScale = new(Plugin.Scale, Plugin.Scale);
}
