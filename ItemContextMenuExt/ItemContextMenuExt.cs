using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using IcyClawz.CustomInteractions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using ILightTemplate = GInterface370;
using ISightTemplate = GInterface378;
using GlobalEvents = GClass3466;

namespace IcyClawz.ItemContextMenuExt;

internal static class SightComponentExtensions
{
    public static int[] GetScopeCalibrationDistances(this SightComponent instance, int scopeIndex) =>
        ((ISightTemplate)instance.Item.Template).CalibrationDistances[scopeIndex];

    public static float[] GetScopeZooms(this SightComponent instance, int scopeIndex) =>
        ((ISightTemplate)instance.Item.Template).Zooms[scopeIndex];
}

internal static class LightComponentExtensions
{
    public static int GetModesCount(this LightComponent instance) =>
        ((ILightTemplate)instance.Item.Template).ModesCount;
}

internal sealed class CustomInteractionsProvider : ICustomInteractionsProvider
{
    private const string IconsPrefix = "Characteristics/Icons/";
    private static StaticIcons StaticIcons => EFTHardSettings.Instance.StaticIcons;

    public IEnumerable<CustomInteraction> GetCustomInteractions(ItemUiContext context, EItemViewType viewType, Item item)
    {
        if (viewType is EItemViewType.Inventory)
        {
            FireModeComponent fireModeComponent = item.GetItemComponent<FireModeComponent>();
            if (fireModeComponent is not null)
                return GetFireModeInteractions(context, fireModeComponent);

            SightComponent sightComponent = item.GetItemComponent<SightComponent>();
            if (sightComponent is not null)
                return GetSightInteractions(context, sightComponent);

            LightComponent lightComponent = item.GetItemComponent<LightComponent>();
            if (lightComponent is not null)
                return GetLightInteractions(context, lightComponent);
        }
        return [];
    }

    private IEnumerable<CustomInteraction> GetFireModeInteractions(ItemUiContext context, FireModeComponent component)
    {
        if (component.AvailableEFireModes.Length > 1)
        {
            yield return new(context)
            {
                Caption = () => "Firing mode",
                Icon = () => StaticIcons.GetAttributeIcon(EItemAttributeId.Weapon),
                SubMenu = () => GetFireModeSubInteractions(context, component),
            };
        }
    }

    private IEnumerable<CustomInteraction> GetFireModeSubInteractions(ItemUiContext context, FireModeComponent component)
    {
        foreach (Weapon.EFireMode fireMode in component.AvailableEFireModes)
        {
            yield return new(context)
            {
                Caption = () => fireMode.ToString().Localized(),
                Action = () =>
                {
                    Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuContextMenu);
                    ComponentUtils.SetFireMode(component, fireMode);
                },
                Enabled = () => fireMode != component.FireMode,
            };
        }
    }

    private IEnumerable<CustomInteraction> GetSightInteractions(ItemUiContext context, SightComponent component)
    {
        if (component.ScopesCount > 1)
        {
            yield return new(context)
            {
                Caption = () => "Active scope",
                Icon = () => StaticIcons.GetAttributeIcon(EItemAttributeId.EncodeState),
                SubMenu = () => GetScopeIndexSubInteractions(context, component),
            };
        }
        if (component.GetScopeModesCount(component.SelectedScope) > 1)
        {
            yield return new(context)
            {
                Caption = () => "Active mode",
                Icon = () => StaticIcons.GetAttributeIcon(EItemAttributeId.EncodeState),
                SubMenu = () => GetScopeModeSubInteractions(context, component),
            };
        }
        if (component.GetScopeCalibrationDistances(component.SelectedScope).Length > 1)
        {
            yield return new(context)
            {
                Caption = () => "Zero distance",
                Icon = () => StaticIcons.GetAttributeIcon(EItemAttributeId.EncodeState),
                SubMenu = () => GetScopeCalibSubInteractions(context, component),
            };
        }
    }

    private IEnumerable<CustomInteraction> GetScopeIndexSubInteractions(ItemUiContext context, SightComponent component)
    {
        foreach (int scopeIndex in Enumerable.Range(0, component.ScopesCount))
        {
            yield return new(context)
            {
                Caption = () => $"Scope {scopeIndex + 1}",
                Action = () =>
                {
                    Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuContextMenu);
                    ComponentUtils.SetScopeIndex(component, scopeIndex);
                },
                Enabled = () => scopeIndex != component.SelectedScope,
            };
        }
    }

    private IEnumerable<CustomInteraction> GetScopeModeSubInteractions(ItemUiContext context, SightComponent component)
    {
        int scopeIndex = component.SelectedScope;
        float[] scopeZooms = component.GetScopeZooms(scopeIndex);
        foreach (int scopeMode in Enumerable.Range(0, component.GetScopeModesCount(scopeIndex)))
        {
            float scopeZoom = scopeZooms.Length > 0 ? scopeZooms[scopeMode] : 0f;
            yield return new(context)
            {
                Caption = () => $"Mode {scopeMode + 1} ({scopeZoom}x)",
                Action = () =>
                {
                    Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuContextMenu);
                    ComponentUtils.SetScopeMode(component, scopeMode);
                },
                Enabled = () => scopeMode != component.ScopesSelectedModes[scopeIndex],
            };
        }
    }

    private IEnumerable<CustomInteraction> GetScopeCalibSubInteractions(ItemUiContext context, SightComponent component)
    {
        int scopeIndex = component.SelectedScope;
        int[] scopeCalibDistances = component.GetScopeCalibrationDistances(scopeIndex);
        foreach (int scopeCalibIndex in Enumerable.Range(0, scopeCalibDistances.Length))
        {
            yield return new(context)
            {
                Caption = () => $"{scopeCalibDistances[scopeCalibIndex]}",
                Action = () =>
                {
                    Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuContextMenu);
                    ComponentUtils.SetScopeCalibIndex(component, scopeCalibIndex);
                },
                Enabled = () => scopeCalibIndex != component.ScopesCurrentCalibPointIndexes[scopeIndex],
            };
        }
    }

    private IEnumerable<CustomInteraction> GetLightInteractions(ItemUiContext context, LightComponent component)
    {
        yield return new(context)
        {
            Caption = () => component.IsActive ? "Turn off" : "Turn on",
            Icon = () => CacheResourcesPopAbstractClass.Pop<Sprite>(IconsPrefix + (component.IsActive ? "TurnOff" : "TurnOn")),
            Action = () =>
            {
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuContextMenu);
                ComponentUtils.SetLightActive(component, !component.IsActive);
                GlobalEvents.RequestGlobalRedraw();
            },
        };
        if (component.GetModesCount() > 1)
        {
            yield return new(context)
            {
                Caption = () => "Active mode",
                Icon = () => StaticIcons.GetAttributeIcon(EItemAttributeId.EncodeState),
                SubMenu = () => GetLightModeSubInteractions(context, component),
            };
        }
    }

    private IEnumerable<CustomInteraction> GetLightModeSubInteractions(ItemUiContext context, LightComponent component)
    {
        foreach (int lightMode in Enumerable.Range(0, component.GetModesCount()))
        {
            yield return new(context)
            {
                Caption = () => $"Mode {lightMode + 1}",
                Action = () =>
                {
                    Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuContextMenu);
                    ComponentUtils.SetLightMode(component, lightMode);
                },
                Enabled = () => lightMode != component.SelectedMode,
            };
        }
    }
}

internal static class ComponentUtils
{
    public static void SetFireMode(FireModeComponent component, Weapon.EFireMode fireMode)
    {
        Player player = GamePlayerOwner.MyPlayer;

        if (player is not null && player.HandsController is Player.FirearmController fc && component.Item == fc.Item)
        {
            if (fc.Item.MalfState.State is not Weapon.EMalfunctionState.None)
            {
                fc.FirearmsAnimator.MisfireSlideUnknown(false);
                player.InventoryController.ExamineMalfunction(fc.Item, false);
                return;
            }

            fc.ChangeFireMode(fireMode);
            return;
        }

        component.SetFireMode(fireMode);
    }

    public static void SetScopeIndex(SightComponent component, int scopeIndex)
    {
        SetScopeState(component, new()
        {
            Id = component.Item.Id,
            ScopeIndexInsideSight = scopeIndex,
            ScopeMode = component.ScopesSelectedModes[scopeIndex],
            ScopeCalibrationIndex = component.ScopesCurrentCalibPointIndexes[scopeIndex],
        });
    }

    public static void SetScopeMode(SightComponent component, int scopeMode)
    {
        int scopeIndex = component.SelectedScope;
        SetScopeState(component, new()
        {
            Id = component.Item.Id,
            ScopeIndexInsideSight = scopeIndex,
            ScopeMode = scopeMode,
            ScopeCalibrationIndex = component.ScopesCurrentCalibPointIndexes[scopeIndex],
        });
    }

    public static void SetScopeCalibIndex(SightComponent component, int scopeCalibIndex)
    {
        int scopeIndex = component.SelectedScope;
        SetScopeState(component, new()
        {
            Id = component.Item.Id,
            ScopeIndexInsideSight = scopeIndex,
            ScopeMode = component.ScopesSelectedModes[scopeIndex],
            ScopeCalibrationIndex = scopeCalibIndex,
        });
    }

    private static void SetScopeState(SightComponent component, FirearmScopeStateStruct scopeState)
    {
        Player player = GamePlayerOwner.MyPlayer;

        if (player is not null && player.HandsController is Player.FirearmController fc && component.Item.IsChildOf(fc.Item))
        {
            fc.SetScopeMode([scopeState]);
            return;
        }

        int scopeIndex = scopeState.ScopeIndexInsideSight;
        component.SelectedScope = scopeIndex;
        component.ScopesSelectedModes[scopeIndex] = scopeState.ScopeMode;
        component.ScopesCurrentCalibPointIndexes[scopeIndex] = scopeState.ScopeCalibrationIndex;
    }

    public static void SetLightActive(LightComponent component, bool isActive)
    {
        SetLightState(component, new()
        {
            Id = component.Item.Id,
            IsActive = isActive,
            LightMode = component.SelectedMode,
        });
    }

    public static void SetLightMode(LightComponent component, int lightMode)
    {
        SetLightState(component, new()
        {
            Id = component.Item.Id,
            IsActive = component.IsActive,
            LightMode = lightMode,
        });
    }

    private static void SetLightState(LightComponent component, FirearmLightStateStruct lightState)
    {
        Player player = GamePlayerOwner.MyPlayer;

        if (player is not null && player.HandsController is Player.FirearmController fc && component.Item.IsChildOf(fc.Item))
        {
            fc.SetLightsState([lightState]);
            return;
        }

        component.IsActive = lightState.IsActive;
        component.SelectedMode = lightState.LightMode;

        if (player is not null)
        {
            foreach (TacticalComboVisualController tcvc in player.GetComponentsInChildren<TacticalComboVisualController>())
            {
                if (ReferenceEquals(tcvc.LightMod, component))
                {
                    tcvc.UpdateBeams();
                    break;
                }
            }
        }
    }
}