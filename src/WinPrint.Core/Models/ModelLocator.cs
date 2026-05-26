using GalaSoft.MvvmLight.Ioc;
using WinPrint.Core.Services;

//using WinPrint.Services;
//using WinPrint.Views;

namespace WinPrint.Core.Models;

public class ModelLocator
{
    private static ModelLocator? _current;

    private ModelLocator ()
    {
        // Register the models via the Services Factory
        SimpleIoc.Default.Register (SettingsService.Create);
        SimpleIoc.Default.Register (FileTypeMappingService.Create);
        SimpleIoc.Default.Register<Options> ();
    }

    public static ModelLocator Current => _current ??= new ModelLocator ();

    public Settings Settings => SimpleIoc.Default.GetInstance<Settings> ();

    public Options Options => SimpleIoc.Default.GetInstance<Options> ();
    public FileTypeMapping FileTypeMapping => SimpleIoc.Default.GetInstance<FileTypeMapping> ();

    public void Register<VM, V> ()
        where VM : class
    {
        SimpleIoc.Default.Register<VM> ();
    }

    public static void Reset ()
    {
        _current = null;
        SimpleIoc.Default.Unregister<Settings> ();
        SimpleIoc.Default.Unregister<FileTypeMapping> ();
        SimpleIoc.Default.Unregister<Options> ();
    }
}
