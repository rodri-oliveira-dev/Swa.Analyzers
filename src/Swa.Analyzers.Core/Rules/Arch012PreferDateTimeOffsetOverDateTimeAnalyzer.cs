using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch012PreferDateTimeOffsetOverDateTimeAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Reliability";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.PreferDateTimeOffsetOverDateTime,
        title: "Prefer DateTimeOffset over DateTime",
        messageFormat: "Prefer DateTimeOffset over DateTime. DateTime is ambiguous about time zone intent.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "DateTime does not carry time zone offset information, making it ambiguous whether the value is local, UTC, or unspecified. DateTimeOffset removes this ambiguity and is preferred for most business and persistence scenarios.",
        helpLinkUri: "docs/rules/ARCH012.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var dateTimeType = compilationContext.Compilation.GetTypeByMetadataName("System.DateTime");
            var attributeType = compilationContext.Compilation.GetTypeByMetadataName("System.Attribute");

            if (dateTimeType is null)
            {
                return;
            }

            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeField(symbolContext, dateTimeType, attributeType),
                SymbolKind.Field);

            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeProperty(symbolContext, dateTimeType, attributeType),
                SymbolKind.Property);

            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeMethod(symbolContext, dateTimeType, attributeType),
                SymbolKind.Method);

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeVariableDeclaration(syntaxContext, dateTimeType),
                SyntaxKind.VariableDeclaration);
        });
    }

    private static void AnalyzeField(SymbolAnalysisContext context, INamedTypeSymbol dateTimeType, INamedTypeSymbol? attributeType)
    {
        var field = (IFieldSymbol)context.Symbol;

        if (!IsDateTimeType(field.Type, dateTimeType))
        {
            return;
        }

        if (IsInsideAttributeType(field, attributeType))
        {
            return;
        }

        var location = GetFieldTypeLocation(field);
        if (location is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, location));
        }
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context, INamedTypeSymbol dateTimeType, INamedTypeSymbol? attributeType)
    {
        var property = (IPropertySymbol)context.Symbol;

        if (!IsDateTimeType(property.Type, dateTimeType))
        {
            return;
        }

        if (property.IsOverride)
        {
            return;
        }

        if (property.ExplicitInterfaceImplementations.Length > 0)
        {
            return;
        }

        if (IsImplicitInterfaceImplementation(property))
        {
            return;
        }

        if (IsInsideAttributeType(property, attributeType))
        {
            return;
        }

        var location = GetPropertyTypeLocation(property);
        if (location is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, location));
        }
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol dateTimeType, INamedTypeSymbol? attributeType)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (IsInsideAttributeType(method, attributeType))
        {
            return;
        }

        if (method.IsOverride)
        {
            return;
        }

        if (method.ExplicitInterfaceImplementations.Length > 0)
        {
            return;
        }

        if (IsImplicitInterfaceImplementation(method))
        {
            return;
        }

        // Analyze return type
        if (IsDateTimeType(method.ReturnType, dateTimeType))
        {
            var location = GetReturnTypeLocation(method);
            if (location is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, location));
            }
        }

        // Analyze parameters
        foreach (var parameter in method.Parameters)
        {
            if (!IsDateTimeType(parameter.Type, dateTimeType))
            {
                continue;
            }

            if (parameter.IsThis)
            {
                continue;
            }

            if (IsThisParameterBySyntax(parameter))
            {
                continue;
            }

            var location = GetParameterTypeLocation(parameter);
            if (location is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, location));
            }
        }
    }

    private static void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context, INamedTypeSymbol dateTimeType)
    {
        var declaration = (VariableDeclarationSyntax)context.Node;

        // Only local variable declarations (fields are handled by symbol analysis)
        if (declaration.Parent is not (LocalDeclarationStatementSyntax or ForStatementSyntax))
        {
            return;
        }

        // Skip 'var' declarations to avoid noise where the type is inferred
        if (declaration.Type is IdentifierNameSyntax identifierName && identifierName.Identifier.Text == "var")
        {
            return;
        }

        var semanticModel = context.SemanticModel;
        var typeSymbol = semanticModel.GetSymbolInfo(declaration.Type, context.CancellationToken).Symbol as ITypeSymbol;

        if (typeSymbol is not null && IsDateTimeType(typeSymbol, dateTimeType))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, declaration.Type.GetLocation()));
        }
    }

    private static bool IsDateTimeType(ITypeSymbol? type, INamedTypeSymbol dateTimeType)
    {
        if (type is null)
        {
            return false;
        }

        if (SymbolEqualityComparer.Default.Equals(type, dateTimeType))
        {
            return true;
        }

        // Nullable<DateTime>
        if (type is INamedTypeSymbol namedType
            && namedType.IsGenericType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && namedType.TypeArguments.Length == 1)
        {
            if (SymbolEqualityComparer.Default.Equals(namedType.TypeArguments[0], dateTimeType))
            {
                return true;
            }
        }

        // DateTime[]
        if (type is IArrayTypeSymbol arrayType)
        {
            return IsDateTimeType(arrayType.ElementType, dateTimeType);
        }

        return false;
    }

    private static bool IsInsideAttributeType(ISymbol symbol, INamedTypeSymbol? attributeType)
    {
        if (attributeType is null)
        {
            return false;
        }

        var containingType = symbol.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        for (var current = containingType.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, attributeType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsImplicitInterfaceImplementation(IPropertySymbol property)
    {
        var containingType = property.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        foreach (var interfaceType in containingType.AllInterfaces)
        {
            foreach (var member in interfaceType.GetMembers(property.Name))
            {
                var implementation = containingType.FindImplementationForInterfaceMember(member);
                if (implementation is not null && SymbolEqualityComparer.Default.Equals(implementation, property))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsImplicitInterfaceImplementation(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        foreach (var interfaceType in containingType.AllInterfaces)
        {
            foreach (var member in interfaceType.GetMembers(method.Name))
            {
                if (member is not IMethodSymbol interfaceMethod)
                {
                    continue;
                }

                var implementation = containingType.FindImplementationForInterfaceMember(interfaceMethod);
                if (implementation is not null && SymbolEqualityComparer.Default.Equals(implementation, method))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Location? GetFieldTypeLocation(IFieldSymbol field)
    {
        if (field.DeclaringSyntaxReferences.IsEmpty)
        {
            return null;
        }

        foreach (var syntaxRef in field.DeclaringSyntaxReferences)
        {
            var syntax = syntaxRef.GetSyntax();
            if (syntax is VariableDeclaratorSyntax declarator && declarator.Parent is VariableDeclarationSyntax declaration)
            {
                return declaration.Type.GetLocation();
            }

            if (syntax is FieldDeclarationSyntax fieldDecl)
            {
                return fieldDecl.Declaration.Type.GetLocation();
            }
        }

        return null;
    }

    private static Location? GetPropertyTypeLocation(IPropertySymbol property)
    {
        if (property.DeclaringSyntaxReferences.IsEmpty)
        {
            return null;
        }

        foreach (var syntaxRef in property.DeclaringSyntaxReferences)
        {
            var syntax = syntaxRef.GetSyntax();
            if (syntax is PropertyDeclarationSyntax propDecl)
            {
                return propDecl.Type.GetLocation();
            }
        }

        return null;
    }

    private static Location? GetReturnTypeLocation(IMethodSymbol method)
    {
        if (method.DeclaringSyntaxReferences.IsEmpty)
        {
            return null;
        }

        foreach (var syntaxRef in method.DeclaringSyntaxReferences)
        {
            var syntax = syntaxRef.GetSyntax();
            if (syntax is MethodDeclarationSyntax methodDecl)
            {
                return methodDecl.ReturnType.GetLocation();
            }

            if (syntax is LocalFunctionStatementSyntax localFunction)
            {
                return localFunction.ReturnType.GetLocation();
            }
        }

        return null;
    }

    private static Location? GetParameterTypeLocation(IParameterSymbol parameter)
    {
        if (parameter.DeclaringSyntaxReferences.IsEmpty)
        {
            return null;
        }

        foreach (var syntaxRef in parameter.DeclaringSyntaxReferences)
        {
            var syntax = syntaxRef.GetSyntax();
            if (syntax is ParameterSyntax paramSyntax)
            {
                return paramSyntax.Type?.GetLocation();
            }
        }

        return null;
    }

    private static bool IsThisParameterBySyntax(IParameterSymbol parameter)
    {
        if (parameter.DeclaringSyntaxReferences.IsEmpty)
        {
            return false;
        }

        foreach (var syntaxRef in parameter.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is ParameterSyntax paramSyntax)
            {
                return paramSyntax.Modifiers.Any(SyntaxKind.ThisKeyword);
            }
        }

        return false;
    }
}
