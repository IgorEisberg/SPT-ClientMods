using Comfort.Common;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace IcyClawz.MunitionsExpert;

internal enum ColorName
{
    Blue, Brown, Cyan, Gray, Green, Magenta, Orange, Pink, Purple, Red, Silver, Tan, Violet, Yellow
}

internal static class ColorCache
{
    private const byte ALPHA = 38;
    private static readonly Dictionary<ColorName, Color> Cache = new()
    {
        [ColorName.Blue] = new Color32(0, 60, 170, ALPHA),
        [ColorName.Brown] = new Color32(140, 85, 30, ALPHA),
        [ColorName.Cyan] = new Color32(0, 150, 150, ALPHA),
        [ColorName.Gray] = new Color32(70, 70, 70, ALPHA),
        [ColorName.Green] = new Color32(70, 140, 0, ALPHA),
        [ColorName.Magenta] = new Color32(215, 0, 100, ALPHA),
        [ColorName.Orange] = new Color32(140, 70, 0, ALPHA),
        [ColorName.Pink] = new Color32(215, 120, 150, ALPHA),
        [ColorName.Purple] = new Color32(120, 40, 135, ALPHA),
        [ColorName.Red] = new Color32(170, 20, 0, ALPHA),
        [ColorName.Silver] = new Color32(150, 150, 150, ALPHA),
        [ColorName.Tan] = new Color32(175, 145, 100, ALPHA),
        [ColorName.Violet] = new Color32(80, 50, 180, ALPHA),
        [ColorName.Yellow] = new Color32(170, 170, 0, ALPHA),
    };

    public static Color Get(ColorName name) => Cache.TryGetValue(name, out Color color) ? color : default;
}

internal enum EAmmoExtraAttributeId
{
    ArmorDamage, FragmentationChance, RicochetChance
}

internal static class ImageExtensions
{
    public static Sprite ToSprite(this System.Drawing.Image instance)
    {
        using MemoryStream ms = new();
        instance.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        Texture2D texture = new(instance.Width, instance.Height, TextureFormat.RGBA32, false);
        texture.LoadImage(ms.ToArray());
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
    }
}

internal static class IconCache
{
    private static readonly Dictionary<Enum, Sprite> Cache = new()
    {
        [EAmmoExtraAttributeId.ArmorDamage] = Properties.Resources.ArmorDamage.ToSprite(),
        [EAmmoExtraAttributeId.FragmentationChance] = Properties.Resources.FragmentationChance.ToSprite(),
        [EAmmoExtraAttributeId.RicochetChance] = Properties.Resources.RicochetChance.ToSprite(),
    };

    public static Sprite Get(Enum id) => Cache.TryGetValue(id, out Sprite sprite) ? sprite : default;
}

internal static class AmmoTemplateExtensions
{
    private static readonly string[] MalfChancesKeys = (string[])
        typeof(AmmoTemplate).GetField("MalfChancesKeys", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

    private static readonly FieldInfo MaxMalfMisfireChance =
        typeof(AmmoTemplate).GetField("MaxMalfMisfireChance", BindingFlags.NonPublic | BindingFlags.Static);

    private static readonly FieldInfo MaxMalfFeedChance =
        typeof(AmmoTemplate).GetField("MaxMalfFeedChance", BindingFlags.NonPublic | BindingFlags.Static);

    private static readonly FieldInfo CachedQualitiesField =
        typeof(AmmoTemplate).GetField("_cachedQualities", BindingFlags.NonPublic | BindingFlags.Instance);

    public static List<ItemAttributeClass> GetCachedQualities(this AmmoTemplate instance) =>
        CachedQualitiesField.GetValue(instance) as List<ItemAttributeClass>;

    public static void AddExtraAttributes(this AmmoTemplate instance)
    {
        instance.SafelyAddQualityToList(new ItemAttributeClass(EAmmoExtraAttributeId.ArmorDamage)
        {
            Name = EAmmoExtraAttributeId.ArmorDamage.ToString(),
            DisplayNameFunc = () => "Armor damage",
            Base = () => instance.ArmorDamage,
            StringValue = () => $"{instance.ArmorDamage}%",
            DisplayType = () => EItemAttributeDisplayType.Compact,
        });

        instance.SafelyAddQualityToList(new ItemAttributeClass(EItemAttributeId.DurabilityBurn)
        {
            Name = EItemAttributeId.DurabilityBurn.GetName(),
            Base = () => instance.DurabilityBurnModificator - 1f,
            StringValue = () => $"{(instance.DurabilityBurnModificator - 1f) * 100f:F1}%",
            DisplayType = () => EItemAttributeDisplayType.Compact,
            LabelVariations = EItemAttributeLabelVariations.Colored,
            LessIsGood = true,
        });

        instance.SafelyAddQualityToList(new ItemAttributeClass(EItemAttributeId.HeatFactor)
        {
            Name = EItemAttributeId.HeatFactor.GetName(),
            Base = () => instance.HeatFactor - 1f,
            StringValue = () => $"{(instance.HeatFactor - 1f) * 100f:F1}%",
            DisplayType = () => EItemAttributeDisplayType.Compact,
            LabelVariations = EItemAttributeLabelVariations.Colored,
            LessIsGood = true,
        });

        instance.SafelyAddQualityToList(new ItemAttributeClass(EAmmoExtraAttributeId.FragmentationChance)
        {
            Name = EAmmoExtraAttributeId.FragmentationChance.ToString(),
            DisplayNameFunc = () => "Fragmentation chance",
            Base = () => instance.FragmentationChance,
            StringValue = () => $"{instance.FragmentationChance * 100f:F1}%",
            DisplayType = () => EItemAttributeDisplayType.Compact,
        });

        instance.SafelyAddQualityToList(new ItemAttributeClass(EAmmoExtraAttributeId.RicochetChance)
        {
            Name = EAmmoExtraAttributeId.RicochetChance.ToString(),
            DisplayNameFunc = () => "Ricochet chance",
            Base = () => instance.RicochetChance,
            StringValue = () => $"{(instance.RicochetChance * 100f):F1}%",
            DisplayType = () => EItemAttributeDisplayType.Compact,
        });

        instance.SafelyAddQualityToList(new ItemAttributeClass(EItemAttributeId.MalfMisfireChance)
        {
            Name = EItemAttributeId.MalfMisfireChance.GetName(),
            Base = () => instance.MalfMisfireChance,
            StringValue = () =>
            {
                float maxMalfMisfireChance = (float)MaxMalfMisfireChance.GetValue(null);
                int index = instance.MalfMisfireChance <= 0f ? 0
                    : instance.MalfMisfireChance < 3f * maxMalfMisfireChance / 7f ? 1
                    : instance.MalfMisfireChance < 4f * maxMalfMisfireChance / 7f ? 2
                    : instance.MalfMisfireChance < 5f * maxMalfMisfireChance / 7f ? 3
                    : instance.MalfMisfireChance < 6f * maxMalfMisfireChance / 7f ? 4
                    : 5;
                return MalfChancesKeys[index].Localized();
            },
            DisplayType = () => EItemAttributeDisplayType.Compact,
        });

        instance.SafelyAddQualityToList(new ItemAttributeClass(EItemAttributeId.MalfFeedChance)
        {
            Name = EItemAttributeId.MalfFeedChance.GetName(),
            Base = () => instance.MalfFeedChance,
            StringValue = () =>
            {
                float maxMalfFeedChance = (float)MaxMalfFeedChance.GetValue(null);
                int index = instance.MalfFeedChance <= 0f ? 0
                    : instance.MalfFeedChance < 1f * maxMalfFeedChance / 7f ? 1
                    : instance.MalfFeedChance < 3f * maxMalfFeedChance / 7f ? 2
                    : instance.MalfFeedChance < 5f * maxMalfFeedChance / 7f ? 3
                    : instance.MalfFeedChance < 6f * maxMalfFeedChance / 7f ? 4
                    : 5;
                return MalfChancesKeys[index].Localized();
            },
            DisplayType = () => EItemAttributeDisplayType.Compact,
        });
    }

    public static int GetPenetrationArmorClass(this AmmoTemplate instance)
    {
        var armorClasses = Singleton<BackendConfigSettingsClass>.Instance.Armor.ArmorClass;
        for (int i = armorClasses.Length - 1; i >= 0; i--)
            if (armorClasses[i].Resistance <= instance.PenetrationPower)
                return i;
        return 0;
    }
}
