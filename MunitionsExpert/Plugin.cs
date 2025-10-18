using BepInEx;
using BepInEx.Configuration;
using EFT.HandBook;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace IcyClawz.MunitionsExpert;

[BepInPlugin("com.IcyClawz.MunitionsExpert", "IcyClawz.MunitionsExpert", "1.6.0")]
public class Plugin : BaseUnityPlugin
{
    private static ConfigEntry<bool> ColorizeConfig { get; set; }
    private static ConfigEntry<ColorName>[] ArmorClassColorConfigs { get; set; }

    private void Awake()
    {
        const string SECTION = "Colorize Icon Backgrounds";
        ColorizeConfig = Config.Bind(SECTION, "", true, new ConfigurationManagerAttributes { Order = 7 });
        ArmorClassColorConfigs = [
            Config.Bind(SECTION, "Unarmored", ColorName.Purple, new ConfigurationManagerAttributes { Order = 6 }),
            Config.Bind(SECTION, "Class 1", ColorName.Blue, new ConfigurationManagerAttributes { Order = 5 }),
            Config.Bind(SECTION, "Class 2", ColorName.Cyan, new ConfigurationManagerAttributes { Order = 4 }),
            Config.Bind(SECTION, "Class 3", ColorName.Green, new ConfigurationManagerAttributes { Order = 3 }),
            Config.Bind(SECTION, "Class 4", ColorName.Yellow, new ConfigurationManagerAttributes { Order = 2 }),
            Config.Bind(SECTION, "Class 5", ColorName.Orange, new ConfigurationManagerAttributes { Order = 1 }),
            Config.Bind(SECTION, "Class 6+", ColorName.Red, new ConfigurationManagerAttributes { Order = 0 }),
        ];
        new StaticIconsPatch().Enable();
        new AmmoTemplatePatch().Enable();
        new ItemViewPatch().Enable();
        new EntityIconPatch().Enable();
    }

    internal static bool Colorize => ColorizeConfig.Value;

    internal static Color GetArmorClassColor(int index)
    {
        index = Mathf.Clamp(index, 0, ArmorClassColorConfigs.Length - 1);
        return ColorCache.Get(ArmorClassColorConfigs[index].Value);
    }
}

internal class StaticIconsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(StaticIcons).GetMethod("GetAttributeIcon", BindingFlags.Public | BindingFlags.Instance);

    [PatchPrefix]
    private static bool PatchPrefix(ref Sprite __result, Enum id) =>
        (__result = IconCache.Get(id)) is null;
}

internal class AmmoTemplatePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(AmmoTemplate).GetMethod("GetCachedReadonlyQualities", BindingFlags.Public | BindingFlags.Instance);

    [PatchPrefix]
    private static bool PatchPrefix(ref AmmoTemplate __instance) =>
        __instance.GetCachedQualities() is null;

    [PatchPostfix]
    private static void PatchPostfix(ref List<ItemAttributeClass> __result, ref AmmoTemplate __instance)
    {
        if (__result is null)
            __result = __instance.GetCachedQualities();
        else
            __instance.AddExtraAttributes();
    }
}

internal class ItemViewPatch : ModulePatch
{
    private static readonly FieldInfo BackgroundColorField =
        typeof(ItemView).GetField("BackgroundColor", BindingFlags.NonPublic | BindingFlags.Instance);

    protected override MethodBase GetTargetMethod() =>
        typeof(ItemView).GetMethod("UpdateColor", BindingFlags.Public | BindingFlags.Instance);

    [PatchPrefix]
    private static void PatchPrefix(ref ItemView __instance)
    {
        if (!Plugin.Colorize || __instance.Item is not AmmoItemClass ammo || ammo.PenetrationPower <= 0)
            return;
        int armorClass = ammo.AmmoTemplate.GetPenetrationArmorClass();
        BackgroundColorField.SetValue(__instance, Plugin.GetArmorClassColor(armorClass));
    }
}

internal class EntityIconPatch : ModulePatch
{
    private static readonly FieldInfo ColorPanelField =
        typeof(EntityIcon).GetField("_colorPanel", BindingFlags.NonPublic | BindingFlags.Instance);

    protected override MethodBase GetTargetMethod() =>
        typeof(EntityIcon).GetMethod("Show", BindingFlags.Public | BindingFlags.Instance);

    [PatchPostfix]
    private static void PatchPostfix(ref EntityIcon __instance, Item item)
    {
        if (!Plugin.Colorize || item is not AmmoItemClass ammo || ammo.PenetrationPower <= 0)
            return;
        if (ColorPanelField.GetValue(__instance) is not Image image)
            return;
        int armorClass = ammo.AmmoTemplate.GetPenetrationArmorClass();
        image.color = Plugin.GetArmorClassColor(armorClass);
    }
}
