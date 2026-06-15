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
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void CurrentDirectory_IgnoresRemovedDirectory()
    {
        string directory = CreateTempDirectory();
        var folder = new OpenFilePickerFolder();
        folder.RememberFile(Path.Combine(directory, "sample.txt"));

        Directory.Delete(directory, true);

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

            string pickerDirectory = await folder.RunFromRememberedDirectoryAsync(
                () => Task.FromResult(Environment.CurrentDirectory));

            Assert.Equal(rememberedDirectory, pickerDirectory);
            Assert.Equal(startDirectory, Environment.CurrentDirectory);
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
            Directory.Delete(startDirectory, true);
            Directory.Delete(rememberedDirectory, true);
        }
    }

    private static string CreateTempDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"winprint-picker-folder-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
