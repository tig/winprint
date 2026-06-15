// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Core.Helpers;

public sealed class OpenFilePickerFolder
{
    private string? _currentDirectory;

    public string? CurrentDirectory =>
        string.IsNullOrEmpty(_currentDirectory) || !Directory.Exists(_currentDirectory)
            ? null
            : _currentDirectory;

    public void RememberFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            string? directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                _currentDirectory = directory;
            }
        }
        catch (ArgumentException)
        {
        }
        catch (NotSupportedException)
        {
        }
    }

    public async Task<T> RunFromRememberedDirectoryAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        string? directory = CurrentDirectory;
        if (directory is null)
        {
            return await action().ConfigureAwait(false);
        }

        string originalDirectory = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = directory;
            return await action().ConfigureAwait(false);
        }
        finally
        {
            if (Directory.Exists(originalDirectory))
            {
                Environment.CurrentDirectory = originalDirectory;
            }
        }
    }
}
