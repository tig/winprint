using Xunit.Sdk;

namespace WinPrint.TUI.UnitTests.Testing;

/// <summary>Assertion helpers over a captured plain-text screen render (e.g. <c>AppFixture.Screen</c>).</summary>
public static class DriverAssert
{
    /// <summary>Asserts the rendered grid contains <paramref name="expected" /> as a substring.</summary>
    public static void ContainsText(string rendered, string expected)
    {
        ArgumentNullException.ThrowIfNull(rendered);
        ArgumentNullException.ThrowIfNull(expected);

        if (rendered.Contains(expected, StringComparison.Ordinal))
        {
            return;
        }

        throw new XunitException($"Expected rendered text to contain:\n{expected}\n\nActual:\n{rendered}");
    }

    /// <summary>Asserts the rendered grid does NOT contain <paramref name="text" />.</summary>
    public static void DoesNotContainText(string rendered, string text)
    {
        ArgumentNullException.ThrowIfNull(rendered);
        ArgumentNullException.ThrowIfNull(text);

        if (!rendered.Contains(text, StringComparison.Ordinal))
        {
            return;
        }

        throw new XunitException($"Expected rendered text NOT to contain:\n{text}\n\nActual:\n{rendered}");
    }
}
