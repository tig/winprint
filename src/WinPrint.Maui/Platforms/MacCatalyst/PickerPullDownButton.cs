using UIKit;

namespace WinPrint.Maui;

/// <summary>
///     A tagged transparent <see cref="UIButton" /> overlaid on a MAUI Picker field so
///     <see cref="MacPickerWorkaround" /> can find and reuse it when the Picker mapper re-runs.
///     Carries the native pull-down <see cref="UIMenu" /> that replaces MAUI's Mac-idiom-incompatible
///     <c>UIPickerView</c>.
/// </summary>
internal sealed class PickerPullDownButton : UIButton
{
    public PickerPullDownButton(UIButtonType type)
        : base(type)
    {
    }
}
