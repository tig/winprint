// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using Microsoft.Maui.Platform;

namespace WinPrint.Maui;

/// <summary>
///     MAUI's stock GraphicsView platform view refuses first-responder status, so the
///     preview can never hold keyboard focus — clicks left focus stranded on whatever
///     sidebar control had it. This subclass accepts focus like any native view.
/// </summary>
internal sealed class FocusablePlatformGraphicsView : PlatformTouchGraphicsView
{
    public override bool CanBecomeFirstResponder => true;

    public override bool CanBecomeFocused => true;
}
