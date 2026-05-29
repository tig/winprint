// Copyright Kindel Systems, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Drawing;
using WinPrint.Core.Models;

namespace WinPrint.Core;

public class FooterViewModel(SheetViewModel svm, HeaderFooter hf) : HeaderFooterViewModel(svm, hf)
{
    internal override RectangleF CalcBounds()
    {
        float h = SheetViewModel.GetFontHeight(Font) + VerticalPadding;
        if (!Enabled)
        {
            return new RectangleF(0, 0, 0, 0);
        }

        if (_svm != null)
        {
            return new RectangleF(_svm.Bounds.Left + _svm.Margins.Left,
                _svm.Bounds.Bottom - _svm.Margins.Bottom - h,
                _svm.Bounds.Width - _svm.Margins.Left - _svm.Margins.Right,
                h);
        }

        return new RectangleF(0, 0, 0, 0);
    }

    internal override bool IsAlignTop()
    {
        return false;
    }
}
