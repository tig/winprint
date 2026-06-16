using System.Globalization;
using UIKit;

namespace WinPrint.Maui;

internal static class MacFontPicker
{
    private const string FixedPitchFonts = "Fixed-Pitch Fonts";
    private const string AllFonts = "All Fonts (HTML/Markdown)";

    public static async Task<(string Family, float Size, string Style)?> PickAsync(
        Page host, string currentFamily, float currentSize, string currentStyle, bool canChooseProportional)
    {
        bool showAllFonts = false;
        if (canChooseProportional)
        {
            string choice = await host.DisplayActionSheetAsync(
                "Content Font", "Cancel", null, FixedPitchFonts, AllFonts);
            switch (choice)
            {
                case FixedPitchFonts:
                    break;
                case AllFonts:
                    showAllFonts = true;
                    break;
                default:
                    return null;
            }
        }

        (string Family, string Style)? font = await PickFontFamilyAsync(currentSize, showAllFonts);
        if (font is null)
        {
            return null;
        }

        string? sizeInput = await host.DisplayPromptAsync(
            "Font Size",
            $"Selected: {font.Value.Family}\nEnter size in points",
            initialValue: currentSize.ToString(CultureInfo.CurrentCulture),
            keyboard: Keyboard.Numeric);
        if (string.IsNullOrWhiteSpace(sizeInput))
        {
            return null;
        }

        float size = float.TryParse(sizeInput, NumberStyles.Float, CultureInfo.CurrentCulture, out float parsed)
            ? parsed
            : currentSize;
        string style = string.IsNullOrWhiteSpace(font.Value.Style) ? currentStyle : font.Value.Style;
        return (font.Value.Family, size, style);
    }

    private static async Task<(string Family, string Style)?> PickFontFamilyAsync(float currentSize, bool showAllFonts)
    {
        UIViewController? presenter = GetTopViewController();
        if (presenter is null)
        {
            return null;
        }

        var configuration = new UIFontPickerViewControllerConfiguration
        {
            IncludeFaces = true
        };
        if (!showAllFonts)
        {
            configuration.FilteredTraits = UIFontDescriptorSymbolicTraits.MonoSpace;
        }

        var picker = new UIFontPickerViewController(configuration);
        var completion = new TaskCompletionSource<(string Family, string Style)?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pickerDelegate = new MacFontPickerDelegate(completion, currentSize);
        picker.Delegate = pickerDelegate;

        presenter.PresentViewController(picker, true, null);
        (string Family, string Style)? result = await completion.Task;
        GC.KeepAlive(pickerDelegate);
        return result;
    }

    private static UIViewController? GetTopViewController()
    {
        UIViewController? controller = null;
        foreach (UIScene scene in UIApplication.SharedApplication.ConnectedScenes)
        {
            if (scene is not UIWindowScene windowScene)
            {
                continue;
            }

            foreach (UIWindow window in windowScene.Windows)
            {
                if (window.IsKeyWindow)
                {
                    controller = window.RootViewController;
                    break;
                }
            }

            if (controller is not null)
            {
                break;
            }
        }

        while (controller?.PresentedViewController is { } presented)
        {
            controller = presented;
        }

        return controller;
    }
}
