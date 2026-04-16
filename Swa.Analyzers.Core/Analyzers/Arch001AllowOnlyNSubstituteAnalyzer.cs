using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Swa.Analyzers.Core.Common;

namespace Swa.Analyzers.Core.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch001AllowOnlyNSubstituteAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ARCH001";

    private const string Category = "Architecture";
    private const string OnlyTestProjectsKey = "dotnet_diagnostic.ARCH001.only_test_projects";
    private const string BlockedNamespacesKey = "dotnet_diagnostic.ARCH001.blocked_namespaces";
    private const string BlockedAssembliesKey = "dotnet_diagnostic.ARCH001.blocked_assemblies";
    private const string TestProjectPatternsKey = "dotnet_diagnostic.ARCH001.test_project_patterns";

    private static readonly ImmutableArray<string> DefaultBlockedNamespaces =
    [
        "Moq",
        "FakeItEasy",
        "Rhino.Mocks"
    ];

    private static readonly ImmutableArray<string> DefaultTestProjectPatterns =
    [
        "test",
        "tests",
        "spec"
    ];

    private static readonly ImmutableArray<string> KnownTestAssemblyNames =
    [
        "xunit",
        "nunit.framework",
        "microsoft.visualstudio.testplatform.testframework"
    ];

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Only NSubstitute is allowed for mocks",
        "Mocking library '{0}' is blocked by project policy; use NSubstitute",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Prevents using non-approved mocking libraries to keep test style and dependencies consistent.",
        helpLinkUri: "https://github.com/your-org/swa-analyzers/blob/main/docs/rules/ARCH001.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(startContext =>
        {
            var global = startContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
            var blockedNamespaces = global.GetCsvOption(BlockedNamespacesKey, DefaultBlockedNamespaces);
            var blockedAssemblies = global.GetCsvOption(BlockedAssembliesKey, blockedNamespaces);
            var onlyTestProjects = global.GetBooleanOption(OnlyTestProjectsKey, defaultValue: true);
            var testProjectPatterns = global.GetCsvOption(TestProjectPatternsKey, DefaultTestProjectPatterns);

            if (onlyTestProjects && !IsTestProject(startContext.Compilation, testProjectPatterns))
            {
                return;
            }

            startContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeUsingDirective(syntaxContext, blockedNamespaces),
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.UsingDirective);

            startContext.RegisterOperationAction(
                operationContext => AnalyzeOperation(operationContext, blockedNamespaces, blockedAssemblies),
                OperationKind.Invocation,
                OperationKind.ObjectCreation,
                OperationKind.PropertyReference,
                OperationKind.FieldReference);
        });
    }

    private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context, ImmutableArray<string> blockedNamespaces)
    {
        var usingDirective = (UsingDirectiveSyntax)context.Node;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(usingDirective.Name!, context.CancellationToken).Symbol;
        var namespaceName = symbolInfo is INamespaceSymbol namespaceSymbol
            ? namespaceSymbol.ToDisplayString()
            : usingDirective.Name?.ToString();
        var blockedRoot = FindBlockedMatch(namespaceName, blockedNamespaces);
        if (blockedRoot is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, usingDirective.GetLocation(), blockedRoot));
        }
    }

    private static void AnalyzeOperation(
        OperationAnalysisContext context,
        ImmutableArray<string> blockedNamespaces,
        ImmutableArray<string> blockedAssemblies)
    {
        var symbol = context.Operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod,
            IObjectCreationOperation creation => creation.Constructor,
            IPropertyReferenceOperation property => property.Property,
            IFieldReferenceOperation field => field.Field,
            _ => null
        };

        if (symbol is null)
        {
            return;
        }

        var containingNamespace = symbol.ContainingNamespace?.ToDisplayString();
        if (string.IsNullOrWhiteSpace(containingNamespace))
        {
            return;
        }

        var blockedRoot = FindBlockedMatch(containingNamespace, blockedNamespaces)
            ?? FindBlockedMatch(symbol.ContainingAssembly?.Name, blockedAssemblies);

        if (blockedRoot is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), blockedRoot));
    }

    private static bool IsTestProject(Compilation compilation, ImmutableArray<string> testProjectPatterns)
    {
        var assemblyName = compilation.AssemblyName ?? string.Empty;
        if (ContainsAnyPattern(assemblyName, testProjectPatterns))
        {
            return true;
        }

        foreach (var reference in compilation.ReferencedAssemblyNames)
        {
            if (KnownTestAssemblyNames.Contains(reference.Name, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsAnyPattern(string value, ImmutableArray<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (value.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string? FindBlockedMatch(string? namespaceOrAssembly, ImmutableArray<string> blockedNamespaces)
    {
        if (string.IsNullOrWhiteSpace(namespaceOrAssembly))
        {
            return null;
        }

        return blockedNamespaces.FirstOrDefault(blocked =>
            namespaceOrAssembly.Equals(blocked, StringComparison.Ordinal)
            || namespaceOrAssembly.StartsWith(blocked + ".", StringComparison.Ordinal));
    }
}
