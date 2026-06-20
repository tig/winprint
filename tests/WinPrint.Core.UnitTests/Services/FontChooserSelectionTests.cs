using WinPrint.Core.Services;
using Xunit;

namespace WinPrint.Core.UnitTests.Services;

public class FontChooserSelectionTests
{
    [Fact]
    public void SelectVisibleFamily_PreservesCurrentFamilyWhenVisible()
    {
        string selection = FontChooserSelection.SelectVisibleFamily(["Consolas", "Cascadia Mono"], "Consolas");

        Assert.Equal("Consolas", selection);
    }

    [Fact]
    public void SelectVisibleFamily_ReplacesCurrentFamilyWhenFilterHidesIt()
    {
        string selection = FontChooserSelection.SelectVisibleFamily(["Cascadia Mono", "Courier New"], "Arial");

        Assert.Equal("Cascadia Mono", selection);
    }

    [Fact]
    public void SelectVisibleFamily_ClearsSelectionWhenNoFamiliesAreVisible()
    {
        string selection = FontChooserSelection.SelectVisibleFamily([], "Arial");

        Assert.Equal(string.Empty, selection);
    }
}
