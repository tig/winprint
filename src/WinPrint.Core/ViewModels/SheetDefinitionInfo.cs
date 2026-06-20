namespace WinPrint.Core.ViewModels;

/// <summary>
///     Lightweight descriptor for a sheet definition (a <c>Settings.Sheets</c> entry):
///     its dictionary <see cref="Key" /> (a Guid string) and friendly <see cref="Name" />.
///     Used to populate the "save sheet definition" prompt shown by each front end on exit.
/// </summary>
public sealed record SheetDefinitionInfo(string Key, string Name);
