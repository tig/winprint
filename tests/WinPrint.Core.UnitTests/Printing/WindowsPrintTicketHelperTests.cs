// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Printing;
using WinPrint.Maui.Services;
using Xunit;

namespace WinPrint.Core.UnitTests.Printing;

/// <summary>
///     #267 — XPS print ticket gets orientation only (no speculative PageMediaSize).
/// </summary>
public class WindowsPrintTicketHelperTests
{
    [Fact]
    public void ApplyOrientation_Landscape()
    {
        var ticket = new PrintTicket();

        WindowsPrintTicketHelper.ApplyOrientation(ticket, true);

        Assert.Equal(PageOrientation.Landscape, ticket.PageOrientation);
    }

    [Fact]
    public void ApplyOrientation_Portrait()
    {
        var ticket = new PrintTicket { PageOrientation = PageOrientation.Landscape };

        WindowsPrintTicketHelper.ApplyOrientation(ticket, false);

        Assert.Equal(PageOrientation.Portrait, ticket.PageOrientation);
    }

    [Fact]
    public void ApplyOrientation_DoesNotForceUnknownMediaSize()
    {
        var ticket = new PrintTicket();
        PageMediaSize? before = ticket.PageMediaSize;

        WindowsPrintTicketHelper.ApplyOrientation(ticket, true);

        // Orientation-only helper must not invent PageMediaSizeName.Unknown dimensions.
        Assert.Equal(before?.PageMediaSizeName, ticket.PageMediaSize?.PageMediaSizeName);
    }
}
