using Terminal.Gui.Input;
using WinPrint.Core.Models;
using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views.Editors;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Captures the <see cref="HeaderFooterEditor" /> macro autocomplete popup in action: focus the
///     format field, type a macro prefix, and verify (and snapshot) that <see cref="MacroCompletionProvider" />
///     offers the matching <c>{Macro}</c> names. Typing <c>file</c> should narrow the list to the
///     four <c>File*</c> macros from <see cref="MacroChoices" />.
/// </summary>
public class HeaderFooterAutocompleteGoldenTests
{
    private static readonly IReadOnlyList<Key> TypeFile = [Key.F, Key.I, Key.L, Key.E];

    [Fact]
    public void TypingMacroPrefix_OpensPopup_MatchesGolden()
    {
        var editor = new HeaderFooterEditor("_Header")
        {
            Value = new Header { Enabled = true, Text = string.Empty }
        };

        string screen = InteractiveCapture.CaptureWithKeys(
            editor,
            TypeFile,
            width: 50,
            height: 12,
            captureWhen: e => e.IsCompletionActive);

        // The popup lists the matching macros (filtered to the File* family by the "file" prefix).
        DriverAssert.ContainsText(screen, "{FileName}");
        DriverAssert.ContainsText(screen, "{FileExtension}");
        DriverAssert.DoesNotContainText(screen, "{Title}");

        GridSnapshot.Verify(screen, "header-autocomplete");
    }
}
