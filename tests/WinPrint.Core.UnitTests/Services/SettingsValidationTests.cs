using WinPrint.Core.Services;
using Xunit;

namespace WinPrint.Core.UnitTests.Services;

/// <summary>
///     Verifies <see cref="SettingsService.TryValidateSettingsJson" /> — the gate the TUI config editor
///     uses (issues #166/#85) — accepts what the loader accepts (well-formed JSON, trailing commas,
///     <c>//</c> comments, empty) and rejects what it can't load, with a message.
/// </summary>
public class SettingsValidationTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("{ \"defaultContentType\": \"text/plain\" }")]
    [InlineData("{ \"defaultContentType\": \"text/plain\", }")] // trailing comma — allowed by the loader
    [InlineData("// a comment\n{}")] // line comment — skipped by the loader
    [InlineData("")] // empty file is valid (loader falls back to defaults)
    [InlineData("   ")] // whitespace-only is treated as empty
    public void TryValidateSettingsJson_AcceptsLoadableConfig(string text)
    {
        bool ok = SettingsService.TryValidateSettingsJson(text, out string? error);

        Assert.True(ok);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{ \"defaultContentType\": }")]
    [InlineData("not json")]
    [InlineData("{ \"a\": 1 \"b\": 2 }")]
    public void TryValidateSettingsJson_RejectsUnloadableJson(string text)
    {
        bool ok = SettingsService.TryValidateSettingsJson(text, out string? error);

        Assert.False(ok);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
