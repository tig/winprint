// Copyright Kindel Systems, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Drawing;
using WinPrint.Core.Models;

namespace WinPrint.Core;

public class HeaderViewModel (SheetViewModel? svm, HeaderFooter? hf) : HeaderFooterViewModel (svm, hf)
{
    internal override RectangleF CalcBounds ()
    {
        var h = SheetViewModel.GetFontHeight (Font) + VerticalPadding;
        if (Enabled && _svm is { })
        {
            return new RectangleF (_svm.Bounds.Left + _svm.Margins.Left,
                _svm.Bounds.Top + _svm.Margins.Top,
                _svm.Bounds.Width - _svm.Margins.Left - _svm.Margins.Right,
                h);
        }

        return new RectangleF (0, 0, 0, 0);
    }

    internal override bool IsAlignTop ()
    {
        return true;
    }
}
