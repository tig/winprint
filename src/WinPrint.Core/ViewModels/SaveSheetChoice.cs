namespace WinPrint.Core.ViewModels;

/// <summary>
///     The action a user chose in a front end's "save sheet definition" exit prompt. Shared by the
///     TUI, WinForms, and MAUI prompts so the choice handling is consistent across front ends.
/// </summary>
public enum SaveSheetChoice
{
    /// <summary>Abort the exit and keep editing.</summary>
    Cancel,

    /// <summary>Save the edits to the selected existing definition.</summary>
    Save,

    /// <summary>Discard the edits (revert to baseline) but still allow the exit to proceed.</summary>
    DontSave,

    /// <summary>Create a new definition from the edits using the typed name.</summary>
    Create
}
