// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Helpers;
using Xunit;

namespace WinPrint.Core.UnitTests.Helpers;

/// <summary>
///     #263 — shell-agnostic expansion of <c>*</c>/<c>?</c> in positional file arguments.
/// </summary>
public class FileArgumentExpanderTests
{
    [Fact]
    public void NoWildcard_ReturnsLiteralUnchanged()
    {
        string path = Path.Combine(Path.GetTempPath(), "plain.txt");
        IReadOnlyList<string> result = FileArgumentExpander.Expand([path]);

        Assert.Equal([path], result);
    }

    [Fact]
    public void OneMatch_ExpandsToFullPath()
    {
        string dir = CreateTempDir();
        try
        {
            string file = Path.Combine(dir, "only.md");
            File.WriteAllText(file, "x");

            string pattern = Path.Combine(dir, "*.md");
            IReadOnlyList<string> result = FileArgumentExpander.Expand([pattern]);

            Assert.Single(result);
            Assert.Equal(Path.GetFullPath(file), result[0]);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ManyMatches_SortedOrdinal()
    {
        string dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "b.md"), "b");
            File.WriteAllText(Path.Combine(dir, "a.md"), "a");
            File.WriteAllText(Path.Combine(dir, "c.md"), "c");

            IReadOnlyList<string> result = FileArgumentExpander.Expand([Path.Combine(dir, "*.md")]);

            Assert.Equal(3, result.Count);
            Assert.Equal("a.md", Path.GetFileName(result[0]));
            Assert.Equal("b.md", Path.GetFileName(result[1]));
            Assert.Equal("c.md", Path.GetFileName(result[2]));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ZeroMatches_ThrowsClearError()
    {
        string dir = CreateTempDir();
        try
        {
            string pattern = Path.Combine(dir, "*.nope");
            InvalidOperationException ex =
                Assert.Throws<InvalidOperationException>(() => FileArgumentExpander.Expand([pattern]));

            Assert.Contains("No files matched", ex.Message);
            Assert.Contains(pattern, ex.Message);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void MixedLiteralAndGlob_PreservesOrder()
    {
        string dir = CreateTempDir();
        try
        {
            string literal = Path.Combine(dir, "z-literal.md");
            File.WriteAllText(literal, "z");
            File.WriteAllText(Path.Combine(dir, "a.md"), "a");
            File.WriteAllText(Path.Combine(dir, "b.md"), "b");

            IReadOnlyList<string> result = FileArgumentExpander.Expand(
            [
                literal,
                Path.Combine(dir, "*.md")
            ]);

            // Glob expands to a.md, b.md, z-literal.md (sorted) after the leading literal,
            // which is repeated if the glob also matches it — expand only the pattern slot.
            Assert.Equal(Path.GetFullPath(literal), result[0]);
            Assert.Contains(result.Skip(1), p => Path.GetFileName(p) == "a.md");
            Assert.Contains(result.Skip(1), p => Path.GetFileName(p) == "b.md");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void QuestionMarkWildcard_ExpandsSingleChar()
    {
        string dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "a1.md"), "1");
            File.WriteAllText(Path.Combine(dir, "a2.md"), "2");
            File.WriteAllText(Path.Combine(dir, "axx.md"), "xx"); // two chars after 'a' — must not match a?.md

            IReadOnlyList<string> result = FileArgumentExpander.Expand([Path.Combine(dir, "a?.md")]);

            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Matches(@"^a.\.md$", Path.GetFileName(p)));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void NestedDirectoryWildcard_Expands()
    {
        // CR: src/*/*.cs style — wildcard before the final segment.
        string root = CreateTempDir();
        try
        {
            string a = Path.Combine(root, "a");
            string b = Path.Combine(root, "b");
            Directory.CreateDirectory(a);
            Directory.CreateDirectory(b);
            File.WriteAllText(Path.Combine(a, "one.md"), "1");
            File.WriteAllText(Path.Combine(b, "two.md"), "2");
            File.WriteAllText(Path.Combine(root, "skip.md"), "x"); // not nested

            string pattern = Path.Combine(root, "*", "*.md");
            IReadOnlyList<string> result = FileArgumentExpander.Expand([pattern]);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => Path.GetFileName(p) == "one.md");
            Assert.Contains(result, p => Path.GetFileName(p) == "two.md");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void DoubleStar_RecursesSubdirectories()
    {
        string root = CreateTempDir();
        try
        {
            string deep = Path.Combine(root, "x", "y");
            Directory.CreateDirectory(deep);
            File.WriteAllText(Path.Combine(deep, "deep.md"), "d");
            File.WriteAllText(Path.Combine(root, "top.md"), "t");

            string pattern = Path.Combine(root, "**", "*.md");
            IReadOnlyList<string> result = FileArgumentExpander.Expand([pattern]);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => Path.GetFileName(p) == "deep.md");
            Assert.Contains(result, p => Path.GetFileName(p) == "top.md");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static string CreateTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "wp-glob-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
