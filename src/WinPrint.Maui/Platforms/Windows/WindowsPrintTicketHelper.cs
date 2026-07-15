using System.Printing;

namespace WinPrint.Maui.Services;

/// <summary>
///     Applies winprint page-setup flags to a <see cref="PrintTicket" /> for XPS spooling (#267).
///     Orientation only — media size is left to the queue so picky drivers (Brother IPP class, etc.)
///     are not forced onto <see cref="PageMediaSizeName.Unknown" />.
/// </summary>
public static class WindowsPrintTicketHelper
{
    public static void ApplyOrientation(PrintTicket ticket, bool landscape)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        ticket.PageOrientation = landscape
            ? PageOrientation.Landscape
            : PageOrientation.Portrait;
    }
}
