using System;
using System.Drawing;
using Serilog;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.ViewModels;
using Font = WinPrint.Core.Models.Font;

namespace WinPrint.Core;

/// <summary>
///     Knows how to paint a header or footer.
/// </summary>
// TODO: Add a Padding property to provide padding below bottom of header/above top of footer
public abstract class HeaderFooterViewModel : ViewModelBase, IDisposable
{
    private bool _bottomBorder;

    // Protected implementation of Dispose pattern.
    // Flag: Has Dispose already been called?
    private bool _disposed;
    private bool _enabled;
    private Font? _font;
    private bool _leftBorder;
    private bool _rightBorder;

    internal SheetViewModel? _svm;

    private string? _text;

    private bool _topBorder;

    // TODO: Make settable
    private int _verticalPadding = 10; // Vertical padding below/above header/footer in 100ths of inch

    /// <inheritdoc />
    protected HeaderFooterViewModel (SheetViewModel? svm, HeaderFooter? hf)
    {
        if (hf is null)
        {
            throw new ArgumentNullException (nameof (hf));
        }

        _svm = svm ?? throw new ArgumentNullException (nameof (svm));

        Text = hf.Text;
        LeftBorder = hf.LeftBorder;
        RightBorder = hf.RightBorder;
        TopBorder = hf.TopBorder;
        BottomBorder = hf.BottomBorder;

        // Font can be null (provided by Sheet definition)
        if (hf.Font != null)
        {
            Font = (Font)hf.Font.Clone ();
        }

        Enabled = hf.Enabled;
        VerticalPadding = hf.VerticalPadding;

        // Wire up changes from Header / Footer models
        hf.PropertyChanged += (s, e) =>
        {
            var reflow = false;
            switch (e.PropertyName)
            {
                case "Text":
                    Text = hf.Text;
                    break;
                case "LeftBorder":
                    LeftBorder = hf.LeftBorder;
                    break;
                case "RightBorder":
                    RightBorder = hf.RightBorder;
                    break;
                case "TopBorder":
                    TopBorder = hf.TopBorder;
                    break;
                case "BottomBorder":
                    BottomBorder = hf.BottomBorder;
                    break;
                case "Font":
                    Font = hf.Font;
                    reflow = true;
                    break;
                case "Enabled":
                    Enabled = hf.Enabled;
                    reflow = true;
                    break;
                case "VerticalPadding":
                    VerticalPadding = hf.VerticalPadding;
                    reflow = true;
                    break;
                default:
                    throw new InvalidOperationException ($"Property change not handled: {e.PropertyName}");
            }

            OnSettingsChanged (reflow);
        };
    }

    public string? Text { get => _text; set => SetField (ref _text, value); }

    /// <summary>
    ///     Font used for header or footer text
    /// </summary>
    public Font? Font { get => _font; set => SetField (ref _font, value); }

    /// <summary>
    ///     Enables or disables printing of left border of heder/footer
    /// </summary>
    public bool LeftBorder { get => _leftBorder; set => SetField (ref _leftBorder, value); }

    /// <summary>
    ///     Enables or disables printing of Top border of heder/footer
    /// </summary>
    public bool TopBorder { get => _topBorder; set => SetField (ref _topBorder, value); }

    /// <summary>
    ///     Enables or disables printing of Right border of heder/footer
    /// </summary>
    public bool RightBorder { get => _rightBorder; set => SetField (ref _rightBorder, value); }

    /// <summary>
    ///     Enables or disables printing of Bottom border of heder/footer
    /// </summary>
    public bool BottomBorder { get => _bottomBorder; set => SetField (ref _bottomBorder, value); }

    /// <summary>
    ///     Enable or disable header/footer
    /// </summary>
    public bool Enabled { get => _enabled; set => SetField (ref _enabled, value); }

    public int VerticalPadding { get => _verticalPadding; set => SetField (ref _verticalPadding, value); }

    /// <summary>
    ///     Header/Footer bounds (page minus margins)
    /// </summary>
    public RectangleF Bounds => CalcBounds ();

    public void Dispose ()
    {
        Dispose (true);
        GC.SuppressFinalize (this);
    }

    protected virtual void Dispose (bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            //if (Font != null) Font.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    ///     Calculate the Header or Footer bounds (position and size on sheet) based on containing document and font size.
    /// </summary>
    /// <returns></returns>
    internal abstract RectangleF CalcBounds ();

    internal abstract bool IsAlignTop ();

    public void Paint (IGraphicsContext g, int sheetNum)
    {
        if (!Enabled)
        {
            return;
        }

        var boundsHF = CalcBounds ();
        boundsHF.Y += IsAlignTop () ? 0 : _verticalPadding;
        boundsHF.Height -= _verticalPadding;

        if (LeftBorder)
        {
            g.DrawLine (g.BlackPen, boundsHF.Left, boundsHF.Top, boundsHF.Left, boundsHF.Bottom);
        }

        if (TopBorder)
        {
            g.DrawLine (g.BlackPen, boundsHF.Left, boundsHF.Top, boundsHF.Right, boundsHF.Top);
        }

        if (RightBorder)
        {
            g.DrawLine (g.BlackPen, boundsHF.Right, boundsHF.Top, boundsHF.Right, boundsHF.Bottom);
        }

        if (BottomBorder)
        {
            g.DrawLine (g.BlackPen, boundsHF.Left, boundsHF.Bottom, boundsHF.Right, boundsHF.Bottom);
        }

        Log.Debug ($"{GetType ().Name}: Expanding Macros - {Text}");
        var macros = new Macros (_svm) { Page = sheetNum };
        var parts = macros.ReplaceMacros (Text).Split ('\t', '|');

        // Left\tCenter\tRight
        if (parts.Length == 0)
        {
            return;
        }

        using var tempFont = CreateTempFont (g);

        var fmt = new GraphicsStringFormat
        {
            Trimming = GraphicsStringTrimming.None,
            // BUGBUG: This is a work around for https://stackoverflow.com/questions/59159919/stringformat-trimming-changes-vertical-placement-of-text
            //         (turning on NoWrap). 
            FormatFlags = GraphicsStringFormatFlags.LineLimit | GraphicsStringFormatFlags.NoWrap | GraphicsStringFormatFlags.NoClip
        };

        fmt.LineAlignment = IsAlignTop () ? GraphicsTextAlignment.Near : GraphicsTextAlignment.Far;

        // Center goes first - it has priority - ensure it gets drawn completely where
        // Left & Right can be trimmed
        var sizeCenter = new GraphicsSizeF (0, 0);
        var boundsRect = new GraphicsRectF (boundsHF.X, boundsHF.Y, boundsHF.Width, boundsHF.Height);

        if (parts.Length > 1)
        {
            fmt.Alignment = GraphicsTextAlignment.Center;
            sizeCenter = g.MeasureString (parts[1], tempFont, (int)boundsHF.Width, fmt);
            // g.DrawRectangle(Pens.Purple, boundsHF.Left, boundsHF.Top, boundsHF.Width, boundsHF.Height);
            g.DrawString (parts[1], tempFont, g.BlackBrush, boundsRect, fmt);
        }

        // Left
        // Remove the space taken up by the center from the bounds
        var textCenterBounds = (boundsHF.Width - sizeCenter.Width) / 2;

        var boundsLeft = new RectangleF (boundsHF.X, boundsHF.Y, textCenterBounds, boundsHF.Height);
        var sizeLeft = g.MeasureString (parts[0], tempFont, (int)textCenterBounds, fmt);

        fmt.Alignment = GraphicsTextAlignment.Near;
        fmt.Trimming = GraphicsStringTrimming.None;
        //g.DrawRectangle(Pens.Orange, boundsLeft.X, boundsLeft.Y, boundsLeft.Width, boundsLeft.Height);
        g.DrawString (parts[0], tempFont, g.BlackBrush,
            new GraphicsRectF (boundsLeft.X, boundsLeft.Y, boundsLeft.Width, boundsLeft.Height), fmt);

        //Right
        var boundsRight = new RectangleF (boundsHF.X + (boundsHF.Width - textCenterBounds), boundsHF.Y, textCenterBounds,
            boundsHF.Height);
        if (parts.Length > 2)
        {
            fmt.Alignment = GraphicsTextAlignment.Far;
            fmt.Trimming = GraphicsStringTrimming.None;
            //g.DrawRectangle(Pens.Blue, boundsRight.X, boundsRight.Y, boundsRight.Width, boundsRight.Height);
            g.DrawString (parts[2], tempFont, g.BlackBrush,
                new GraphicsRectF (boundsRight.X, boundsRight.Y, boundsRight.Width, boundsRight.Height), fmt);
        }
    }

    /// <summary>
    ///     Get a font suitable for printing or preview. If no font was specified just return default system font.
    /// </summary>
    /// <param name="g"></param>
    /// <returns></returns>
    private IGraphicsFont CreateTempFont (IGraphicsContext g)
    {
        if (Font == null)
        {
            return g.CreateFont ("sansserif", 8F, GraphicsFontStyle.Regular, GraphicsFontUnit.Point);
        }

        var tempFont = g.IsDisplayUnit
            ? g.CreateFont (Font.Family, Font.Size, SystemDrawingAdapters.ToGraphicsFontStyle (Font.Style), GraphicsFontUnit.Point)
            : g.CreateFont (Font.Family, Font.Size / 72F * 96F, SystemDrawingAdapters.ToGraphicsFontStyle (Font.Style), GraphicsFontUnit.Pixel);

        return tempFont;
    }


    // if bool is true, reflow. Otherwise just paint
    public event EventHandler<bool>? SettingsChanged;

    protected void OnSettingsChanged (bool reflow)
    {
        SettingsChanged?.Invoke (this, reflow);
    }
}
