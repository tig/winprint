using Terminal.Gui.Editor.Completion;
using Terminal.Gui.Editor.Document;
using Terminal.Gui.Input;
using WinPrint.Core.Models;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Feeds winprint's header/footer macro names (see <see cref="MacroChoices" />) to the tui-cs
///     <c>Editor</c> autocomplete popup. Typing a macro name — or pressing <c>Ctrl+Space</c> on an
///     empty field — offers the known macros; accepting one inserts the braced token, e.g.
///     <c>{FileName}</c>.
///     <para>
///         The editor's completion prefix is word characters only (letters/digits/underscore), so a
///         leading <c>{</c> is a prefix boundary that the accept step does <em>not</em> overwrite. To
///         avoid producing <c>{{FileName}</c> when the user has already typed the opening brace, the
///         insert text omits the leading brace whenever one immediately precedes the prefix.
///     </para>
/// </summary>
internal sealed class MacroCompletionProvider : IEditorCompletionProvider
{
    /// <inheritdoc />
    public IReadOnlyList<CompletionItem> GetCompletions(TextDocument document, int caretOffset, string prefix)
    {
        // The prefix starts caretOffset - prefix.Length back; the char just before it tells us
        // whether the user already typed the opening brace (so we don't emit a second one).
        int prefixStart = caretOffset - prefix.Length;
        bool afterBrace = prefixStart > 0 && document.GetCharAt(prefixStart - 1) == '{';

        return MacroChoices.Names
            .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(name => new CompletionItem
            {
                Label = "{" + name + "}",
                InsertText = afterBrace ? name + "}" : "{" + name + "}"
            })
            .ToList();
    }

    /// <inheritdoc />
    // Ctrl+Space force-opens the popup (e.g. on an empty field); ordinary typing opens and filters
    // it automatically through the editor's filter-as-you-type path.
    public bool ShouldTrigger(Key key)
    {
        return key == Key.Space.WithCtrl;
    }
}
