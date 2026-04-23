using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Reliability";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.ProhibitAsyncOrBlockingInConstructors,
        title: "Prohibit asynchronous or blocking logic in constructors",
        messageFormat: "Avoid {0} in constructors. Constructors should not perform asynchronous or blocking operations. Consider using an async factory method.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Constructors should not contain blocking calls (.Result, .Wait(), .GetAwaiter().GetResult()) or unawaited asynchronous operations. Use an async factory method instead.",
        helpLinkUri: "docs/rules/ARCH011.md");

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

            compilationContext.RegisterOperationBlockStartAction(blockContext =>
            {
                var method = blockContext.OwningSymbol as IMethodSymbol;
                if (method?.MethodKind is not (MethodKind.Constructor or MethodKind.SharedConstructor))
                {
                    return;
                }

                blockContext.RegisterOperationAction(
                    context => AnalyzePropertyReference(context, taskOfTType, valueTaskOfTType),
                    OperationKind.PropertyReference);

                blockContext.RegisterOperationAction(
                    context => AnalyzeInvocation(context, taskType, taskOfTType, valueTaskType, valueTaskOfTType),
                    OperationKind.Invocation);
            });
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
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, "synchronous blocking with .Result"));
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

        // Check for .Wait()
        if (string.Equals(targetMethod.Name, "Wait", StringComparison.Ordinal)
            && targetMethod.Parameters.Length <= 1)
        {
            if (IsKnownAwaitableType(invocation.Instance, taskType, taskOfTType))
            {
                var location = GetMemberLocation(invocation.Syntax);
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, "synchronous blocking with .Wait()"));
            }

            return;
        }

        // Check for .GetAwaiter().GetResult()
        if (string.Equals(targetMethod.Name, "GetResult", StringComparison.Ordinal)
            && targetMethod.Parameters.IsEmpty)
        {
            if (invocation.Instance is IInvocationOperation getAwaiterInvocation
                && string.Equals(getAwaiterInvocation.TargetMethod.Name, "GetAwaiter", StringComparison.Ordinal)
                && getAwaiterInvocation.TargetMethod.Parameters.IsEmpty
                && IsKnownAwaitableType(getAwaiterInvocation.Instance, taskType, taskOfTType, valueTaskType, valueTaskOfTType))
            {
                var location = GetMemberLocation(invocation.Syntax);
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, "synchronous blocking with .GetAwaiter().GetResult()"));
            }

            return;
        }

        // Check for unawaited async calls
        if (IsKnownAwaitableType(invocation.Type, taskType, taskOfTType, valueTaskType, valueTaskOfTType))
        {
            if (ShouldReportUnawaitedAsync(invocation))
            {
                var location = GetMemberLocation(invocation.Syntax);
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, "unawaited asynchronous calls"));
            }
        }
    }

    private static bool ShouldReportUnawaitedAsync(IInvocationOperation invocation)
    {
        var parent = invocation.Parent;
        while (parent is not null)
        {
            if (parent is IAwaitOperation)
            {
                return false;
            }

            if (parent is ISimpleAssignmentOperation or IVariableInitializerOperation)
            {
                return false;
            }

            if (parent is IArgumentOperation)
            {
                return false;
            }

            if (parent is IExpressionStatementOperation)
            {
                return true;
            }

            if (parent is IInvocationOperation or IMemberReferenceOperation)
            {
                parent = parent.Parent;
                continue;
            }

            break;
        }

        return false;
    }

    private static bool IsKnownAwaitableType(IOperation? instance, params INamedTypeSymbol?[] expectedTypes)
    {
        if (instance is null)
        {
            return false;
        }

        return IsKnownAwaitableType(instance.Type, expectedTypes);
    }

    private static bool IsKnownAwaitableType(ITypeSymbol? type, params INamedTypeSymbol?[] expectedTypes)
    {
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
