using Comfort.Common;
using EFT;
using EFT.Settings;
using EFT.InventoryLogic;
using SPT.Reflection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT.Trading;
using UnityEngine;

namespace IcyClawz.ItemSellPrice;

internal static class TraderClassExtensions
{
    private static IClientSession _session;
    private static IClientSession Session => _session ??= ClientAppUtils.GetMainApp().GetClientBackEndSession();

    private static readonly FieldInfo SupplyDataField =
        typeof(Trader).GetField("_supplyData", BindingFlags.Public | BindingFlags.Instance);

    public static SupplyData GetSupplyData(this Trader trader) =>
        SupplyDataField.GetValue(trader) as SupplyData;

    public static void SetSupplyData(this Trader trader, SupplyData supplyData) =>
        SupplyDataField.SetValue(trader, supplyData);

    public static async void UpdateSupplyData(this Trader trader)
    {
        var result = await Session.GetSupplyData(trader.Id);
        if (result.Succeed)
            trader.SetSupplyData(result.Value);
        else
            Debug.LogError("Failed to download supply daSettingsManagerta");
    }
}

internal static class ItemExtensions
{
    private static readonly Dictionary<string, string[]> DisplayNames = new()
    {
        ["ch"] = ["售价（{0}）", "无法出售给商人"],
        ["cz"] = ["Prodejní cena ({0})", "Nemůže být prodán obchodníkům"],
        ["en"] = ["Selling price ({0})", "Cannot be sold to traders"],
        ["es"] = ["Precio de venta ({0})", "No puede ser vendido a los vendedores"],
        ["es-mx"] = ["Precio de venta ({0})", "No puede ser vendido a comerciantes"],
        ["fr"] = ["Prix de vente ({0})", "Ne peut pas être vendu aux marchands"],
        ["ge"] = ["Verkaufspreis ({0})", "Kann nicht an Händler verkauft werden"],
        ["hu"] = ["Eladási ár ({0})", "Kereskedőknek nem adható el"],
        ["it"] = ["Prezzo di vendita ({0})", "Non vendibile ai mercanti"],
        ["jp"] = ["販売価格（{0}）", "トレーダーには販売できません"],
        ["kr"] = ["판매 가격 ({0})", "상인에게 판매 불가"],
        ["pl"] = ["Cena sprzedaży ({0})", "Nie można sprzedawać handlarzom"],
        ["po"] = ["Preço de venda ({0})", "Não pode ser vendido a comerciantes"],
        ["ru"] = ["Цена продажи ({0})", "Невозможно продать торговцам"],
        ["sk"] = ["Predajná cena ({0})", "Nedá sa predať obchodníkom"],
        ["tu"] = ["Satış fiyatı ({0})", "Tüccarlara satılamaz"],
    };

    private static IClientSession _session;
    private static IClientSession Session => _session ??= ClientAppUtils.GetMainApp().GetClientBackEndSession();

    public static void AddTraderOfferAttribute(this Item item)
    {
        ItemAttribute attribute = new(EItemAttributeId.MoneySum)
        {
            Name = EItemAttributeId.MoneySum.GetName(),
            DisplayNameFunc = () =>
            {
                var language = Singleton<SettingsManager>.Instance.Game.Settings.Language.Value;
                
                if (language is null || !DisplayNames.ContainsKey(language))
                    language = "en";
                var offer = GetBestTraderOffer(item);
                return offer is not null
                    ? string.Format(DisplayNames[language][0], offer.Name)
                    : DisplayNames[language][1];
            },
            Base = () =>
            {
                var offer = GetBestTraderOffer(item);
                return offer?.Price ?? 0.01f;
            },
            StringValue = () =>
            {
                var offer = GetBestTraderOffer(item);
                return offer is not null
                    ? $"{offer.Currency} {offer.Price}" + (offer.Count > 1 ? $" ({offer.Count})" : "")
                    : "";
            },
            FullStringValue = () =>
            {
                var offers = GetAllTraderOffers(item);
                return offers.Any()
                    ? string.Join(Environment.NewLine, offers.Select(offer => $"{offer.Name}: {offer.Currency} {offer.Price}"))
                    : "";
            },
            DisplayType = () => EItemAttributeDisplayType.Compact,
        };
        item.Attributes = [attribute, .. item.Attributes];
    }

    private sealed class TraderOffer(string name, int price, string currency, double course, int count)
    {
        public string Name = name;
        public int Price = price;
        public string Currency = currency;
        public double Course = course;
        public int Count = count;
    }

    private static TraderOffer GetTraderOffer(Item item, Trader trader)
    {
        var price = trader.GetUserItemPrice(item);
        return price.HasValue ? new(
            trader.LocalizedName,
            price.Value.Amount,
            CurrencyUtil.GetCurrencyCharById(price.Value.CurrencyId.Value),
            trader.GetSupplyData().CurrencyCourses[price.Value.CurrencyId.Value],
            item.StackObjectsCount
        ) : null;
    }

    private static IEnumerable<TraderOffer> GetAllTraderOffers(Item item)
    {
        if (!Session.Profile.Examined(item))
            return [];
        if (item.Owner?.OwnerType is EOwnerType.RagFair or EOwnerType.Trader
            && (item.StackObjectsCount > 1 || item.UnlimitedCount))
        {
            item = item.CloneItem();
            item.StackObjectsCount = 1;
            item.UnlimitedCount = false;
        }
        return Session.Traders
            .Where(trader => !trader.Settings.AvailableInRaid)
            .Select(trader => GetTraderOffer(item, trader))
            .Where(offer => offer is not null)
            .OrderByDescending(offer => offer.Price * offer.Course);
    }

    private static TraderOffer GetBestTraderOffer(Item item) =>
        GetAllTraderOffers(item).FirstOrDefault();
}