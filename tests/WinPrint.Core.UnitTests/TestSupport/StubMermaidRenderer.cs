using WinPrint.Core.ContentTypeEngines;

namespace WinPrint.Core.UnitTests.TestSupport;

/// <summary>
///     Deterministic <see cref="IMermaidRenderer" /> double: returns the configured bytes (null =
///     simulate a failed render) and records every diagram it was asked to render, so tests can verify
///     the mermaid pipeline without any network access.
/// </summary>
public sealed class StubMermaidRenderer(byte[]? bytes) : IMermaidRenderer
{
    public List<string> Diagrams { get; } = [];

    public Task<byte[]?> RenderAsync(string diagram)
    {
        Diagrams.Add(diagram);
        return Task.FromResult(bytes);
    }
}
