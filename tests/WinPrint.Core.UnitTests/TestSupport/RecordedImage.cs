namespace WinPrint.Core.UnitTests.TestSupport;

/// <summary>An image-drawing operation captured by <see cref="RecordingGraphicsContext" />.</summary>
public readonly record struct RecordedImage(float X, float Y, float Width, float Height);
