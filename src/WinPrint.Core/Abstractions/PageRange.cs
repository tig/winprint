namespace WinPrint.Core.Abstractions;

/// <summary>
///     The inclusive range of sheets to print, mirroring the UI From/To inputs. <see cref="From" />
///     is 1-based; <see cref="To" /> of <c>0</c> means "to the end" (print all remaining sheets).
/// </summary>
public sealed class PageRange
{
    /// <summary>First sheet to print (1-based). Defaults to 1.</summary>
    public int From { get; set; } = 1;

    /// <summary>Last sheet to print, or <c>0</c> for "all remaining". Defaults to 0.</summary>
    public int To { get; set; }

    /// <summary><see langword="true" /> when <see cref="To" /> is 0, i.e. print through the last sheet.</summary>
    public bool IsToEnd => To == 0;
}
