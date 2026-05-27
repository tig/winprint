namespace WinPrint.cli;

public sealed record PrintResult (
    string Action,
    int Sheets,
    string ContentType,
    string Language,
    string ContentTypeEngine,
    string Printer,
    string PaperSize,
    string Orientation,
    string SheetDefinition)
{
    public override string ToString ()
    {
        return Action switch
        {
            "printed" => $"Printed {Sheets} sheet{Plural (Sheets)}.",
            "counted" => $"Would print {Sheets} sheet{Plural (Sheets)}.",
            _ => Action
        };
    }

    public static PrintResult NoPrint (string action)
    {
        return new PrintResult (action, 0, "", "", "", "", "", "", "");
    }

    private static string Plural (int count)
    {
        return count == 1 ? "" : "s";
    }
}
