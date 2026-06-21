using Microsoft.Maui.Handlers;
using UIKit;

namespace WinPrint.Maui;

/// <summary>
///     A MacCatalyst handler that renders a MAUI <see cref="Picker" /> as a native pop-up button
///     (<see cref="UIButton" /> with a <see cref="UIMenu" />, like <c>NSPopUpButton</c>).
///     <para>
///         MAUI's stock Picker opens its options in a <c>UIPickerView</c>, which is unsupported in
///         the Mac idiom (<c>UIDeviceFamily = 6</c>) and crashes with an uncaught
///         <c>NSException</c> → SIGTRAP the moment the dropdown opens — see issue #133. A pop-up
///         button is the native Mac-idiom dropdown and renders/behaves correctly. Registered for
///         <see cref="Picker" /> from <c>MauiProgram</c> on MacCatalyst only.
///     </para>
/// </summary>
internal sealed class MacPickerHandler : ViewHandler<IPicker, UIButton>
{
    public static readonly IPropertyMapper<IPicker, MacPickerHandler> PickerMapper =
        new PropertyMapper<IPicker, MacPickerHandler>(ViewMapper)
        {
            [nameof(IPicker.Title)] = static (handler, _) => handler.RebuildMenu(),
            [nameof(IPicker.SelectedIndex)] = static (handler, _) => handler.RebuildMenu(),
            [nameof(IPicker.TextColor)] = static (handler, _) => handler.RebuildMenu(),
        };

    public MacPickerHandler()
        : base(PickerMapper)
    {
    }

    // A plain UIButton instance (not a subclass) may use the UIButtonType constructor.
    protected override UIButton CreatePlatformView()
    {
        var button = new UIButton(UIButtonType.System)
        {
            ShowsMenuAsPrimaryAction = true,
            ChangesSelectionAsPrimaryAction = true, // pop-up button: shows the selected item + chevron
            Configuration = UIButtonConfiguration.BorderedButtonConfiguration,
        };
        return button;
    }

    protected override void ConnectHandler(UIButton platformView)
    {
        base.ConnectHandler(platformView);
        RebuildMenu();
    }

    private void RebuildMenu()
    {
        if (VirtualView is null || PlatformView is null)
        {
            return;
        }

        int count = VirtualView.GetCount();
        var actions = new UIMenuElement[count];
        for (int i = 0; i < count; i++)
        {
            int index = i;
            var action = UIAction.Create(
                VirtualView.GetItem(i) ?? string.Empty,
                null,
                null,
                _ =>
                {
                    if (VirtualView is not null)
                    {
                        VirtualView.SelectedIndex = index;
                    }
                });
            action.State = index == VirtualView.SelectedIndex ? UIMenuElementState.On : UIMenuElementState.Off;
            actions[i] = action;
        }

        PlatformView.Menu = UIMenu.Create(actions);

        // Show the title when nothing is selected yet (ChangesSelectionAsPrimaryAction shows the
        // checked item's title once there is a selection).
        if (count == 0 || VirtualView.SelectedIndex < 0)
        {
            PlatformView.SetTitle(VirtualView.Title ?? string.Empty, UIControlState.Normal);
        }
    }
}
