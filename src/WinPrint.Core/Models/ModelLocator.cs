using WinPrint.Core.Services;

namespace WinPrint.Core.Models;

public class ModelLocator
{
    private static ModelLocator? s_current;

    private ModelLocator()
    {
    }

    public static ModelLocator Current => s_current ??= new ModelLocator();

    private static WinPrintServices Services => WinPrintServices.Current;

    public Settings Settings => Services.Settings!;

    public Options Options => Services.Options;

    public FileTypeMapping FileTypeMapping => Services.FileTypeMapping;

    public static void Reset()
    {
        s_current = null;
        WinPrintServices.Reset();
    }
}