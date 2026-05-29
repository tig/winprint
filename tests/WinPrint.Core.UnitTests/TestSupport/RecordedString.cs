namespace WinPrint.Core.UnitTests.TestSupport;

/// <summary>A text-drawing operation captured by <see cref="RecordingGraphicsContext" />.</summary>
public readonly record struct RecordedString(string Text, float X, float Y);
