// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Helpers;
using Xunit;

namespace WinPrint.Core.UnitTests.Helpers;

public class OpenFilePickerFolderTests
{
    [Fact]
    public void RememberFile_StoresExistingParentDirectory()
    {
        string directory = CreateTempDirectory();
        try
        {
            string file = Path.Combine(directory, "sample.txt");
            File.WriteAllText(file, "sample");
            var folder = new OpenFilePickerFolder();

            folder.RememberFile(file);

            Assert.Equal(directory, folder.CurrentDirectory);
        }
        finally
        {
            DeleteIfExists(directory);
        }
    }

    [Fact]
    public void CurrentDirectory_IgnoresRemovedDirectory()
    {
        string directory = CreateTempDirectory();
        try
        {
            var folder = new OpenFilePickerFolder();
            folder.RememberFile(Path.Combine(directory, "sample.txt"));

            DeleteIfExists(directory);

            Assert.Null(folder.CurrentDirectory);
        }
        finally
        {
            DeleteIfExists(directory);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\0invalid")]
    public void RememberFile_PathologicalInput_DoesNotThrowAndRemembersNothing(string filePath)
    {
        var folder = new OpenFilePickerFolder();

        folder.RememberFile(filePath); // must not throw
        Assert.Null(folder.CurrentDirectory);
    }

    [Fact]
    public void RememberFile_OverlongPath_DoesNotThrow()
    {
        var folder = new OpenFilePickerFolder();

        // An absurdly long path can make Path.GetFullPath throw PathTooLongException (an IOException) on
        // some platforms; RememberFile must swallow it rather than crash the caller.
        string overlong = Path.Combine(Path.GetTempPath(), new string('a', 5000), "file.txt");

        folder.RememberFile(overlong); // must not throw
        Assert.Null(folder.CurrentDirectory);
    }

    [Fact]
    public async Task RunFromRememberedDirectoryAsync_UsesRememberedDirectoryAndRestoresOriginal()
    {
        string originalDirectory = Environment.CurrentDirectory;
        string startDirectory = CreateTempDirectory();
        string rememberedDirectory = CreateTempDirectory();
        try
        {
            Environment.CurrentDirectory = startDirectory;
            var folder = new OpenFilePickerFolder();
            folder.RememberFile(Path.Combine(rememberedDirectory, "sample.txt"));

            string pickerDirectory =
                await folder.RunFromRememberedDirectoryAsync(() => Task.FromResult(Environment.CurrentDirectory));

            // Compare canonical forms — setting Environment.CurrentDirectory resolves symlinks (e.g. on
            // macOS /var -> /private/var), so the raw temp paths won't match the read-back value.
            Assert.Equal(Canonicalize(rememberedDirectory), pickerDirectory);
            Assert.Equal(Canonicalize(startDirectory), Environment.CurrentDirectory);
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
            DeleteIfExists(startDirectory);
            DeleteIfExists(rememberedDirectory);
        }
    }

    [Fact]
    public async Task RunFromRememberedDirectoryAsync_WhenActionThrows_PropagatesOriginalException()
    {
        string originalDirectory = Environment.CurrentDirectory;
        string rememberedDirectory = CreateTempDirectory();
        try
        {
            var folder = new OpenFilePickerFolder();
            folder.RememberFile(Path.Combine(rememberedDirectory, "sample.txt"));

            // The action's exception must propagate unchanged — the best-effort directory restore in the
            // finally must never throw and mask it.
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                folder.RunFromRememberedDirectoryAsync<int>(() => throw new InvalidOperationException("boom")));

            Assert.Equal("boom", ex.Message);
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
            DeleteIfExists(rememberedDirectory);
        }
    }

    private static string CreateTempDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"winprint-picker-folder-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteIfExists(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    // Returns the directory in the canonical form the OS reports (resolves symlinks), matching what
    // reading Environment.CurrentDirectory yields after setting it.
    private static string Canonicalize(string directory)
    {
        string previous = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = directory;
            return Environment.CurrentDirectory;
        }
        finally
        {
            Environment.CurrentDirectory = previous;
        }
    }
}
