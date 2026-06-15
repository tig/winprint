using UIKit;

namespace WinPrint.Maui;

internal sealed class MacFontPickerDelegate : UIFontPickerViewControllerDelegate
{
    private readonly TaskCompletionSource<(string Family, string Style)?> _completion;
    private readonly float _currentSize;

    public MacFontPickerDelegate(
        TaskCompletionSource<(string Family, string Style)?> completion, float currentSize)
    {
        _completion = completion;
        _currentSize = currentSize;
    }

    public override void DidPickFont(UIFontPickerViewController viewController)
    {
        UIFontDescriptor? descriptor = viewController.SelectedFontDescriptor;
        if (descriptor is null)
        {
            Complete(viewController, null);
            return;
        }

        UIFont? font = UIFont.FromDescriptor(descriptor, _currentSize);
        if (font is null)
        {
            Complete(viewController, null);
            return;
        }

        Complete(viewController, (font.FamilyName, GetStyle(descriptor.SymbolicTraits)));
    }

    public override void WasCancelled(UIFontPickerViewController viewController)
    {
        Complete(viewController, null);
    }

    private void Complete(UIFontPickerViewController viewController, (string Family, string Style)? result)
    {
        viewController.DismissViewController(true, null);
        _completion.TrySetResult(result);
    }

    private static string GetStyle(UIFontDescriptorSymbolicTraits traits)
    {
        bool bold = traits.HasFlag(UIFontDescriptorSymbolicTraits.Bold);
        bool italic = traits.HasFlag(UIFontDescriptorSymbolicTraits.Italic);
        return (bold, italic) switch
        {
            (true, true) => "Bold, Italic",
            (true, false) => "Bold",
            (false, true) => "Italic",
            _ => "Regular"
        };
    }
}
