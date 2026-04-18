using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch006WarnOnExcludingInBeEquivalentToAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "TestQuality";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.WarnOnExcludingInBeEquivalentTo,
        title: "Warn on exclusions in BeEquivalentTo()",
        messageFormat: "Avoid using '{0}' in BeEquivalentTo() options. Exclusions can reduce test precision.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "FluentAssertions equivalency exclusions (Excluding*) can hide regressions by making tests less strict. Prefer asserting precise equivalency and use exclusions only when there is an explicit, documented reason.",
        helpLinkUri: "docs/rules/ARCH006.md");

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
                // Avoid noise outside test projects.
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

        if (!string.Equals(targetMethod.Name, "BeEquivalentTo", StringComparison.Ordinal))
        {
            return;
        }

        if (!IsFluentAssertionsMethod(targetMethod))
        {
            // Avoid false positives on lookalike APIs.
            return;
        }

        if (!IsWithinTestContext(context.ContainingSymbol, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        foreach (var argument in invocation.Arguments)
        {
            if (!TryGetAnonymousFunction(argument.Value, out var anonymousFunction))
            {
                continue;
            }

            ReportExcludingCallsInAnonymousFunctionBody(context, anonymousFunction);
        }
    }

    private static void ReportExcludingCallsInAnonymousFunctionBody(OperationAnalysisContext context, IAnonymousFunctionOperation anonymousFunction)
    {
        // Intentionally scan only the BeEquivalentTo options delegate body.
        // This keeps the check targeted and avoids scanning the whole syntax tree.
        var body = anonymousFunction.Body;
        if (body is null)
        {
            return;
        }

        var stack = new Stack<IOperation>();
        stack.Push(body);

        while (stack.Count > 0)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var current = stack.Pop();

            if (current is IInvocationOperation invocation
                && IsEquivalencyExcludingMethod(invocation.TargetMethod))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    GetInvocationMemberNameLocation(invocation.Syntax),
                    invocation.TargetMethod.Name));
            }

            foreach (var child in current.ChildOperations)
            {
                stack.Push(child);
            }
        }
    }

    private static bool TryGetAnonymousFunction(IOperation? operation, out IAnonymousFunctionOperation anonymousFunction)
    {
        anonymousFunction = null!;

        if (operation is null)
        {
            return false;
        }

        IOperation? current = operation;

        while (current is not null)
        {
            switch (current)
            {
                case IAnonymousFunctionOperation anon:
                    anonymousFunction = anon;
                    return true;

                case IConversionOperation conversion:
                    current = conversion.Operand;
                    continue;

                case IDelegateCreationOperation delegateCreation:
                    current = delegateCreation.Target;
                    continue;

                case IParenthesizedOperation parenthesized:
                    current = parenthesized.Operand;
                    continue;
            }

            break;
        }

        return false;
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

    private static bool IsEquivalencyExcludingMethod(IMethodSymbol method)
    {
        if (!method.Name.StartsWith("Excluding", StringComparison.Ordinal))
        {
            return false;
        }

        var containingType = method.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        // Avoid false positives on unrelated FluentAssertions APIs that might coincidentally use the same name.
        // Excluding* methods used to tweak BeEquivalentTo live under FluentAssertions.Equivalency.
        return IsInFluentAssertionsEquivalencyNamespace(containingType.ContainingNamespace);
    }

    private static bool IsInFluentAssertionsEquivalencyNamespace(INamespaceSymbol? @namespace)
    {
        // Match FluentAssertions.Equivalency and its sub-namespaces.
        for (var current = @namespace; current is not null && !current.IsGlobalNamespace; current = current.ContainingNamespace)
        {
            if (string.Equals(current.Name, "Equivalency", StringComparison.Ordinal)
                && current.ContainingNamespace is { IsGlobalNamespace: false } parent
                && string.Equals(parent.Name, "FluentAssertions", StringComparison.Ordinal)
                && parent.ContainingNamespace.IsGlobalNamespace)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInFluentAssertionsNamespace(INamespaceSymbol? @namespace)
    {
        // Match FluentAssertions and its sub-namespaces.
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
        // Same approach as ARCH003/ARCH005: include local functions and helper methods inside test types.
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

    private static Location GetInvocationMemberNameLocation(SyntaxNode syntax)
    {
        return syntax switch
        {
            InvocationExpressionSyntax invocation => invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name.GetLocation(),
                IdentifierNameSyntax identifierName => identifierName.Identifier.GetLocation(),
                GenericNameSyntax genericName => genericName.Identifier.GetLocation(),
                _ => invocation.GetLocation(),
            },
            _ => syntax.GetLocation(),
        };
    }
}
