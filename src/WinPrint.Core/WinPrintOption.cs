namespace WinPrint.Core;

/// <summary>
///     One canonical winprint command-line option, shared by every front end (CLI, TUI, WinForms, MAUI).
///     The TUI is the reference surface; <see cref="WinPrintOptions" /> is the single source of truth that
///     every front end derives its parser from, so option names, short aliases, and semantics can never
///     diverge between front ends.
/// </summary>
/// <param name="Name">Long option name, without the leading <c>--</c> (e.g. <c>paper-size</c>).</param>
/// <param name="Short">Single-character alias, without the leading <c>-</c>, or <see langword="null" /> for none.</param>
/// <param name="ValueType">The option's value type: <see cref="string" />, <see cref="int" />, or <see cref="bool" /> (a flag).</param>
/// <param name="Help">Help text shown in <c>--help</c> and the end-user docs.</param>
public sealed record WinPrintOption(string Name, char? Short, Type ValueType, string Help);
