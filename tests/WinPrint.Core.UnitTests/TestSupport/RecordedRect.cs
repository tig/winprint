namespace WinPrint.Core.UnitTests.TestSupport;

/// <summary>A rectangle-drawing operation captured by <see cref="RecordingGraphicsContext" />.</summary>
public readonly record struct RecordedRect (float X, float Y, float Width, float Height);
