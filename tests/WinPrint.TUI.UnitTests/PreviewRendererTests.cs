using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.TUI.Services;
using Xunit;

namespace WinPrint.TUI.UnitTests;

public sealed class PreviewRendererTests
{
    private readonly PreviewRenderer _renderer = new();

    private static SheetSettings CreateDefaultSheet()
    {
        return new SheetSettings
        {
            Name = "Test",
            Rows = 1,
            Columns = 1,
            Padding = 0,
            Margins = new PrintMargins { Top = 100, Bottom = 100, Left = 75, Right = 75 },
            ContentSettings = new ContentSettings { Font = new Font { Family = "Consolas", Size = 8 } }
        };
    }

    [Fact]
    public async Task CountPagesAsync_NonExistentFile_ReturnsZero()
    {
        int count = await _renderer.CountPagesAsync("/nonexistent/file.txt", CreateDefaultSheet());
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task CountPagesAsync_EmptyFile_ReturnsAtLeastOne()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "");
            int count = await _renderer.CountPagesAsync(tempFile, CreateDefaultSheet());
            Assert.True(count >= 1);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CountPagesAsync_LargeFile_ReturnsMultiplePages()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            // Write 200 lines — should require multiple pages at ~60 lines/page
            string content = string.Join("\n", Enumerable.Range(1, 200).Select(i => $"Line {i}"));
            File.WriteAllText(tempFile, content);

            int count = await _renderer.CountPagesAsync(tempFile, CreateDefaultSheet());
            Assert.True(count > 1, $"Expected multiple pages, got {count}");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CountPagesAsync_MultiPageUp_ReducesSheetCount()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            string content = string.Join("\n", Enumerable.Range(1, 200).Select(i => $"Line {i}"));
            File.WriteAllText(tempFile, content);

            SheetSettings singleUp = CreateDefaultSheet();
            singleUp.Rows = 1;
            singleUp.Columns = 1;

            SheetSettings twoUp = CreateDefaultSheet();
            twoUp.Rows = 1;
            twoUp.Columns = 2;

            int singleCount = await _renderer.CountPagesAsync(tempFile, singleUp);
            int twoUpCount = await _renderer.CountPagesAsync(tempFile, twoUp);

            Assert.True(twoUpCount <= singleCount,
                $"2-up ({twoUpCount}) should have <= sheets than 1-up ({singleCount})");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task RenderPageAsync_ReturnsNull_InPhase1()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "Hello World");
            byte[]? result = await _renderer.RenderPageAsync(tempFile, CreateDefaultSheet(), 0);
            Assert.Null(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
