using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch005RestrictArgAnyUsageAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "TestQuality";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.RestrictArgAnyUsage,
        title: "Restrict usage of NSubstitute Arg.Any()",
        messageFormat: "Avoid NSubstitute Arg.Any() outside the allowed convention. Use DidNotReceive/DidNotReceiveWithAnyArgs instead.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Arg.Any() is a very broad matcher that can hide intent and make tests less precise. This rule restricts Arg.Any() usage to specific negative-assertion conventions (DidNotReceive/DidNotReceiveWithAnyArgs), where broad matching is explicitly accepted.",
        helpLinkUri: "docs/rules/ARCH005.md");

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

            var nsubstituteArgType = compilationContext.Compilation.GetTypeByMetadataName("NSubstitute.Arg");
            if (nsubstituteArgType is null)
            {
                // Avoid false positives when NSubstitute isn't referenced.
                return;
            }

            var isTestTypeCache = new ConcurrentDictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, nsubstituteArgType, testMethodAttributes, isTestTypeCache),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol nsubstituteArgType,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod;

        if (!string.Equals(targetMethod.Name, "Any", StringComparison.Ordinal))
        {
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType, nsubstituteArgType))
        {
            // Ensure we only target NSubstitute.Arg.Any()
            return;
        }

        if (!IsWithinTestContext(context.ContainingSymbol, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        if (IsAllowedByConvention(invocation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, GetArgAnyLocation(invocation.Syntax)));
    }

    private static bool IsAllowedByConvention(IInvocationOperation argAnyInvocation)
    {
        // Convention: allow Arg.Any() only when used directly as an argument
        // of an invocation in a call chain that is preceded by DidNotReceive()/DidNotReceiveWithAnyArgs().
        // Example:
        //   substitute.DidNotReceive().Foo(Arg.Any<int>());

        // We intentionally use the *operation* tree (semantic) to locate the argument owner invocation,
        // which is robust to casts/conversions around Arg.Any().

        IOperation? current = argAnyInvocation;
        while (current is not null)
        {
            if (current.Parent is IArgumentOperation argumentOperation)
            {
                // Next parent should be the invocation receiving the argument
                if (argumentOperation.Parent is IInvocationOperation receivingInvocation)
                {
                    if (!IsDirectArgumentValue(argumentOperation, argAnyInvocation))
                    {
                        return false;
                    }

                    return HasDidNotReceiveInReceiverChain(receivingInvocation);
                }
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool IsDirectArgumentValue(IArgumentOperation argumentOperation, IInvocationOperation argAnyInvocation)
    {
        // Only allow when Arg.Any() is the argument value itself (ignoring implicit conversions).
        // This avoids allowing "matcher" usage inside expressions like: Foo(Arg.Any<int>() + 1).
        IOperation? value = argumentOperation.Value;

        while (value is IConversionOperation conversion)
        {
            value = conversion.Operand;
        }

        return ReferenceEquals(value, argAnyInvocation);
    }

    private static bool HasDidNotReceiveInReceiverChain(IInvocationOperation receivingInvocation)
    {
        // We want: X.DidNotReceive().Foo(...Arg.Any...)
        // So the instance for Foo is itself an invocation operation with name DidNotReceive or DidNotReceiveWithAnyArgs.
        var instance = receivingInvocation.Instance;
        if (instance is null)
        {
            return false;
        }

        if (instance is IInvocationOperation didNotReceiveInvocation)
        {
            return IsDidNotReceiveMethod(didNotReceiveInvocation.TargetMethod);
        }

        // Conditional access: `sub.DidNotReceive()?.Foo(Arg.Any<int>())`
        // In this shape, the Foo invocation's Instance is a placeholder (IConditionalAccessInstanceOperation)
        // and the DidNotReceive invocation is available as the conditional's Operation.
        if (instance is IConditionalAccessInstanceOperation
            && receivingInvocation.Parent is IConditionalAccessOperation conditionalAccess
            && conditionalAccess.Operation is IInvocationOperation conditionalReceiverInvocation)
        {
            return IsDidNotReceiveMethod(conditionalReceiverInvocation.TargetMethod);
        }

        return false;
    }

    private static bool IsDidNotReceiveMethod(IMethodSymbol method)
    {
        if (!string.Equals(method.Name, "DidNotReceive", StringComparison.Ordinal)
            && !string.Equals(method.Name, "DidNotReceiveWithAnyArgs", StringComparison.Ordinal))
        {
            return false;
        }

        // Avoid allowing custom lookalike APIs.
        return IsInNSubstituteNamespace(method.ContainingNamespace);
    }

    private static bool IsInNSubstituteNamespace(INamespaceSymbol? @namespace)
    {
        for (var current = @namespace; current is not null && !current.IsGlobalNamespace; current = current.ContainingNamespace)
        {
            if (string.Equals(current.Name, "NSubstitute", StringComparison.Ordinal)
                && current.ContainingNamespace.IsGlobalNamespace)
            {
                return true;
            }
        }

        return false;
    }

    private static Location GetArgAnyLocation(SyntaxNode syntax)
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
        // Same approach as ARCH003: include local functions and helper methods inside test types.
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
}
