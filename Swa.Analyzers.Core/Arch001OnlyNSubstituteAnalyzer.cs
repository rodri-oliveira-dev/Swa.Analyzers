using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Swa.Analyzers.Core;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch001OnlyNSubstituteAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ARCH001";

    private static readonly string[] DefaultBlockedNamespaces = ["Moq", "FakeItEasy", "Rhino.Mocks"];
    private static readonly string[] DefaultTestProjectPatterns = ["test", "tests", "spec"];

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Only NSubstitute is allowed as mocking library",
        messageFormat: "Replace '{0}' usage with NSubstitute",
        category: DiagnosticCategories.Architecture,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Tests should use only NSubstitute as mocking library.",
        helpLinkUri: "https://github.com/your-org/swa-analyzers/blob/main/docs/rules/ARCH001.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var options = Arch001Options.From(compilationContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions);

            if (options.OnlyTestProjects && !IsTestProject(compilationContext.Compilation, options.TestProjectPatterns))
            {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(
                static ctx => AnalyzeUsingDirective(ctx, options.BlockedNamespaces),
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.UsingDirective);

            compilationContext.RegisterSyntaxNodeAction(
                static ctx => AnalyzeSymbolReference(ctx, options.BlockedNamespaces),
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.IdentifierName,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.GenericName);
        });
    }

    private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context, ImmutableArray<string> blockedNamespaces)
    {
        var usingDirective = (UsingDirectiveSyntax)context.Node;
        var namespaceName = usingDirective.Name?.ToString();
        if (string.IsNullOrWhiteSpace(namespaceName))
        {
            return;
        }

        foreach (var blockedNamespace in blockedNamespaces)
        {
            if (IsMatch(namespaceName, blockedNamespace))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, usingDirective.GetLocation(), blockedNamespace));
                return;
            }
        }
    }

    private static void AnalyzeSymbolReference(SyntaxNodeAnalysisContext context, ImmutableArray<string> blockedNamespaces)
    {
        var symbol = context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken).Symbol;
        if (symbol is null)
        {
            return;
        }

        var containingNamespace = symbol.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        if (string.IsNullOrWhiteSpace(containingNamespace))
        {
            return;
        }

        foreach (var blockedNamespace in blockedNamespaces)
        {
            if (IsMatch(containingNamespace, blockedNamespace))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), blockedNamespace));
                return;
            }
        }
    }

    private static bool IsTestProject(Compilation compilation, ImmutableArray<string> testProjectPatterns)
    {
        if (string.IsNullOrWhiteSpace(compilation.AssemblyName))
        {
            return false;
        }

        var assemblyName = compilation.AssemblyName;
        foreach (var pattern in testProjectPatterns)
        {
            if (assemblyName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var reference in compilation.ReferencedAssemblyNames)
        {
            var referencedName = reference.Name;
            if (referencedName.Equals("xunit.core", StringComparison.OrdinalIgnoreCase) ||
                referencedName.Equals("nunit.framework", StringComparison.OrdinalIgnoreCase) ||
                referencedName.Equals("Microsoft.VisualStudio.TestPlatform.TestFramework", StringComparison.OrdinalIgnoreCase) ||
                referencedName.Equals("MSTest.TestFramework", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsMatch(string value, string blockedNamespace)
        => value.Equals(blockedNamespace, StringComparison.Ordinal)
            || value.StartsWith(blockedNamespace + ".", StringComparison.Ordinal);

    private readonly record struct Arch001Options(
        bool OnlyTestProjects,
        ImmutableArray<string> BlockedNamespaces,
        ImmutableArray<string> TestProjectPatterns)
    {
        public static Arch001Options From(AnalyzerConfigOptions options)
        {
            var onlyTestProjects = true;
            if (options.TryGetValue(AnalyzerConfigKeys.Arch001OnlyTestProjects, out var onlyTestProjectsText)
                && bool.TryParse(onlyTestProjectsText, out var parsedOnlyTestProjects))
            {
                onlyTestProjects = parsedOnlyTestProjects;
            }

            var blockedNamespaces = ParseList(options, AnalyzerConfigKeys.Arch001BlockedNamespaces, DefaultBlockedNamespaces);
            var testProjectPatterns = ParseList(options, AnalyzerConfigKeys.Arch001TestProjectPatterns, DefaultTestProjectPatterns);

            return new Arch001Options(onlyTestProjects, blockedNamespaces, testProjectPatterns);
        }

        private static ImmutableArray<string> ParseList(
            AnalyzerConfigOptions options,
            string key,
            string[] defaults)
        {
            if (!options.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                return [.. defaults];
            }

            var list = value
                .Split([',', ';', '|'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.Ordinal)
                .ToImmutableArray();

            return list.IsDefaultOrEmpty ? [.. defaults] : list;
        }
    }
}
