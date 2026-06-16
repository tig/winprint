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
        // RememberFile is best-effort: ignore any unusable path. Path.GetFullPath can throw
        // ArgumentException/NotSupportedException (invalid chars), PathTooLongException (an IOException),
        // SecurityException, or UnauthorizedAccessException — none should escape to the caller.
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException
                                       or System.Security.SecurityException or UnauthorizedAccessException)
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

        // Switching the process-wide current directory is best-effort: if we can't switch (directory
        // vanished, permission/IO error) still run the action from the original directory, and never let
        // the restore in the finally throw and mask the action's own exception.
        string originalDirectory = Environment.CurrentDirectory;
        bool switched = TrySetCurrentDirectory(directory);
        try
        {
            return await action().ConfigureAwait(false);
        }
        finally
        {
            if (switched)
            {
                TrySetCurrentDirectory(originalDirectory);
            }
        }
    }

    private static bool TrySetCurrentDirectory(string directory)
    {
        try
        {
            Environment.CurrentDirectory = directory;
            return true;
        }
        catch (Exception ex) when (ex is IOException or System.Security.SecurityException
                                       or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }
}
