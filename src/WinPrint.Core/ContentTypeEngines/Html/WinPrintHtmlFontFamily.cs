// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using TheArtOfDev.HtmlRenderer.Adapters;

namespace WinPrint.Core.ContentTypeEngines.Html;

/// <summary>A font family name for the HtmlRenderer adapter.</summary>
internal sealed class WinPrintHtmlFontFamily : RFontFamily
{
    public WinPrintHtmlFontFamily(string name)
    {
        Name = name;
    }

    public override string Name { get; }
}
