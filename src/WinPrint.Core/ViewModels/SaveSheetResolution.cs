namespace WinPrint.Core.ViewModels;

/// <summary>
///     The outcome of a front end's "save sheet definition" exit prompt for a single dirty definition:
///     the chosen <see cref="SaveSheetChoice" /> plus the data that choice needs.
///     Returned by the prompt delegate passed to
///     <see cref="AppViewModel.ResolveUnsavedSheetsOnExitAsync" /> so the save-on-exit decision logic is
///     shared and front-end-agnostic (the front end only owns showing the dialog).
/// </summary>
/// <param name="Choice">What the user chose.</param>
/// <param name="SelectedIndex">For <see cref="SaveSheetChoice.Save" />: the index within
///     <see cref="AppViewModel.SheetDefinitions" /> to save onto. Ignored otherwise.</param>
/// <param name="NewName">For <see cref="SaveSheetChoice.Create" />: the name for the new definition.
///     Ignored otherwise.</param>
public sealed record SaveSheetResolution(SaveSheetChoice Choice, int SelectedIndex, string NewName);
