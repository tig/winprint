using System.Reflection;
using Terminal.Gui.Views;
using WinPrint.TUI.Views.Editors;

namespace WinPrint.TUI.UnitTests.Testing;

/// <summary>Test helpers for driving composed editor controls the way a user would.</summary>
internal static class EditorTesting
{
    /// <summary>
    ///     Selects <paramref name="value" /> in one of a <see cref="FontEditor" />'s composed dropdowns
    ///     (referenced by private field name, e.g. <c>_family</c> / <c>_size</c>), simulating a user pick
    ///     without the brittleness of sending key sequences through the driver.
    /// </summary>
    public static void SelectInDropDown(this FontEditor editor, string fieldName, string value)
    {
        FieldInfo field = typeof(FontEditor).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                          ?? throw new InvalidOperationException($"FontEditor has no field '{fieldName}'.");
        var dropDown = (DropDownList)field.GetValue(editor)!;
        dropDown.Value = value;
    }
}
