using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Reliability";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.ProhibitSyncOverAsyncBlockingCalls,
        title: "Prohibit synchronous blocking of asynchronous operations",
        messageFormat: "Avoid synchronous blocking with '{0}'. This risks deadlocks and reduces scalability. Prefer 'await'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Blocking on asynchronous operations using .Result, .Wait(), or .GetAwaiter().GetResult() risks deadlocks and degrades application scalability. Use 'await' instead.",
        helpLinkUri: "docs/rules/ARCH009.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var taskType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var taskOfTType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            var valueTaskType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
            var valueTaskOfTType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

            if (taskType is null && valueTaskType is null)
            {
                return;
            }

            compilationContext.RegisterOperationAction(
                context => AnalyzePropertyReference(context, taskOfTType, valueTaskOfTType),
                OperationKind.PropertyReference);

            compilationContext.RegisterOperationAction(
                context => AnalyzeInvocation(context, taskType, taskOfTType, valueTaskType, valueTaskOfTType),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzePropertyReference(
        OperationAnalysisContext context,
        INamedTypeSymbol? taskOfTType,
        INamedTypeSymbol? valueTaskOfTType)
    {
        var propertyReference = (IPropertyReferenceOperation)context.Operation;

        if (!string.Equals(propertyReference.Property.Name, "Result", StringComparison.Ordinal))
        {
            return;
        }

        if (!IsKnownAwaitableType(propertyReference.Instance, taskOfTType, valueTaskOfTType))
        {
            return;
        }

        var location = GetMemberLocation(propertyReference.Syntax);
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, ".Result"));
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskOfTType,
        INamedTypeSymbol? valueTaskType,
        INamedTypeSymbol? valueTaskOfTType)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod;

        if (string.Equals(targetMethod.Name, "Wait", StringComparison.Ordinal)
            && targetMethod.Parameters.Length <= 1)
        {
            if (IsKnownAwaitableType(invocation.Instance, taskType, taskOfTType))
            {
                var location = GetMemberLocation(invocation.Syntax);
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, ".Wait()"));
            }

            return;
        }

        if (string.Equals(targetMethod.Name, "GetResult", StringComparison.Ordinal)
            && targetMethod.Parameters.IsEmpty)
        {
            if (invocation.Instance is IInvocationOperation getAwaiterInvocation
                && string.Equals(getAwaiterInvocation.TargetMethod.Name, "GetAwaiter", StringComparison.Ordinal)
                && getAwaiterInvocation.TargetMethod.Parameters.IsEmpty
                && IsKnownAwaitableType(getAwaiterInvocation.Instance, taskType, taskOfTType, valueTaskType, valueTaskOfTType))
            {
                var location = GetMemberLocation(invocation.Syntax);
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, ".GetAwaiter().GetResult()"));
            }

            return;
        }
    }

    private static bool IsKnownAwaitableType(IOperation? instance, params INamedTypeSymbol?[] expectedTypes)
    {
        if (instance is null)
        {
            return false;
        }

        var type = instance.Type;
        if (type is null)
        {
            return false;
        }

        foreach (var expectedType in expectedTypes)
        {
            if (expectedType is null)
            {
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(type, expectedType))
            {
                return true;
            }

            if (type.OriginalDefinition is not null
                && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, expectedType))
            {
                return true;
            }
        }

        return false;
    }

    private static Location GetMemberLocation(SyntaxNode syntax)
    {
        return syntax switch
        {
            InvocationExpressionSyntax invocation => invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name.GetLocation(),
                _ => invocation.GetLocation(),
            },
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),
            _ => syntax.GetLocation(),
        };
    }
}
