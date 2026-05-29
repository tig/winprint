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

        if (Svm != null)
        {
            return new RectangleF(Svm.Bounds.Left + Svm.Margins.Left,
                Svm.Bounds.Bottom - Svm.Margins.Bottom - h,
                Svm.Bounds.Width - Svm.Margins.Left - Svm.Margins.Right,
                h);
        }

        return new RectangleF(0, 0, 0, 0);
    }

    internal override bool IsAlignTop()
    {
        return false;
    }
}
