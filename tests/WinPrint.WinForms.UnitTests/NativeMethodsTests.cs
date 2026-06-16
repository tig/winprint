using System.Runtime.InteropServices;
using WinPrint.WinForms;
using Xunit;

namespace WinPrint.WinForms.UnitTests;

/// <summary>
///     Unit tests for <see cref="NativeMethods" /> — the shell file-association P/Invoke wrapper. These
///     verify the two-call buffer-sizing marshalling pattern against the real Win32 <c>AssocQueryString</c>,
///     so they only run on Windows (the WinForms front end is Windows-only anyway).
/// </summary>
public class NativeMethodsTests
{
    [Fact]
    public void FileExtentionInfo_KnownExtension_ReturnsNonNullWithoutThrowing()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // Windows-only shell API.
        }

        // The wrapper does a size query then a fetch; a marshalling bug would throw or truncate.
        string friendlyName = NativeMethods.FileExtentionInfo(AssocStr.FriendlyDocName, ".txt");

        Assert.NotNull(friendlyName);
    }

    [Fact]
    public void FileExtentionInfo_UnregisteredExtension_DoesNotThrow()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string result = NativeMethods.FileExtentionInfo(AssocStr.Executable, ".winprint-no-such-assoc");

        Assert.NotNull(result);
    }
}
