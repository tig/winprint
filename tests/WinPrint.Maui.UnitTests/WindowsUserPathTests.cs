using WinPrint.Maui.Packaging;
using Xunit;

namespace WinPrint.Maui.UnitTests;

public class WindowsUserPathTests
{
    [Fact]
    public void AddPathEntryAppendsMissingEntry()
    {
        string updatedPath = WindowsUserPath.AddPathEntry(@"C:\Tools;C:\Windows",
            @"C:\Users\Tig\AppData\Local\Kindel.WinPrint\current");

        Assert.Equal(@"C:\Tools;C:\Windows;C:\Users\Tig\AppData\Local\Kindel.WinPrint\current", updatedPath);
    }

    [Fact]
    public void AddPathEntryDoesNotDuplicateEquivalentEntry()
    {
        string currentPath = @"C:\Tools;C:\Users\Tig\AppData\Local\Kindel.WinPrint\current\";

        string updatedPath =
            WindowsUserPath.AddPathEntry(currentPath, @"c:\users\tig\appdata\local\kindel.winprint\current");

        Assert.Equal(currentPath, updatedPath);
    }

    [Fact]
    public void RemovePathEntryRemovesEquivalentEntry()
    {
        string currentPath = @"C:\Tools;C:\Users\Tig\AppData\Local\Kindel.WinPrint\current\;C:\Windows";

        string updatedPath =
            WindowsUserPath.RemovePathEntry(currentPath, @"c:\users\tig\appdata\local\kindel.winprint\current");

        Assert.Equal(@"C:\Tools;C:\Windows", updatedPath);
    }
}
