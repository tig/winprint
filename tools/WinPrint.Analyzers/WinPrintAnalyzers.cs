using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WinPrint.Analyzers;

/// <summary>
/// Enforces the project's house rules:
///   WPA0001 - One type per file.
///   WPA0002 - No nested type declarations.
///
/// Generated files (anything ending in <c>.Designer.cs</c>, <c>.Generated.cs</c>,
/// <c>.g.cs</c>, or <c>.g.i.cs</c>, any file under an <c>obj/</c> or <c>bin/</c>
/// directory, or any file containing an <c>&lt;auto-generated&gt;</c> header) are exempt.
/// </summary>
[DiagnosticAnalyzer (LanguageNames.CSharp)]
public sealed class WinPrintAnalyzers : DiagnosticAnalyzer
{
    public const string OneTypePerFileId = "WPA0001";
    public const string NoNestedTypesId = "WPA0002";

    private static readonly DiagnosticDescriptor OneTypePerFileRule = new (
        id: OneTypePerFileId,
        title: "One type per file",
        messageFormat: "File '{0}' declares more than one top-level type ('{1}'); split each type into its own file",
        category: "WinPrint.Style",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each source file must declare exactly one top-level type.");

    private static readonly DiagnosticDescriptor NoNestedTypesRule = new (
        id: NoNestedTypesId,
        title: "No nested types",
        messageFormat: "Type '{0}' is nested inside '{1}'; promote it to a top-level type in its own file",
        category: "WinPrint.Style",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Nested type declarations are not allowed. Promote nested types to top-level types in their own files.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create (OneTypePerFileRule, NoNestedTypesRule);

    public override void Initialize (AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution ();
        context.RegisterSyntaxTreeAction (AnalyzeSyntaxTree);
    }

    private static void AnalyzeSyntaxTree (SyntaxTreeAnalysisContext context)
    {
        SyntaxTree tree = context.Tree;
        if (IsGenerated (tree))
        {
            return;
        }

        SyntaxNode root = tree.GetRoot (context.CancellationToken);

        // WPA0001 - collect every top-level type declaration in the file.
        BaseTypeDeclarationSyntax[] topLevelTypes = root
            .DescendantNodes (descendIntoChildren: node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .OfType<BaseTypeDeclarationSyntax> ()
            .Where (t => t.Parent is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .ToArray ();

        if (topLevelTypes.Length > 1)
        {
            string fileName = System.IO.Path.GetFileName (tree.FilePath);
            string names = string.Join (", ", topLevelTypes.Select (t => t.Identifier.ValueText));
            // Report on the second-and-subsequent declarations so the first type
            // (the conventional primary type) stays clean.
            for (int i = 1; i < topLevelTypes.Length; i++)
            {
                context.ReportDiagnostic (Diagnostic.Create (
                    OneTypePerFileRule,
                    topLevelTypes[i].Identifier.GetLocation (),
                    fileName,
                    names));
            }
        }

        // WPA0002 - flag every type declared inside another type.
        foreach (BaseTypeDeclarationSyntax nested in root.DescendantNodes ().OfType<BaseTypeDeclarationSyntax> ())
        {
            if (nested.Parent is BaseTypeDeclarationSyntax outer)
            {
                context.ReportDiagnostic (Diagnostic.Create (
                    NoNestedTypesRule,
                    nested.Identifier.GetLocation (),
                    nested.Identifier.ValueText,
                    outer.Identifier.ValueText));
            }
        }
    }

    private static bool IsGenerated (SyntaxTree tree)
    {
        string path = tree.FilePath ?? string.Empty;
        if (path.Length == 0)
        {
            return false;
        }

        string fileName = System.IO.Path.GetFileName (path);
        if (fileName.EndsWith (".Designer.cs", System.StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith (".Generated.cs", System.StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith (".g.cs", System.StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith (".g.i.cs", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string normalized = path.Replace ('\\', '/');
        if (normalized.Contains ("/obj/") || normalized.Contains ("/bin/"))
        {
            return true;
        }

        // Check for an <auto-generated> header in the leading trivia.
        SyntaxNode root = tree.GetRoot ();
        SyntaxTriviaList leading = root.GetLeadingTrivia ();
        foreach (SyntaxTrivia trivia in leading)
        {
            string text = trivia.ToString ();
            if (text.IndexOf ("<auto-generated", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
