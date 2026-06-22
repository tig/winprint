using WinPrint.TUI.Views;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Verifies <see cref="ConfigEditorDialog" />'s JSON validation gate (issue #166): the editor must
///     accept what the config loader accepts (well-formed JSON, plus trailing commas / <c>//</c>
///     comments and an empty file) and reject malformed JSON with a message.
/// </summary>
public class ConfigEditorDialogTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("{ \"copies\": 1 }")]
    [InlineData("{ \"copies\": 1, }")] // trailing comma — allowed by the loader
    [InlineData("// a comment\n{}")] // line comment — skipped by the loader
    [InlineData("")] // empty file is valid (loader falls back to defaults)
    [InlineData("   ")] // whitespace-only is treated as empty
    public void TryValidateJson_AcceptsValidConfig(string text)
    {
        bool ok = ConfigEditorDialog.TryValidateJson(text, out string? error);

        Assert.True(ok);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{ \"copies\": }")]
    [InlineData("not json")]
    [InlineData("{ \"a\": 1 \"b\": 2 }")]
    public void TryValidateJson_RejectsMalformedJson(string text)
    {
        bool ok = ConfigEditorDialog.TryValidateJson(text, out string? error);

        Assert.False(ok);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
