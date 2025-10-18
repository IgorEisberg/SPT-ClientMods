using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

using CurrencyUtil = GClass3130;

namespace IcyClawz.ItemSellPrice;

internal static class TraderClassExtensions
{
    private static ISession _Session;
    private static ISession Session => _Session ??= ClientAppUtils.GetMainApp().GetClientBackEndSession();

    private static readonly FieldInfo SupplyDataField =
        typeof(TraderClass).GetField("SupplyData_0", BindingFlags.Public | BindingFlags.Instance);

    public static SupplyData GetSupplyData(this TraderClass trader) =>
        SupplyDataField.GetValue(trader) as SupplyData;

    public static void SetSupplyData(this TraderClass trader, SupplyData supplyData) =>
        SupplyDataField.SetValue(trader, supplyData);

    public static async void UpdateSupplyData(this TraderClass trader)
    {
        Result<SupplyData> result = await Session.GetSupplyData(trader.Id);
        if (result.Succeed)
            trader.SetSupplyData(result.Value);
        else
            Debug.LogError("Failed to download supply data");
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

    private static ISession _Session;
    private static ISession Session => _Session ??= ClientAppUtils.GetMainApp().GetClientBackEndSession();

    public static void AddTraderOfferAttribute(this Item item)
    {
        ItemAttributeClass attribute = new(EItemAttributeId.MoneySum)
        {
            Name = EItemAttributeId.MoneySum.GetName(),
            DisplayNameFunc = () =>
            {
                string language = Singleton<SharedGameSettingsClass>.Instance?.Game?.Settings?.Language?.GetValue();
                if (language is null || !DisplayNames.ContainsKey(language))
                    language = "en";
                TraderOffer offer = GetBestTraderOffer(item);
                return offer is not null
                    ? string.Format(DisplayNames[language][0], offer.Name)
                    : DisplayNames[language][1];
            },
            Base = () =>
            {
                TraderOffer offer = GetBestTraderOffer(item);
                return offer is not null ? offer.Price : 0.01f;
            },
            StringValue = () =>
            {
                TraderOffer offer = GetBestTraderOffer(item);
                return offer is not null
                    ? $"{offer.Currency} {offer.Price}" + (offer.Count > 1 ? $" ({offer.Count})" : "")
                    : "";
            },
            FullStringValue = () =>
            {
                IEnumerable<TraderOffer> offers = GetAllTraderOffers(item);
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

    private static TraderOffer GetTraderOffer(Item item, TraderClass trader)
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