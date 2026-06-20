using CoreGraphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace WinPrint.Maui;

/// <summary>
///     Gives MAUI's <see cref="Picker" /> a native pull-down dropdown in the Mac idiom.
///     <para>
///         The app runs in the Mac idiom (<c>UIDeviceFamily = 6</c>, set in Info.plist for native
///         1:1 scaling). MAUI's iOS/Catalyst Picker opens its options in a <c>UIPickerView</c>
///         (the iOS spinning wheel), but <c>UIPickerView</c> is unsupported in the Mac idiom and
///         throws <c>_throwForUnsupportedNonMacIdiomBehaviorWithReason</c> (an uncaught
///         <c>NSException</c> → SIGTRAP) the moment the dropdown opens — see issue #133.
///     </para>
///     <para>
///         This suppresses that input view and overlays the picker field with a transparent
///         <c>UIButton</c> whose <c>UIMenu</c> is built from the picker's items — i.e. a native
///         pull-down (pop-up button, like <c>NSPopUpButton</c>) anchored to the control, with the
///         current selection check-marked. The Mac idiom and the field's displayed text are kept.
///         Registered from <c>MauiProgram</c> on MacCatalyst only.
///     </para>
/// </summary>
internal static class MacPickerWorkaround
{
    public static void Register()
    {
        PickerHandler.Mapper.AppendToMapping("WinPrintMacIdiomPicker", static (handler, picker) =>
        {
            if (handler.PlatformView is not MauiPicker field)
            {
                return;
            }

            // Never let MAUI install the crashing UIPickerView as the input view.
            field.InputView = new UIView(CGRect.Empty);
            field.InputAccessoryView = null;
            field.TintColor = UIColor.Clear; // hide the caret; selection is via the menu

            PickerPullDownButton button = FindOrCreateButton(field);
            button.Menu = BuildMenu(picker);
        });
    }

    private static PickerPullDownButton FindOrCreateButton(MauiPicker field)
    {
        foreach (UIView sub in field.Subviews)
        {
            if (sub is PickerPullDownButton existing)
            {
                return existing;
            }
        }

        var button = new PickerPullDownButton(UIButtonType.Custom)
        {
            ShowsMenuAsPrimaryAction = true, // click opens the pull-down anchored to the field
            Frame = field.Bounds,
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
        };
        field.AddSubview(button);
        return button;
    }

    private static UIMenu BuildMenu(IPicker picker)
    {
        int count = picker.GetCount();
        var actions = new UIMenuElement[count];
        for (int i = 0; i < count; i++)
        {
            int index = i;
            var action = UIAction.Create(
                picker.GetItem(i) ?? string.Empty,
                null,
                null,
                _ => picker.SelectedIndex = index);
            action.State = index == picker.SelectedIndex ? UIMenuElementState.On : UIMenuElementState.Off;
            actions[i] = action;
        }

        return UIMenu.Create(actions);
    }
}
