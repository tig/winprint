// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Abstractions;
using WinPrint.Core.ViewModels;
using Xunit;

namespace WinPrint.Core.UnitTests.ViewModels;

/// <summary>
///     Verifies the print-enablement predicate the MacCatalyst <b>File ▸ Print…</b> menu item is greyed
///     out by. <c>AppDelegate.BuildMenu</c> disables Print when <c>MainPage.CanPrint</c> is false, which
///     chains to <c>MainViewModel.PrintCommand.CanExecute =&gt; IsFileLoaded &amp;&amp; !IsBusy</c>. The
///     substance of that — <see cref="AppViewModel.IsFileLoaded" /> flipping false→true when a document
///     loads — is exercised here without a UI runtime.
///     <para>
///         This replaces the Appium <c>MacMenuUITests</c>, which drove the real native menu bar and
///         flaked: on a fresh CI desktop the app intermittently failed to become frontmost, so the tests
///         queried Finder's File menu instead of WinPrint's and hit <c>NoSuchElement</c> on Open…/Print….
///         See <c>src/WinPrint.Maui/Platforms/MacCatalyst/AppDelegate.cs</c>.
///     </para>
/// </summary>
public class AppViewModelPrintEnablementTests
{
    // No SheetViewModel => LoadFileAsync records the file without reflow (the GDI+-free front-end path),
    // so these stay deterministic and run on any OS.
    private static AppViewModel CreatePreviewlessVm() =>
        new(new PrintPageSetup { PaperWidth = 850, PaperHeight = 1100, DpiX = 96, DpiY = 96 });

    [Fact]
    public void IsFileLoaded_IsFalseAtStartup_SoPrintIsDisabled()
    {
        AppViewModel vm = CreatePreviewlessVm();

        // No document open at startup => AppDelegate greys out File ▸ Print….
        Assert.False(vm.IsFileLoaded);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task IsFileLoaded_BecomesTrueAfterLoadingAFile_SoPrintEnables()
    {
        AppViewModel vm = CreatePreviewlessVm();
        string temp = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(temp, "content to print");

            bool loaded = await vm.LoadFileAsync(temp);

            Assert.True(loaded);
            Assert.True(vm.IsFileLoaded); // => File ▸ Print… becomes enabled.
            Assert.False(vm.IsBusy);
        }
        finally
        {
            File.Delete(temp);
        }
    }
}
