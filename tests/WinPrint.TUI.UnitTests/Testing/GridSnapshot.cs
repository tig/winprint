using System.Runtime.CompilerServices;
using System.Text;
using Xunit.Sdk;

namespace WinPrint.TUI.UnitTests.Testing;

/// <summary>
///     Golden-file snapshot of a rendered screen as a plain-text character grid (e.g. from
///     <c>AppFixture.Screen</c>). The grid is the full set of cell glyphs for a frame, so it is stable
///     and diffable; convert one to a PNG for visual review with <c>scripts/grid2png.py</c>.
/// </summary>
/// <remarks>
///     First run records the golden under <c>__snapshots__/&lt;name&gt;.txt</c> and passes; later runs
///     compare. Accept an intended change by re-running with <c>UPDATE_SNAPSHOTS=1</c>. On mismatch the
///     failure shows the diff inline and writes a sibling <c>.txt.actual</c>.
/// </remarks>
public static class GridSnapshot
{
    private static bool UpdateRequested =>
        Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") is "1" or "true";

    /// <summary>Compares a captured <paramref name="grid" /> render against the golden named <paramref name="name" />.</summary>
    /// <param name="grid">The captured character grid (e.g. <c>AppFixture.Screen</c>).</param>
    /// <param name="name">Stable snapshot name (becomes <c>&lt;name&gt;.txt</c>).</param>
    /// <param name="callerFile">Compiler-supplied; locates <c>__snapshots__/</c> in the test project.</param>
    public static void Verify(string grid, string name, [CallerFilePath] string callerFile = "")
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string actual = Canonicalize(grid);
        string dir = SnapshotDir(callerFile);
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, name + ".txt");

        if (UpdateRequested || !File.Exists(path))
        {
            WriteRaw(path, actual);
            return;
        }

        string expected = Canonicalize(File.ReadAllText(path));

        if (string.Equals(expected, actual, StringComparison.Ordinal))
        {
            return;
        }

        string actualPath = path + ".actual";
        WriteRaw(actualPath, actual);

        throw new XunitException(
            $"""
             Grid snapshot '{name}' does not match {path}.

             Expected:
             ----------------------------------------------------------------------
             {expected}
             ----------------------------------------------------------------------
             Actual:
             ----------------------------------------------------------------------
             {actual}
             ----------------------------------------------------------------------

             Wrote the actual render to: {actualPath}
             If this change is intended, accept it by re-running with UPDATE_SNAPSHOTS=1.
             """);
    }

    // Walk up from the caller source file to the directory containing the test .csproj, so goldens
    // always resolve to <project>/__snapshots__ regardless of where the helper file lives.
    private static string SnapshotDir(string callerFile)
    {
        string? dir = Path.GetDirectoryName(callerFile);

        while (!string.IsNullOrEmpty(dir))
        {
            if (Directory.EnumerateFiles(dir, "*.csproj").Any())
            {
                return Path.Combine(dir, "__snapshots__");
            }

            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException(
            $"Could not locate the test project directory from caller path '{callerFile}'.");
    }

    private static string Canonicalize(string? text)
    {
        return (text ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private static void WriteRaw(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, new UTF8Encoding(false));
    }
}
