using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace WinPrint.Core.Models;

/// <summary>
///     Knows how to paint a header or footer.
///     Single line of text? TODO: Might want to suppport wrapping.
///     Format: A .NET Interpolated String. Two tabstops.
///     $
///     Three segement
///     Left Aligned       Centered          Right Aligned
///     Format: Left/Centered/Right can be delimited with either tab char (\t) or |
///     {FullyQualifiedPath}|Modified: {FileDate:F}|{Page:D3}/{NumPages}
///     {FullyQualifiedPath}\tModified: {FileDate:F}\t{Page:D3}/{NumPages}
///     Macros
///     DatePrinted
///     DateRevised
///     Page
///     NumPages
///     FileName
///     FilePath
///     FullyQualifiedPath
///     FileExtension
///     FileTYpe
///     Title
///     Options
///     Top padding
///     Bottom padding
///     Right padding
///     Left Padding
///     top, left, right, bottom border
///     border pen style
///     border color
///     font
/// </summary>
// TODO: How to deal with clipping
// 1) Order of print - Left, Right, Center (center wins)
// 2) Elipsis - different based on macro. E.g. FullFilePath is "Start...FileName" where FileName is truncated last.
// 3) Clipped (never overwritten - ugly)
// 4) Wrapped (post MLP)
public abstract class HeaderFooter : ModelBase
{
    private bool _bottomBorder;
    private bool _enabled;
    private Font? _font;
    private bool _leftBorder;
    private bool _rightBorder;
    private string? _text;
    private bool _topBorder;
    private int _verticalPadding;

    /// <summary>
    ///     Header text. May contain macros (e.g. {FileName} or {Page}
    /// </summary>
    public string? Text
    {
        get => _text;
        set => SetField(ref _text, value);
    }

    /// <summary>
    ///     Provides a telemetry-safe version of Text (a comma delimited list with only the macros used). See
    ///     HeaderFooterViewModel for more details on how macros are parsed.
    /// </summary>
    [JsonIgnore]
    [SafeForTelemetry]
    public string MacrosUsed
    {
        get
        {
            var matches = Regex.Matches(Text ?? string.Empty,
                    @"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+")
                .Select(match => match.Value)
                .ToList();
            return string.Join(", ", from macro in matches select macro);
        }
    }

    /// <summary>
    ///     Font used for header or footer text
    /// </summary>
    [SafeForTelemetry]
    public Font? Font
    {
        get => _font;
        set => SetField(ref _font, value);
    }

    /// <summary>
    ///     Enables or disables printing of left border of heder/footer
    /// </summary>
    [SafeForTelemetry]
    public bool LeftBorder
    {
        get => _leftBorder;
        set => SetField(ref _leftBorder, value);
    }

    /// <summary>
    ///     Enables or disables printing of Top border of heder/footer
    /// </summary>
    [SafeForTelemetry]
    public bool TopBorder
    {
        get => _topBorder;
        set => SetField(ref _topBorder, value);
    }

    /// <summary>
    ///     Enables or disables printing of Right border of heder/footer
    /// </summary>
    [SafeForTelemetry]
    public bool RightBorder
    {
        get => _rightBorder;
        set => SetField(ref _rightBorder, value);
    }

    /// <summary>
    ///     Enables or disables printing of Bottom border of heder/footer
    /// </summary>
    [SafeForTelemetry]
    public bool BottomBorder
    {
        get => _bottomBorder;
        set => SetField(ref _bottomBorder, value);
    }

    /// <summary>
    ///     Enable or disable header/footer
    /// </summary>
    [SafeForTelemetry]
    public bool Enabled
    {
        get => _enabled;
        set => SetField(ref _enabled, value);
    }

    /// <summary>
    ///     Vertical padding below header / above footer in 100ths of an inch
    /// </summary>
    [SafeForTelemetry]
    public int VerticalPadding
    {
        get => _verticalPadding;
        set => SetField(ref _verticalPadding, value);
    }
}
