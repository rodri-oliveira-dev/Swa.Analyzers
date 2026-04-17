using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch002AvoidTaskContinueWithAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Reliability";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AvoidTaskContinueWith,
        title: "Avoid Task.ContinueWith",
        messageFormat: "Avoid Task.ContinueWith. Prefer 'await' for readability, exception propagation and maintainability.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Task.ContinueWith makes async control flow harder to read and reason about. Prefer 'await' to keep code linear, preserve exception propagation through the returned Task, and improve long-term maintenance.",
        helpLinkUri: "docs/rules/ARCH002.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var taskType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            if (taskType is null)
            {
                return;
            }

            var taskOfTType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

            compilationContext.RegisterOperationAction(
                context => AnalyzeInvocation(context, taskType, taskOfTType),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol taskType,
        INamedTypeSymbol? taskOfTType)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod;

        if (!string.Equals(targetMethod.Name, "ContinueWith", StringComparison.Ordinal))
        {
            return;
        }

        var containingType = targetMethod.ContainingType;

        var isTaskContinueWith = SymbolEqualityComparer.Default.Equals(containingType, taskType);
        var isTaskOfTContinueWith = taskOfTType is not null
            && SymbolEqualityComparer.Default.Equals(containingType.OriginalDefinition, taskOfTType);

        if (!isTaskContinueWith && !isTaskOfTContinueWith)
        {
            return;
        }

        var location = GetContinueWithLocation(invocation.Syntax);
        context.ReportDiagnostic(Diagnostic.Create(Rule, location));
    }

    private static Location GetContinueWithLocation(SyntaxNode syntax)
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
