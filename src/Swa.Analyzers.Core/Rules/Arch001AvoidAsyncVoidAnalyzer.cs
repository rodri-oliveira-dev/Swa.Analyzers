using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch001AvoidAsyncVoidAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Reliability";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AvoidAsyncVoid,
        title: "Avoid async void outside event handlers",
        messageFormat: "Avoid async void in '{0}'. Use async Task instead.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Async void methods and anonymous functions cannot be awaited and propagate exceptions differently. Prefer async Task, except for event handlers with a standard event signature.",
        helpLinkUri: "docs/rules/ARCH001.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);
        context.RegisterOperationAction(AnalyzeAnonymousFunction, OperationKind.AnonymousFunction);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (!method.IsAsync || !ReturnsVoid(method))
        {
            return;
        }

        if (method.MethodKind is not MethodKind.Ordinary)
        {
            return;
        }

        if (IsStandardEventHandler(method, context.Compilation))
        {
            return;
        }

        if (method.Locations.IsDefaultOrEmpty)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations[0], method.Name));
    }

    private static void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
    {
        var localFunction = (LocalFunctionStatementSyntax)context.Node;

        var method = context.SemanticModel.GetDeclaredSymbol(localFunction, context.CancellationToken);
        if (method is null)
        {
            return;
        }

        if (!method.IsAsync || !ReturnsVoid(method))
        {
            return;
        }

        if (IsStandardEventHandler(method, context.Compilation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, localFunction.Identifier.GetLocation(), method.Name));
    }

    private static void AnalyzeAnonymousFunction(OperationAnalysisContext context)
    {
        var anonymousFunction = (IAnonymousFunctionOperation)context.Operation;
        var symbol = anonymousFunction.Symbol;

        if (symbol is null || !symbol.IsAsync || !ReturnsVoid(symbol))
        {
            return;
        }

        if (IsStandardEventHandler(symbol, context.Compilation))
        {
            return;
        }

        var location = GetAnonymousFunctionLocation(anonymousFunction.Syntax);
        var displayName = string.IsNullOrWhiteSpace(symbol.Name) ? "anonymous function" : symbol.Name;
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, displayName));
    }

    private static bool ReturnsVoid(IMethodSymbol method) =>
        method.ReturnsVoid ||
        method.ReturnType.SpecialType == SpecialType.System_Void;

    private static bool IsStandardEventHandler(IMethodSymbol method, Compilation compilation)
    {
        if (method.Parameters.Length != 2)
        {
            return false;
        }

        if (method.Parameters[0].Type.SpecialType != SpecialType.System_Object)
        {
            return false;
        }

        var eventArgsSymbol = compilation.GetTypeByMetadataName(typeof(System.EventArgs).FullName!);
        if (eventArgsSymbol is null)
        {
            return false;
        }

        var secondParameterType = method.Parameters[1].Type;

        return SymbolEqualityComparer.Default.Equals(secondParameterType, eventArgsSymbol)
            || InheritsFrom(secondParameterType, eventArgsSymbol);
    }

    private static bool InheritsFrom(ITypeSymbol candidate, ITypeSymbol baseType)
    {
        for (var current = candidate.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }

    private static Location GetAnonymousFunctionLocation(SyntaxNode syntax)
    {
        return syntax switch
        {
            AnonymousFunctionExpressionSyntax anonymousFunction when anonymousFunction.AsyncKeyword != default
                => anonymousFunction.AsyncKeyword.GetLocation(),
            _ => syntax.GetLocation()
        };
    }
}
