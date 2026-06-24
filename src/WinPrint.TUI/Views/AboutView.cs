using System.Reflection;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace WinPrint.TUI.Views;

/// <summary>
///     The "about" footer: a help/about <see cref="Link" />
///     to the winprint home page plus the product version. The version is read from the entry
///     assembly's informational or assembly version at runtime. <see cref="Link" /> opens the URL
///     itself when activated.
/// </summary>
public sealed class AboutView : View
{
    /// <summary>winprint home / help page.</summary>
    public const string HomePageUrl = "https://tig.github.io/winprint";

    /// <summary>Creates the about footer.</summary>
    /// <param name="version">
    ///     Version text to display (without the leading <c>v</c>). Defaults to the runtime product
    ///     version; pass a fixed value for deterministic rendering (e.g. golden tests).
    /// </param>
    public AboutView(string? version = null)
    {
        Width = Dim.Fill();
        Height = Dim.Auto(DimAutoStyle.Content);
        SuperViewRendersLineCanvas = true;

        var help = new Link
        {
            X = 0,
            Y = 0,
            Text = "Help & about…",
            Url = HomePageUrl
        };

        var versionLabel = new Label
        {
            X =0,
            Y = 1,
            Text = $"v{version ?? ProductVersion()}"
        };

        Add(help, versionLabel);
    }

    /// <summary>The product version string shown in the footer.</summary>
    public static string ProductVersion()
    {
        Assembly assembly = Assembly.GetEntryAssembly() ?? typeof(AboutView).Assembly;

        string? informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            // SourceLink appends "+<git-sha>"; show just the human version before it.
            int plus = informational.IndexOf('+', StringComparison.Ordinal);
            return plus >= 0 ? informational[..plus] : informational;
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }
}
