using System;

namespace WinPrint.Core.Abstractions;

public sealed class GraphicsStringFormat
{
    public GraphicsStringFormatFlags FormatFlags { get; set; } = GraphicsStringFormatFlags.None;
    public GraphicsTextAlignment Alignment { get; set; } = GraphicsTextAlignment.Near;
    public GraphicsTextAlignment LineAlignment { get; set; } = GraphicsTextAlignment.Near;
    public GraphicsStringTrimming Trimming { get; set; } = GraphicsStringTrimming.None;
}
