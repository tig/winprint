// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace WinPrint.Maui;

/// <summary>
///     Swaps the GraphicsView's platform view for <see cref="FocusablePlatformGraphicsView" />
///     so the print preview can take keyboard focus. Registered in MauiProgram.
/// </summary>
internal sealed class FocusableGraphicsViewHandler : GraphicsViewHandler
{
    protected override PlatformTouchGraphicsView CreatePlatformView()
    {
        return new FocusablePlatformGraphicsView();
    }
}
