namespace WinPrint.Core;

public class SheetViewModelSettingsChangedEvent
{
    public bool Reflow { get; set; }
    public string PropertyName { get; set; } = string.Empty;
}
