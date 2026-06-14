using System;
using System.IO;
using System.Threading.Tasks;
using WinPrint.Core.Models;
using WinPrint.Core.ViewModels;
using WinPrint.TUI;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Regression coverage for the TUI's file → live-preview path. Both entry points — the command-line
///     file argument (<c>wp file.cs</c>) and the File… button — funnel through
///     <see cref="AppViewModel.LoadFileAsync" /> on the <see cref="SettingsContext" />'s app view model.
///     This exercises that real path (not a hand-built <see cref="Options" />) so a broken
///     measurement-context wiring — which makes the engine fail to load and silently reset
///     <see cref="AppViewModel.ActiveFile" /> back to "&lt;no file&gt;" — is caught.
/// </summary>
public class FilePreviewLoadTests
{
    private const string SampleCSharp =
        "using System;\n\nnamespace Demo;\n\npublic class Hello\n{\n" +
        "    public static void Main()\n    {\n        Console.WriteLine(\"Hello, WinPrint!\");\n    }\n}\n";

    [Fact]
    public async Task SettingsContext_LoadFile_RendersPreview()
    {
        string file = Path.Combine(Path.GetTempPath(), $"wp_preview_{Guid.NewGuid():N}.cs");
        await File.WriteAllTextAsync(file, SampleCSharp);
        try
        {
            // The same path `wp file.cs` takes: options → SettingsContext → bound AppViewModel.
            SettingsContext context = SettingsContext.Create(new Options { Files = [file] });
            AppViewModel app = context.App;
            Assert.Equal(file, context.File);

            // The same call the File… button makes (SettingsPanel.OpenFile → App.LoadFileAsync).
            bool loaded = await app.LoadFileAsync(context.File!);

            Assert.True(loaded, "LoadFileAsync should succeed for a real file (CLI arg and File button both use it).");
            Assert.True(app.IsFileLoaded, "ActiveFile should remain set after a successful load (not reset to <no file>).");
            Assert.True(app.TotalPages > 0, "A loaded file should reflow to at least one sheet for the preview.");
        }
        finally
        {
            File.Delete(file);
        }
    }
}
