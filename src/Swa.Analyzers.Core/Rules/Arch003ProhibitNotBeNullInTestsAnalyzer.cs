using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch003ProhibitNotBeNullInTestsAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "TestQuality";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.ProhibitNotBeNullInTests,
        title: "Prohibit NotBeNull() in tests",
        messageFormat: "Avoid NotBeNull() in tests. Prefer a more specific assertion when possible.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "NotBeNull() is a weak assertion that often hides intent. Prefer more specific assertions (for example NotBeNullOrEmpty, BeOfType, BeAssignableTo, HaveValue) to improve test clarity.",
        helpLinkUri: "docs/rules/ARCH003.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var testMethodAttributes = GetKnownTestMethodAttributes(compilationContext.Compilation);
            if (testMethodAttributes.IsDefaultOrEmpty)
            {
                // Avoid false positives outside test projects.
                return;
            }

            var isTestTypeCache = new ConcurrentDictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, testMethodAttributes, isTestTypeCache),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod;

        if (!string.Equals(targetMethod.Name, "NotBeNull", StringComparison.Ordinal))
        {
            return;
        }

        if (!IsFluentAssertionsMethod(targetMethod))
        {
            return;
        }

        if (!IsWithinTestContext(context.ContainingSymbol, testMethodAttributes, isTestTypeCache))
        {
            // Limit the rule to actual test contexts.
            return;
        }

        var location = GetNotBeNullLocation(invocation.Syntax);
        context.ReportDiagnostic(Diagnostic.Create(Rule, location));
    }

    private static bool IsFluentAssertionsMethod(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        return IsInFluentAssertionsNamespace(containingType.ContainingNamespace);
    }

    private static bool IsInFluentAssertionsNamespace(INamespaceSymbol? @namespace)
    {
        for (var current = @namespace; current is not null && !current.IsGlobalNamespace; current = current.ContainingNamespace)
        {
            if (string.Equals(current.Name, "FluentAssertions", StringComparison.Ordinal)
                && current.ContainingNamespace.IsGlobalNamespace)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTestMethod(IMethodSymbol method, ImmutableArray<INamedTypeSymbol> testMethodAttributes)
    {
        if (testMethodAttributes.IsDefaultOrEmpty)
        {
            return false;
        }

        foreach (var attribute in method.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass is null)
            {
                continue;
            }

            foreach (var testAttribute in testMethodAttributes)
            {
                if (SymbolEqualityComparer.Default.Equals(attributeClass, testAttribute))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsWithinTestContext(
        ISymbol containingSymbol,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        // Handle cases where the invocation is inside a local function or other nested symbol.
        for (ISymbol? current = containingSymbol; current is not null; current = current.ContainingSymbol)
        {
            if (current is IMethodSymbol method && IsTestMethod(method, testMethodAttributes))
            {
                return true;
            }

            if (current is INamedTypeSymbol type && IsTestType(type, testMethodAttributes, isTestTypeCache))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTestType(
        INamedTypeSymbol type,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        // The rule intentionally scopes to “test types” (classes that contain at least one known test method)
        // to reduce noise for utility code living inside test projects.
        return isTestTypeCache.GetOrAdd(type, _ => ComputeIsTestType(type, testMethodAttributes));
    }

    private static bool ComputeIsTestType(INamedTypeSymbol type, ImmutableArray<INamedTypeSymbol> testMethodAttributes)
    {
        foreach (var member in type.GetMembers())
        {
            if (member is IMethodSymbol method && IsTestMethod(method, testMethodAttributes))
            {
                return true;
            }
        }

        return false;
    }

    private static ImmutableArray<INamedTypeSymbol> GetKnownTestMethodAttributes(Compilation compilation)
    {
        // This list is intentionally narrow: the analyzer only runs when the compilation references
        // at least one known test framework attribute type.
        var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

        AddIfNotNull(builder, compilation.GetTypeByMetadataName("Xunit.FactAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("Xunit.TheoryAttribute"));

        AddIfNotNull(builder, compilation.GetTypeByMetadataName("NUnit.Framework.TestAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("NUnit.Framework.TestCaseAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("NUnit.Framework.TestCaseSourceAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("NUnit.Framework.SetUpAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("NUnit.Framework.TearDownAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("NUnit.Framework.OneTimeSetUpAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("NUnit.Framework.OneTimeTearDownAttribute"));

        AddIfNotNull(builder, compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethodAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.ClassInitializeAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.ClassCleanupAttribute"));

        return builder.ToImmutable();
    }

    private static void AddIfNotNull(ImmutableArray<INamedTypeSymbol>.Builder builder, INamedTypeSymbol? symbol)
    {
        if (symbol is not null)
        {
            builder.Add(symbol);
        }
    }

    private static Location GetNotBeNullLocation(SyntaxNode syntax)
    {
        return syntax switch
        {
            InvocationExpressionSyntax invocation => invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name.GetLocation(),
                _ => invocation.GetLocation(),
            },
            _ => syntax.GetLocation(),
        };
    }
}
