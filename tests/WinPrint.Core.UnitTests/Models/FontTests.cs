using System.Text.Json;
using WinPrint.Core.Models;
using Xunit;
using Xunit.Abstractions;
using Font = WinPrint.Core.Models.Font;

namespace WinPrint.Core.UnitTests.Models;

public class FontTests : TestModelsBase
{
    public FontTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void TestFont()
    {
        var font = new Font();
        Assert.Equal(8, font.Size);
        Assert.Equal(FontStyle.Regular, font.Style);
        Assert.Equal("sansserif", font.Family);
    }

    [Fact]
    public void TestSetFamily()
    {
        var font = new Font
        {
            Family = "Cascadia Code"
        };
        Assert.Equal("Cascadia Code", font.Family);
    }

    [Fact]
    public void TestSetSize()
    {
        var font = new Font();
        Assert.Equal(8, font.Size);

        font.Size = 10;
        Assert.Equal(10, font.Size);
    }

    [Fact]
    public void TestSetStyle()
    {
        var font = new Font();
        Assert.Equal(8, font.Size);

        font.Style = FontStyle.Italic;
        Assert.Equal(FontStyle.Italic, font.Style);

        //font.Style = (FontStyle)88;
        //Assert.AreEqual(FontStyle.Regular, font.Style, "Invalid style was not converted properly");
    }

    [Fact]
    public void Style_AcceptsDefinedFlagCombinations()
    {
        var font = new Font
        {
            Style = FontStyle.Bold | FontStyle.Underline
        };

        Assert.Equal(FontStyle.Bold | FontStyle.Underline, font.Style);
    }

    [Fact]
    public void TestPersistence()
    {
        var font = new Font();

        string json = JsonSerializer.Serialize(font, jsonOptions);

        Assert.NotNull(json);
        Assert.True(json.Length > 0);

        Font? font2 = JsonSerializer.Deserialize<Font>(json);
        Assert.NotNull(font2);
        Assert.Equal(font.Family, font2.Family);
        Assert.Equal(font.Size, font2.Size);
        Assert.Equal(font.Style, font2.Style);
    }

    [Fact]
    public void TestDeserialize()
    {
        // Defaults
        string json = "{\"Family\":\"Microsoft Sans Serif\",\"Style\":\"Regular\",\"Size\":8}";
        Font? font = JsonSerializer.Deserialize<Font>(json, jsonOptions);
        Assert.NotNull(font);
        Assert.Equal("Microsoft Sans Serif", font.Family);
        Assert.Equal(8, font.Size);
        Assert.Equal(FontStyle.Regular, font.Style);

        // Non Defaults
        json = "{\"Family\":\"Cascadia Code\",\"Style\":\"Italic\",\"Size\":10}";
        font = JsonSerializer.Deserialize<Font>(json, jsonOptions);
        Assert.NotNull(font);
        Assert.Equal("Cascadia Code", font.Family);
        Assert.Equal(10, font.Size);
        Assert.Equal(FontStyle.Italic, font.Style);

        // Numeric enum value
        json = "{\"Family\":\"Microsoft Sans Serif\",\"Style\":1,\"Size\":8}";
        font = JsonSerializer.Deserialize<Font>(json, jsonOptions);
        Assert.NotNull(font);
        Assert.Equal(FontStyle.Bold, font.Style);

        // Camel casing
        json = "{\"family\":\"Cascadia Code\",\"style\":\"Italic\",\"size\":10}";
        font = JsonSerializer.Deserialize<Font>(json, jsonOptions);
        Assert.NotNull(font);
        Assert.Equal("Cascadia Code", font.Family);
        Assert.Equal(10, font.Size);
        Assert.Equal(FontStyle.Italic, font.Style);

        // Mixed casing
        json = "{\"FAMILY\":\"Cascadia Code\",\"STYLE\":\"Italic\",\"SIzE\":10}";
        font = JsonSerializer.Deserialize<Font>(json, jsonOptions);
        Assert.NotNull(font);
        Assert.Equal("Cascadia Code", font.Family);
        Assert.Equal(10, font.Size);
        Assert.Equal(FontStyle.Italic, font.Style);
    }
}
