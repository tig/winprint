using Foundation;
using ObjCRuntime;
using UIKit;

namespace WinPrint.Maui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    private const string KeySelector = "winprintHandleKeyCommand:";

    protected override MauiApp CreateMauiApp()
    {
        return MauiProgram.CreateMauiApp();
    }

    /// <summary>
    ///     App-wide keyboard shortcuts. There is no WinUI-style window key hook on
    ///     Catalyst, so without these the Mac app has no keyboard support at all. The
    ///     delegate sits at the end of the responder chain and the commands claim
    ///     priority over system behavior, so they work no matter which sidebar control
    ///     UIKit hands focus to (the sheet Picker would otherwise pop its menu open on
    ///     a stray Down arrow). The one exception: while a text field is being edited,
    ///     arrows and Home/End are NOT claimed at all, so caret movement keeps working.
    ///     This getter is consulted per keystroke, so the check is live.
    /// </summary>
    public override UIKeyCommand[] KeyCommands
    {
        get
        {
            var commands = new List<UIKeyCommand>
            {
                MakeKeyCommand(UIKeyCommand.PageUp, 0),
                MakeKeyCommand(UIKeyCommand.PageDown, 0),
                MakeKeyCommand(UIKeyCommand.Escape, 0),
                MakeKeyCommand((NSString)"=", UIKeyModifierFlags.Command),
                MakeKeyCommand((NSString)"+", UIKeyModifierFlags.Command),
                MakeKeyCommand((NSString)"-", UIKeyModifierFlags.Command),
                MakeKeyCommand((NSString)"0", UIKeyModifierFlags.Command)
            };

            if (!IsTextInputFocused())
            {
                commands.Add(MakeKeyCommand(UIKeyCommand.Home, 0));
                commands.Add(MakeKeyCommand(UIKeyCommand.End, 0));
                commands.Add(MakeKeyCommand(UIKeyCommand.UpArrow, 0));
                commands.Add(MakeKeyCommand(UIKeyCommand.DownArrow, 0));
                commands.Add(MakeKeyCommand(UIKeyCommand.LeftArrow, 0));
                commands.Add(MakeKeyCommand(UIKeyCommand.RightArrow, 0));
            }

            return [.. commands];
        }
    }

    private static UIKeyCommand MakeKeyCommand(NSString input, UIKeyModifierFlags modifiers)
    {
        var command = UIKeyCommand.Create(input, modifiers, new Selector(KeySelector));
        command.WantsPriorityOverSystemBehavior = true;
        return command;
    }

    private static bool IsTextInputFocused()
    {
        foreach (UIScene scene in UIApplication.SharedApplication.ConnectedScenes)
        {
            if (scene is not UIWindowScene windowScene)
            {
                continue;
            }

            foreach (UIWindow window in windowScene.Windows)
            {
                if (FindFirstResponder(window) is UITextField or UITextView)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static UIView? FindFirstResponder(UIView view)
    {
        if (view.IsFirstResponder)
        {
            return view;
        }

        foreach (UIView subview in view.Subviews)
        {
            if (FindFirstResponder(subview) is { } responder)
            {
                return responder;
            }
        }

        return null;
    }

    [Export(KeySelector)]
    public void HandleKeyCommand(UIKeyCommand sender)
    {
        MainPage? page = MainPage.Current;
        if (page is null || sender.Input is null)
        {
            return;
        }

        bool cmd = sender.ModifierFlags.HasFlag(UIKeyModifierFlags.Command);
        string input = sender.Input;
        if (input == UIKeyCommand.Escape)
        {
            // Escape always returns keyboard focus to the preview.
            page.FocusPreview();
            return;
        }

        string? key = input switch
        {
            _ when input == UIKeyCommand.PageUp => "PageUp",
            _ when input == UIKeyCommand.PageDown => "PageDown",
            _ when input == UIKeyCommand.Home => "Home",
            _ when input == UIKeyCommand.End => "End",
            _ when input == UIKeyCommand.UpArrow => "Up",
            _ when input == UIKeyCommand.DownArrow => "Down",
            _ when input == UIKeyCommand.LeftArrow => "Left",
            _ when input == UIKeyCommand.RightArrow => "Right",
            "=" or "+" => "Add",
            "-" => "Subtract",
            "0" => "D0",
            _ => null
        };

        if (key is not null)
        {
            // HandleKeyDown's zoom cases gate on ctrl; Command is the Mac equivalent.
            page.HandleKeyDown(key, cmd, false);
        }
    }
}
