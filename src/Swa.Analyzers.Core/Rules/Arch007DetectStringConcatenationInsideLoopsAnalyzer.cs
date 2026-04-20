using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch007DetectStringConcatenationInsideLoopsAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Performance";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.DetectStringConcatenationInsideLoops,
        title: "Detect string concatenation inside loops",
        messageFormat: "Avoid string concatenation for '{0}' inside loops. Consider using StringBuilder.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Repeated string concatenation inside loops can cause excessive allocations (often quadratic growth). Prefer StringBuilder or other buffering approaches when building strings iteratively.",
        helpLinkUri: "docs/rules/ARCH007.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeLoop, OperationKind.Loop);
    }

    private static void AnalyzeLoop(OperationAnalysisContext context)
    {
        var loop = (ILoopOperation)context.Operation;

        // Reduce noise for loops that are used as a scope, or that are known to
        // not execute (while/for false), or run only once (do/while false).
        // Examples:
        //   while (false) { ... }       // never runs
        //   do { ... } while (false);  // runs once
        if (HasConstantFalseCondition(loop))
        {
            return;
        }

        var body = loop.Body;
        if (body is null)
        {
            return;
        }

        // Report at most once per target symbol per loop to avoid spamming diagnostics.
        var reportedTargets = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

        var stack = new Stack<IOperation>();
        stack.Push(body);

        while (stack.Count > 0)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var current = stack.Pop();

            if (current is ILoopOperation)
            {
                // Nested loops are handled by their own callbacks.
                continue;
            }

            if (current is IAnonymousFunctionOperation || current is ILocalFunctionOperation)
            {
                // Avoid false positives in delayed execution contexts.
                continue;
            }

            if (current is ICompoundAssignmentOperation compoundAssignment
                && IsStringConcatenationCompoundAssignment(compoundAssignment, out var compoundTarget))
            {
                if (ShouldReport(loop, body.Syntax, compoundTarget)
                    && reportedTargets.Add(compoundTarget))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule,
                        GetAssignmentTargetLocation(compoundAssignment.Target.Syntax),
                        compoundTarget.Name));
                }
            }
            else if (current is ISimpleAssignmentOperation simpleAssignment
                && IsStringConcatenationSimpleAssignment(simpleAssignment, out var simpleTarget))
            {
                if (ShouldReport(loop, body.Syntax, simpleTarget)
                    && reportedTargets.Add(simpleTarget))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule,
                        GetAssignmentTargetLocation(simpleAssignment.Target.Syntax),
                        simpleTarget.Name));
                }
            }

            foreach (var child in current.ChildOperations)
            {
                stack.Push(child);
            }
        }
    }

    private static bool HasConstantFalseCondition(ILoopOperation loop)
    {
        if (loop.Syntax is DoStatementSyntax doStatement
            && doStatement.Condition.IsKind(SyntaxKind.FalseLiteralExpression))
        {
            // Intentionally treat do/while(false) as a trivial loop (runs once).
            return true;
        }

        // ILoopOperation doesn't expose a universal Condition for all loop kinds.
        // We intentionally keep this heuristic narrow and only treat a literal `false`
        // (or equivalent constant) as a special case.
        IOperation? condition = loop switch
        {
            IWhileLoopOperation whileLoop => whileLoop.Condition,
            IForLoopOperation forLoop => forLoop.Condition,
            _ => null,
        };

        if (condition is null)
        {
            return false;
        }

        var constant = condition.ConstantValue;
        return constant.HasValue && constant.Value is bool value && value is false;
    }

    private static bool ShouldReport(ILoopOperation loop, SyntaxNode loopBodySyntax, ISymbol target)
    {
        // False-positive reduction: ignore locals declared inside the loop body,
        // because they're re-initialized per iteration and usually indicate a per-item
        // formatting operation rather than incremental string building.
        if (target is ILocalSymbol local && IsLocalDeclaredInsideLoopBody(local, loopBodySyntax))
        {
            return false;
        }

        return true;
    }

    private static bool IsStringConcatenationCompoundAssignment(
        ICompoundAssignmentOperation compoundAssignment,
        out ISymbol target)
    {
        target = null!;

        if (compoundAssignment.OperatorKind != BinaryOperatorKind.Add)
        {
            return false;
        }

        return TryGetStringTargetSymbol(compoundAssignment.Target, out target);
    }

    private static bool IsStringConcatenationSimpleAssignment(
        ISimpleAssignmentOperation simpleAssignment,
        out ISymbol target)
    {
        target = null!;

        if (!TryGetStringTargetSymbol(simpleAssignment.Target, out target))
        {
            return false;
        }

        if (simpleAssignment.Value is null)
        {
            return false;
        }

        // Pattern 1: s = s + expr  (and variants like: s = s + a + b)
        if (TryGetBinaryStringAdd(simpleAssignment.Value, out var binaryAdd)
            && ContainsTargetReference(binaryAdd, target))
        {
            return true;
        }

        // Pattern 2: s = $"{s}{expr}" (interpolated strings)
        if (TryGetOperation<IInterpolatedStringOperation>(simpleAssignment.Value, out var interpolatedString)
            && ContainsTargetReference(interpolatedString, target))
        {
            return true;
        }

        // Pattern 3: s = string.Concat(s, expr)
        if (TryGetOperation<IInvocationOperation>(simpleAssignment.Value, out var invocation)
            && IsSystemStringConcat(invocation.TargetMethod)
            && InvocationArgumentsContainTargetReference(invocation, target))
        {
            return true;
        }

        return false;
    }

    private static bool IsSystemStringConcat(IMethodSymbol method)
    {
        if (!string.Equals(method.Name, "Concat", StringComparison.Ordinal))
        {
            return false;
        }

        return method.ContainingType is { SpecialType: SpecialType.System_String };
    }

    private static bool InvocationArgumentsContainTargetReference(IInvocationOperation invocation, ISymbol target)
    {
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Value is not null && ContainsTargetReference(argument.Value, target))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetBinaryStringAdd(IOperation operation, out IBinaryOperation binaryAdd)
    {
        binaryAdd = null!;

        if (!TryGetOperation<IBinaryOperation>(operation, out var binaryOperation))
        {
            return false;
        }

        if (binaryOperation.OperatorKind != BinaryOperatorKind.Add)
        {
            return false;
        }

        if (binaryOperation.Type?.SpecialType != SpecialType.System_String)
        {
            return false;
        }

        binaryAdd = binaryOperation;
        return true;
    }

    private static bool ContainsTargetReference(IOperation operation, ISymbol target)
    {
        var stack = new Stack<IOperation>();
        stack.Push(operation);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (IsTargetReference(current, target))
            {
                return true;
            }

            if (current is IAnonymousFunctionOperation || current is ILocalFunctionOperation)
            {
                // Do not traverse delayed execution contexts.
                continue;
            }

            foreach (var child in current.ChildOperations)
            {
                stack.Push(child);
            }
        }

        return false;
    }

    private static bool IsTargetReference(IOperation operation, ISymbol target)
    {
        return operation switch
        {
            ILocalReferenceOperation localRef
                => SymbolEqualityComparer.Default.Equals(localRef.Local, target),
            IFieldReferenceOperation fieldRef
                => SymbolEqualityComparer.Default.Equals(fieldRef.Field, target),
            _ => false,
        };
    }

    private static bool TryGetStringTargetSymbol(IOperation targetOperation, out ISymbol target)
    {
        target = null!;

        switch (targetOperation)
        {
            case ILocalReferenceOperation localRef when localRef.Local.Type.SpecialType == SpecialType.System_String:
                target = localRef.Local;
                return true;

            case IFieldReferenceOperation fieldRef when fieldRef.Field.Type.SpecialType == SpecialType.System_String:
                target = fieldRef.Field;
                return true;
        }

        return false;
    }

    private static bool IsLocalDeclaredInsideLoopBody(ILocalSymbol local, SyntaxNode loopBodySyntax)
    {
        if (local.DeclaringSyntaxReferences.IsDefaultOrEmpty)
        {
            return false;
        }

        var declarationSyntax = local.DeclaringSyntaxReferences[0].GetSyntax();
        return loopBodySyntax.Span.Contains(declarationSyntax.SpanStart);
    }

    private static bool TryGetOperation<TOperation>(IOperation operation, out TOperation result)
        where TOperation : class, IOperation
    {
        result = null!;

        IOperation? current = operation;
        while (current is not null)
        {
            if (current is TOperation typed)
            {
                result = typed;
                return true;
            }

            if (current is IConversionOperation conversion)
            {
                current = conversion.Operand;
                continue;
            }

            if (current is IParenthesizedOperation parenthesized)
            {
                current = parenthesized.Operand;
                continue;
            }

            break;
        }

        return false;
    }

    private static Location GetAssignmentTargetLocation(SyntaxNode syntax)
    {
        return syntax switch
        {
            IdentifierNameSyntax identifierName => identifierName.Identifier.GetLocation(),
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),
            MemberBindingExpressionSyntax memberBinding => memberBinding.Name.GetLocation(),
            _ => syntax.GetLocation(),
        };
    }
}
