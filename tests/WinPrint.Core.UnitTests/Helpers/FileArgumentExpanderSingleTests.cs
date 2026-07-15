// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Helpers;
using Xunit;

namespace WinPrint.Core.UnitTests.Helpers;

/// <summary>
///     <see cref="FileArgumentExpander.ExpandSingle" /> — TUI opens exactly one file (#263 review).
/// </summary>
public class FileArgumentExpanderSingleTests
{
    [Fact]
    public void ExpandSingle_OneMatch_ReturnsPath()
    {
        string dir = CreateTempDir();
        try
        {
            string file = Path.Combine(dir, "only.md");
            File.WriteAllText(file, "x");

            string result = FileArgumentExpander.ExpandSingle([Path.Combine(dir, "*.md")]);

            Assert.Equal(Path.GetFullPath(file), result);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ExpandSingle_ManyMatches_Throws()
    {
        string dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "a.md"), "a");
            File.WriteAllText(Path.Combine(dir, "b.md"), "b");

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                FileArgumentExpander.ExpandSingle([Path.Combine(dir, "*.md")]));

            Assert.Contains("2 files", ex.Message);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ExpandSingle_TwoLiterals_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            FileArgumentExpander.ExpandSingle(["a.md", "b.md"]));

        Assert.Contains("2 files", ex.Message);
    }

    private static string CreateTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "wp-glob1-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
