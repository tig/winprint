namespace WinPrint.Core.UnitTests.TestSupport;

/// <summary>A line-drawing operation captured by <see cref="RecordingGraphicsContext" />.</summary>
public readonly record struct RecordedLine(float X1, float Y1, float X2, float Y2);
