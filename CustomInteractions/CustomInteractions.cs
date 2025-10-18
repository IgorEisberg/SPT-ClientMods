using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

using EmptyInteractionsAbstractClass = GClass3775;

namespace IcyClawz.CustomInteractions;

public interface ICustomInteractionsProvider
{
    IEnumerable<CustomInteraction> GetCustomInteractions(ItemUiContext context, EItemViewType viewType, Item item);
}

public static class CustomInteractionsManager
{
    internal static readonly LinkedList<ICustomInteractionsProvider> Providers = [];

    public static void Register(ICustomInteractionsProvider provider)
    {
        if (!Providers.Contains(provider))
            Providers.AddLast(provider);
    }
}

public class CustomInteraction(ItemUiContext context)
{
    internal readonly CustomInteractionImpl Impl = new(context, UnityEngine.Random.Range(0, int.MaxValue).ToString("x4"));

    public Func<string> Caption { get => Impl.Caption; set => Impl.Caption = value; }
    public Func<Sprite> Icon { get => Impl.Icon; set => Impl.Icon = value; }
    public Action Action { get => Impl.Action; set => Impl.Action = value; }
    public Func<IEnumerable<CustomInteraction>> SubMenu { get => Impl.SubMenu; set => Impl.SubMenu = value; }
    public Func<bool> Enabled { get => Impl.Enabled; set => Impl.Enabled = value; }
    public Func<string> Error { get => Impl.Error; set => Impl.Error = value; }
}

internal sealed class CustomInteractionImpl(ItemUiContext context, string id) : DynamicInteractionClass(id, id)
{
    internal readonly ItemUiContext Context = context;

    public Func<string> Caption { get; set; }
    public new Func<Sprite> Icon { get; set; }
    public Action Action { get => Action_0; set => Action_0 = value; }
    public Func<IEnumerable<CustomInteraction>> SubMenu { get; set; }
    public Func<bool> Enabled { get; set; }
    public Func<string> Error { get; set; }

    public bool IsInteractive() => Enabled?.Invoke() ?? true;
}

internal sealed class CustomInteractionsImpl(ItemUiContext context) : EmptyInteractionsAbstractClass(context)
{
    public IEnumerable<CustomInteractionImpl> CustomInteractions => DynamicInteractions.OfType<CustomInteractionImpl>();
    public override bool HasIcons => CustomInteractions.Any(interaction => interaction.Icon is not null);
}

internal static class AbstractInteractionsExtensions
{
    private static Dictionary<string, DynamicInteractionClass> GetDynamicInteractions<T>(this ItemInfoInteractionsAbstractClass<T> instance) where T : struct, Enum =>
        typeof(ItemInfoInteractionsAbstractClass<T>).GetField("Dictionary_0", BindingFlags.Public | BindingFlags.Instance)
            .GetValue(instance) as Dictionary<string, DynamicInteractionClass>;

    public static void AddCustomInteraction<T>(this ItemInfoInteractionsAbstractClass<T> instance, CustomInteractionImpl impl) where T : struct, Enum =>
        instance.GetDynamicInteractions()[impl.Key] = impl;
}

internal static class InteractionButtonsContainerExtensions
{
    private static readonly FieldInfo ButtonsContainerField =
        typeof(InteractionButtonsContainer).GetField("_buttonsContainer", BindingFlags.NonPublic | BindingFlags.Instance);

    private static RectTransform GetButtonsContainer(this InteractionButtonsContainer instance) =>
        ButtonsContainerField.GetValue(instance) as RectTransform;

    private static readonly FieldInfo ButtonTemplateField =
        typeof(InteractionButtonsContainer).GetField("_buttonTemplate", BindingFlags.NonPublic | BindingFlags.Instance);

    private static SimpleContextMenuButton GetButtonTemplate(this InteractionButtonsContainer instance) =>
        ButtonTemplateField.GetValue(instance) as SimpleContextMenuButton;

    private static readonly FieldInfo CurrentButtonField =
        typeof(InteractionButtonsContainer).GetField("simpleContextMenuButton_0", BindingFlags.NonPublic | BindingFlags.Instance);

    private static void SetCurrentButton(this InteractionButtonsContainer instance, SimpleContextMenuButton button) =>
        CurrentButtonField.SetValue(instance, button);

    private static readonly MethodInfo CreateButtonMethod =
        typeof(InteractionButtonsContainer).GetMethod("method_1", BindingFlags.Public | BindingFlags.Instance);

    private static SimpleContextMenuButton CreateButton(this InteractionButtonsContainer instance,
        string key, string caption, SimpleContextMenuButton template, RectTransform container,
        [CanBeNull] Sprite sprite, [CanBeNull] Action onButtonClicked, [CanBeNull] Action onMouseHover,
        bool subMenu = false, bool autoClose = true) =>
        (SimpleContextMenuButton)CreateButtonMethod.Invoke(instance, [
            key, caption, template, container, sprite, onButtonClicked, onMouseHover, subMenu, autoClose
        ]);

    private static readonly MethodInfo CloseSubMenuMethod =
        typeof(InteractionButtonsContainer).GetMethod("method_4", BindingFlags.Public | BindingFlags.Instance);

    private static void CloseSubMenu(this InteractionButtonsContainer instance) =>
        CloseSubMenuMethod.Invoke(instance, null);

    private static readonly MethodInfo AddButtonMethod =
        typeof(InteractionButtonsContainer).GetMethod("method_5", BindingFlags.Public | BindingFlags.Instance);

    private static void AddButton(this InteractionButtonsContainer instance, SimpleContextMenuButton button) =>
        AddButtonMethod.Invoke(instance, [button]);

    public static void AddCustomButton(this InteractionButtonsContainer instance, CustomInteractionImpl impl)
    {
        bool isInteractive = impl.IsInteractive();
        IEnumerable<CustomInteraction> subMenu = impl.SubMenu?.Invoke();
        SimpleContextMenuButton button = null;
        button = instance.CreateButton(
            impl.Key,
            impl.Caption?.Invoke() ?? "",
            instance.GetButtonTemplate(),
            instance.GetButtonsContainer(),
            impl.Icon?.Invoke(),
            () =>
            {
                if (isInteractive)
                    impl.Execute();
            },
            () =>
            {
                instance.SetCurrentButton(button);
                instance.CloseSubMenu();
                if (isInteractive && subMenu != null)
                {
                    CustomInteractionsImpl subInteractions = new CustomInteractionsImpl(impl.Context);
                    foreach (CustomInteractionImpl subImpl in subMenu.Select(item => item.Impl))
                        subInteractions.AddCustomInteraction(subImpl);
                    instance.SetSubInteractions(subInteractions);
                }
            },
            subMenu?.Any() ?? false,
            false
        );
        button.SetButtonInteraction(
            isInteractive ? SuccessfulResult.New : new FailedResult(impl.Error?.Invoke() ?? "", 0)
        );
        instance.AddButton(button);
    }
}