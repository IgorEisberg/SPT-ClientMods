using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    public Func<Sprite> Icon { get => Impl.CustomIcon; set => Impl.CustomIcon = value; }
    public Action Action { get => Impl.Action; set => Impl.Action = value; }
    public Func<IEnumerable<CustomInteraction>> SubMenu { get => Impl.SubMenu; set => Impl.SubMenu = value; }
    public Func<bool> Enabled { get => Impl.Enabled; set => Impl.Enabled = value; }
    public Func<string> Error { get => Impl.Error; set => Impl.Error = value; }
}

// DynamicContextInteraction is the SPT 4.1 replacement for the old (now-removed) DynamicInteractionClass.
// Unlike the old class, it's a plain concrete class (non-virtual Execute()), so we only subclass it to carry
// our extra data (Caption/Enabled/Error/SubMenu) and to tag instances for InteractionButtonsContainerPatch to
// recognize; the actual click/hover behaviour is fully replaced in that patch rather than relying on Execute().
internal sealed class CustomInteractionImpl(ItemUiContext context, string id) : DynamicContextInteraction(id, id, () => { }, null)
{
    internal readonly ItemUiContext Context = context;

    public Func<string> Caption { get; set; }
    public Func<Sprite> CustomIcon { get; set; }
    public Action Action { get; set; }
    public Func<IEnumerable<CustomInteraction>> SubMenu { get; set; }
    public Func<bool> Enabled { get; set; }
    public Func<string> Error { get; set; }

    public bool IsInteractive() => Enabled?.Invoke() ?? true;
}

// Comfort.Common.SuccessfulResult still exists for the "enabled" case, but the old SuccessfulResult/FailedResult
// pair EFT itself used to expose is gone; FailedInventoryResult (the only remaining IResult in Assembly-CSharp)
// requires a full InventoryError object, so we implement our own minimal IResult for the "disabled" case instead.
internal sealed class SimpleFailedResult(string error) : IResult
{
    public bool Succeed => false;
    public bool Failed => true;
    public string Error { get; } = error;
    public int ErrorCode => 0;
}

// The submenu container. Mirrors the original mod's design of subclassing the game's interactions base with the
// REAL ItemUiContext (the parameterless EmptyContextInteractions ctor passes null, and the submenu render path
// is not verified safe against a null context). ContextInteractions<T> declares exactly three abstract members;
// they're only reached for strongly-typed T interactions, which a purely dynamic submenu never has.
internal sealed class CustomSubInteractions(ItemUiContext context) : ContextInteractions<EItemInfoButton>(context)
{
    public override void ExecuteInteractionInternal(EItemInfoButton button) { }
    public override bool IsActive(EItemInfoButton button) => false;
    public override IResult IsInteractive(EItemInfoButton button) => SuccessfulResult.New;
}

internal static class AbstractInteractionsExtensions
{
    // _dynamicInteractions is PUBLIC despite the underscore-prefixed name (SPT 4.1's deobfuscation names many
    // public fields this way -- _buttonTemplate/_buttonsContainer below are the same). Reflection isn't needed
    // at all; direct access also makes a rename a compile error instead of a runtime NullReferenceException.
    public static void AddCustomInteraction(this ContextInteractions<EItemInfoButton> instance, CustomInteractionImpl impl) =>
        instance._dynamicInteractions[impl.Key] = impl;
}

internal static class InteractionButtonsContainerExtensions
{
    public static void AddCustomButton(this InteractionButtonsContainer instance, CustomInteractionImpl impl)
    {
        bool isInteractive = impl.IsInteractive();
        IEnumerable<CustomInteraction> subMenu = impl.SubMenu?.Invoke();
        bool hasSubMenu = subMenu?.Any() ?? false;
        SimpleContextMenuButton button = null;
        button = instance.CreateContextButton(
            impl.Key,
            impl.Caption?.Invoke() ?? "",
            instance._buttonTemplate,
            instance._buttonsContainer,
            impl.CustomIcon?.Invoke(),
            () =>
            {
                if (isInteractive)
                    impl.Action?.Invoke();
            },
            () =>
            {
                instance.CloseSubMenu();
                if (isInteractive && hasSubMenu)
                {
                    CustomSubInteractions subInteractions = new(impl.Context);
                    foreach (CustomInteractionImpl subImpl in subMenu.Select(item => item.Impl))
                        subInteractions.AddCustomInteraction(subImpl);
                    instance.SetSubInteractions(subInteractions);
                }
            },
            hasSubMenu,
            false
        );
        button.SetButtonInteraction(
            isInteractive ? SuccessfulResult.New : new SimpleFailedResult(impl.Error?.Invoke() ?? "")
        );
        instance.BindButton(button);
    }
}
