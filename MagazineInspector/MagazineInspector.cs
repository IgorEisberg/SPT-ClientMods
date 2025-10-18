using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using InGameStatus = GClass2064;

namespace IcyClawz.MagazineInspector;

internal static class MagazineClassExtensions
{
    private static readonly Dictionary<string, string> DisplayNames = new()
    {
        ["ch"] = "上膛弹药",
        ["cz"] = "Nabité střelivo",
        ["en"] = "Loaded ammo",
        ["es"] = "Munición cargada",
        ["es-mx"] = "Munición cargada",
        ["fr"] = "Munitions chargées",
        ["ge"] = "Geladene Munition",
        ["hu"] = "Töltött lőszer",
        ["it"] = "Munizioni caricate",
        ["jp"] = "装填弾薬",
        ["kr"] = "장전 탄약",
        ["pl"] = "Załadowana amunicja",
        ["po"] = "Munição carregada",
        ["ru"] = "Заряженные боеприпасы",
        ["sk"] = "Nabitá munícia",
        ["tu"] = "Yüklü mühimmat",
    };

    private static ISession _Session;
    private static ISession Session => _Session ??= ClientAppUtils.GetMainApp().GetClientBackEndSession();

    private static Profile ActiveProfile => InGameStatus.InRaid ? GamePlayerOwner.MyPlayer.Profile : Session.Profile;

    public static void AddAmmoCountAttribute(this MagazineItemClass magazine)
    {
        ItemAttributeClass attribute = magazine.Attributes.Find(attr => attr.Id is EItemAttributeId.MaxCount);
        if (attribute is null)
            return;
        attribute.DisplayNameFunc = () =>
        {
            string language = Singleton<SharedGameSettingsClass>.Instance?.Game?.Settings?.Language?.GetValue();
            if (language is null || !DisplayNames.ContainsKey(language))
                language = "en";
            return DisplayNames[language];
        };
        attribute.Base = () =>
        {
            int? ammoCount = GetAmmoCount(magazine, ActiveProfile, out _);
            return ammoCount ?? 0f;
        };
        attribute.StringValue = () =>
        {
            int? ammoCount = GetAmmoCount(magazine, ActiveProfile, out _);
            return $"{ammoCount?.ToString() ?? "?"}/{magazine.MaxCount}";
        };
        attribute.FullStringValue = () =>
        {
            int? ammoCount = GetAmmoCount(magazine, ActiveProfile, out bool magChecked);
            return magChecked
                ? string.Join(Environment.NewLine, magazine.Cartridges.Items.Reverse().Select(cartridge =>
                {
                    string count = ammoCount is not null ? cartridge.StackObjectsCount.ToString() : "?";
                    string name = ActiveProfile.Examined(cartridge) ? cartridge.LocalizedName() : "Unknown item".Localized();
                    return $"{count} × {name}";
                }))
                : "";
        };
    }

    private static int? GetAmmoCount(MagazineItemClass magazine, Profile profile, out bool magChecked)
    {
        if (!InGameStatus.InRaid || magazine.Count == 0)
        {
            magChecked = true;
            return magazine.Count;
        }
        magChecked = profile.CheckedMagazines.ContainsKey(magazine.Id);
        if (!magChecked)
            return null;
        bool equipped = profile.Inventory.Equipment.GetAllSlots().Any(slot => ReferenceEquals(slot.ContainedItem, magazine));
        if (magazine.Count >= (equipped ? magazine.MaxCount - 1 : magazine.MaxCount))
            return magazine.Count;
        int skill = Mathf.Max(profile.MagDrillsMastering, profile.CheckedMagazineSkillLevel(magazine.Id), magazine.CheckOverride);
        return skill > 1 || (skill == 1 && magazine.MaxCount <= 10) ? magazine.Count : null;
    }
}