using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch013RestrictMockingFrameworksToNSubstituteAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "TestQuality";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.RestrictMockingFrameworksToNSubstitute,
        title: "Restrict mocking frameworks to NSubstitute",
        messageFormat: "Mocking framework '{0}' is not allowed by policy. Use NSubstitute instead.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "To keep tests consistent and reduce maintenance cost, teams often standardize on a single mocking framework. This rule reports usages of known alternative mocking frameworks when the policy standard is NSubstitute.",
        helpLinkUri: "docs/rules/ARCH013.md");

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

            var presentFrameworks = GetPresentDisallowedFrameworks(compilationContext.Compilation);
            if (presentFrameworks.IsDefaultOrEmpty)
            {
                // Fast exit when no known disallowed mocking framework is referenced.
                return;
            }

            var frameworksByRootNamespace = presentFrameworks
                .ToImmutableDictionary(static x => x.RootNamespace, static x => x.Name, StringComparer.Ordinal);

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, frameworksByRootNamespace),
                OperationKind.Invocation);

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeObjectCreation(operationContext, frameworksByRootNamespace),
                OperationKind.ObjectCreation);

            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeField(symbolContext, frameworksByRootNamespace),
                SymbolKind.Field);

            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeProperty(symbolContext, frameworksByRootNamespace),
                SymbolKind.Property);

            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeMethod(symbolContext, frameworksByRootNamespace),
                SymbolKind.Method);

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeVariableDeclaration(syntaxContext, frameworksByRootNamespace),
                SyntaxKind.VariableDeclaration);

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeUsingDirective(syntaxContext, frameworksByRootNamespace),
                SyntaxKind.UsingDirective);
        });
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        ImmutableDictionary<string, string> frameworksByRootNamespace)
    {
        if (IsDeclaredInsideDisallowedFramework(context.ContainingSymbol, frameworksByRootNamespace))
        {
            // Avoid reporting inside the framework itself (relevant for tests that stub the framework in-source).
            return;
        }

        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (method.ContainingType is null)
        {
            return;
        }

        if (!TryGetDisallowedFrameworkName(method.ContainingType.ContainingNamespace, frameworksByRootNamespace, out var frameworkName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, GetInvocationMemberNameLocation(invocation.Syntax), frameworkName));
    }

    private static void AnalyzeObjectCreation(
        OperationAnalysisContext context,
        ImmutableDictionary<string, string> frameworksByRootNamespace)
    {
        if (IsDeclaredInsideDisallowedFramework(context.ContainingSymbol, frameworksByRootNamespace))
        {
            return;
        }

        var creation = (IObjectCreationOperation)context.Operation;
        var createdType = creation.Type;

        if (createdType is null)
        {
            return;
        }

        if (!TryGetDisallowedFrameworkName(createdType.ContainingNamespace, frameworksByRootNamespace, out var frameworkName))
        {
            return;
        }

        if (creation.Syntax is ImplicitObjectCreationExpressionSyntax && ShouldSkipImplicitNewDiagnostic(creation.Syntax))
        {
            // Avoid double-reporting when the type is already explicit at the declaration site
            // (e.g., `Moq.Mock<IFoo> mock = new();`). In those cases, the declaration type is already reported.
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, GetObjectCreationTypeLocation(creation.Syntax), frameworkName));
    }

    private static void AnalyzeField(SymbolAnalysisContext context, ImmutableDictionary<string, string> frameworksByRootNamespace)
    {
        var field = (IFieldSymbol)context.Symbol;

        if (IsDeclaredInsideDisallowedFramework(field, frameworksByRootNamespace))
        {
            return;
        }

        if (!TryGetDisallowedFrameworkName(field.Type, frameworksByRootNamespace, out var frameworkName))
        {
            return;
        }

        var location = GetFieldTypeLocation(field);
        if (location is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, frameworkName));
        }
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context, ImmutableDictionary<string, string> frameworksByRootNamespace)
    {
        var property = (IPropertySymbol)context.Symbol;

        if (IsDeclaredInsideDisallowedFramework(property, frameworksByRootNamespace))
        {
            return;
        }

        if (!TryGetDisallowedFrameworkName(property.Type, frameworksByRootNamespace, out var frameworkName))
        {
            return;
        }

        var location = GetPropertyTypeLocation(property);
        if (location is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, frameworkName));
        }
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, ImmutableDictionary<string, string> frameworksByRootNamespace)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (IsDeclaredInsideDisallowedFramework(method, frameworksByRootNamespace))
        {
            return;
        }

        if (method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet)
        {
            // Property types are handled by AnalyzeProperty.
            return;
        }

        if (method.MethodKind is MethodKind.EventAdd or MethodKind.EventRemove)
        {
            // Event accessors are not interesting for this rule.
            return;
        }

        // Return type
        if (TryGetDisallowedFrameworkName(method.ReturnType, frameworksByRootNamespace, out var returnFrameworkName))
        {
            var location = GetReturnTypeLocation(method);
            if (location is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, returnFrameworkName));
            }
        }

        // Parameters
        foreach (var parameter in method.Parameters)
        {
            if (!TryGetDisallowedFrameworkName(parameter.Type, frameworksByRootNamespace, out var frameworkName))
            {
                continue;
            }

            var location = GetParameterTypeLocation(parameter);
            if (location is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, frameworkName));
            }
        }
    }

    private static void AnalyzeVariableDeclaration(
        SyntaxNodeAnalysisContext context,
        ImmutableDictionary<string, string> frameworksByRootNamespace)
    {
        if (IsDeclaredInsideDisallowedFramework(context.ContainingSymbol, frameworksByRootNamespace))
        {
            return;
        }

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

        var typeSymbol = context.SemanticModel
            .GetSymbolInfo(declaration.Type, context.CancellationToken)
            .Symbol as ITypeSymbol;

        if (typeSymbol is null)
        {
            return;
        }

        if (!TryGetDisallowedFrameworkName(typeSymbol, frameworksByRootNamespace, out var frameworkName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, declaration.Type.GetLocation(), frameworkName));
    }

    private static void AnalyzeUsingDirective(
        SyntaxNodeAnalysisContext context,
        ImmutableDictionary<string, string> frameworksByRootNamespace)
    {
        if (IsDeclaredInsideDisallowedFramework(context.ContainingSymbol, frameworksByRootNamespace))
        {
            return;
        }

        var usingDirective = (UsingDirectiveSyntax)context.Node;

        if (usingDirective.Name is null)
        {
            return;
        }

        // `using X = ...;` still uses Name to represent the imported namespace/type.
        var symbol = context.SemanticModel.GetSymbolInfo(usingDirective.Name, context.CancellationToken).Symbol;
        if (symbol is null)
        {
            return;
        }

        string? frameworkName = null;

        switch (symbol)
        {
            case INamespaceSymbol ns:
                if (!TryGetDisallowedFrameworkName(ns, frameworksByRootNamespace, out var nsFramework))
                {
                    return;
                }

                frameworkName = nsFramework;
                break;

            case ITypeSymbol type:
                if (!TryGetDisallowedFrameworkName(type, frameworksByRootNamespace, out var typeFramework))
                {
                    return;
                }

                frameworkName = typeFramework;
                break;

            default:
                return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            GetUsingDirectiveRootNameLocation(usingDirective.Name),
            frameworkName));
    }

    private static bool TryGetDisallowedFrameworkName(
        ITypeSymbol? type,
        ImmutableDictionary<string, string> frameworksByRootNamespace,
        out string frameworkName)
    {
        frameworkName = null!;

        if (type is null)
        {
            return false;
        }

        // Arrays (e.g., Mock<T>[])
        if (type is IArrayTypeSymbol arrayType)
        {
            return TryGetDisallowedFrameworkName(arrayType.ElementType, frameworksByRootNamespace, out frameworkName);
        }

        // Nullable<T>
        if (type is INamedTypeSymbol namedType
            && namedType.IsGenericType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && namedType.TypeArguments.Length == 1)
        {
            return TryGetDisallowedFrameworkName(namedType.TypeArguments[0], frameworksByRootNamespace, out frameworkName);
        }

        return TryGetDisallowedFrameworkName(type.ContainingNamespace, frameworksByRootNamespace, out frameworkName);
    }

    private static bool TryGetDisallowedFrameworkName(
        INamespaceSymbol? @namespace,
        ImmutableDictionary<string, string> frameworksByRootNamespace,
        out string frameworkName)
    {
        frameworkName = null!;

        for (var current = @namespace; current is not null && !current.IsGlobalNamespace; current = current.ContainingNamespace)
        {
            if (!current.ContainingNamespace.IsGlobalNamespace)
            {
                continue;
            }

            if (frameworksByRootNamespace.TryGetValue(current.Name, out var name) && name is not null)
            {
                frameworkName = name;
                return true;
            }

            return false;
        }

        return false;
    }

    private static bool IsDeclaredInsideDisallowedFramework(ISymbol? symbol, ImmutableDictionary<string, string> frameworksByRootNamespace)
    {
        if (symbol is null)
        {
            return false;
        }

        return TryGetDisallowedFrameworkName(symbol.ContainingNamespace, frameworksByRootNamespace, out _);
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
            if (syntaxRef.GetSyntax() is PropertyDeclarationSyntax propDecl)
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
            if (syntaxRef.GetSyntax() is ParameterSyntax paramSyntax)
            {
                return paramSyntax.Type?.GetLocation();
            }
        }

        return null;
    }

    private static ImmutableArray<INamedTypeSymbol> GetKnownTestMethodAttributes(Compilation compilation)
    {
        // Same “test project” gate used in other TestQuality rules.
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

    private static ImmutableArray<DisallowedMockFramework> GetPresentDisallowedFrameworks(Compilation compilation)
    {
        var builder = ImmutableArray.CreateBuilder<DisallowedMockFramework>();

        foreach (var framework in DisallowedMockFramework.All)
        {
            if (framework.IsPresent(compilation))
            {
                builder.Add(framework);
            }
        }

        return builder.ToImmutable();
    }

    private static Location GetInvocationMemberNameLocation(SyntaxNode syntax)
    {
        return syntax switch
        {
            InvocationExpressionSyntax invocation => invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name switch
                {
                    IdentifierNameSyntax identifierName => identifierName.Identifier.GetLocation(),
                    GenericNameSyntax genericName => genericName.Identifier.GetLocation(),
                    _ => memberAccess.Name.GetLocation(),
                },
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name switch
                {
                    IdentifierNameSyntax identifierName => identifierName.Identifier.GetLocation(),
                    GenericNameSyntax genericName => genericName.Identifier.GetLocation(),
                    _ => memberBinding.Name.GetLocation(),
                },
                IdentifierNameSyntax identifierName => identifierName.Identifier.GetLocation(),
                GenericNameSyntax genericName => genericName.Identifier.GetLocation(),
                _ => invocation.GetLocation(),
            },
            _ => syntax.GetLocation(),
        };
    }

    private static bool ShouldSkipImplicitNewDiagnostic(SyntaxNode implicitObjectCreationSyntax)
    {
        // We only want to skip when there is an explicit type declaration near the `new()` site,
        // because the symbol analyzers / variable declaration analysis already report the same framework.

        // Local/field variable declaration: `SomeType x = new();`
        if (implicitObjectCreationSyntax.Parent is EqualsValueClauseSyntax equalsValue
            && equalsValue.Parent is VariableDeclaratorSyntax declarator
            && declarator.Parent is VariableDeclarationSyntax variableDeclaration)
        {
            return !IsVarTypeSyntax(variableDeclaration.Type);
        }

        // Property initializer: `SomeType X { get; } = new();`
        if (implicitObjectCreationSyntax.Parent is EqualsValueClauseSyntax propertyEqualsValue
            && propertyEqualsValue.Parent is PropertyDeclarationSyntax propertyDeclaration)
        {
            return !IsVarTypeSyntax(propertyDeclaration.Type);
        }

        // Parameter default: `SomeType x = new()`
        if (implicitObjectCreationSyntax.Parent is EqualsValueClauseSyntax parameterEqualsValue
            && parameterEqualsValue.Parent is ParameterSyntax parameterSyntax
            && parameterSyntax.Type is not null)
        {
            return !IsVarTypeSyntax(parameterSyntax.Type);
        }

        return false;
    }

    private static bool IsVarTypeSyntax(TypeSyntax typeSyntax)
    {
        return typeSyntax is IdentifierNameSyntax identifierName
            && string.Equals(identifierName.Identifier.Text, "var", StringComparison.Ordinal);
    }

    private static Location GetObjectCreationTypeLocation(SyntaxNode syntax)
    {
        // For regular object creation, report on the explicit type.
        // For target-typed `new()` (implicit object creation), fall back to the `new` keyword.
        return syntax switch
        {
            ObjectCreationExpressionSyntax creation => GetTypeSyntaxNameLocation(creation.Type),
            ImplicitObjectCreationExpressionSyntax implicitCreation => implicitCreation.NewKeyword.GetLocation(),
            _ => syntax.GetLocation(),
        };
    }

    private static Location GetTypeSyntaxNameLocation(TypeSyntax typeSyntax)
    {
        return typeSyntax switch
        {
            QualifiedNameSyntax qualified => GetTypeSyntaxNameLocation(qualified.Right),
            AliasQualifiedNameSyntax aliasQualified => GetTypeSyntaxNameLocation(aliasQualified.Name),
            IdentifierNameSyntax identifier => identifier.Identifier.GetLocation(),
            GenericNameSyntax generic => generic.Identifier.GetLocation(),
            _ => typeSyntax.GetLocation(),
        };
    }

    private static Location GetUsingDirectiveRootNameLocation(NameSyntax nameSyntax)
    {
        // For `using Moq.Language.Flow;` we want to highlight the root `Moq`.
        return nameSyntax switch
        {
            QualifiedNameSyntax qualified => GetUsingDirectiveRootNameLocation(qualified.Left),
            AliasQualifiedNameSyntax aliasQualified => aliasQualified.Alias.Identifier.GetLocation(),
            IdentifierNameSyntax identifier => identifier.Identifier.GetLocation(),
            GenericNameSyntax generic => generic.Identifier.GetLocation(),
            _ => nameSyntax.GetLocation(),
        };
    }

    private readonly struct DisallowedMockFramework
    {
        public DisallowedMockFramework(
            string name,
            string rootNamespace,
            ImmutableArray<string> knownAssemblyNames,
            ImmutableArray<string> knownTypeMetadataNames)
        {
            Name = name;
            RootNamespace = rootNamespace;
            KnownAssemblyNames = knownAssemblyNames;
            KnownTypeMetadataNames = knownTypeMetadataNames;
        }

        public string Name
        {
            get;
        }

        public string RootNamespace
        {
            get;
        }

        public ImmutableArray<string> KnownAssemblyNames
        {
            get;
        }

        public ImmutableArray<string> KnownTypeMetadataNames
        {
            get;
        }

        public static ImmutableArray<DisallowedMockFramework> All =>
            ImmutableArray.Create(
                new DisallowedMockFramework(
                    name: "Moq",
                    rootNamespace: "Moq",
                    knownAssemblyNames: ImmutableArray.Create("Moq"),
                    knownTypeMetadataNames: ImmutableArray.Create("Moq.Mock`1", "Moq.It", "Moq.ItExpr", "Moq.Mock")),
                new DisallowedMockFramework(
                    name: "FakeItEasy",
                    rootNamespace: "FakeItEasy",
                    knownAssemblyNames: ImmutableArray.Create("FakeItEasy"),
                    knownTypeMetadataNames: ImmutableArray.Create("FakeItEasy.A", "FakeItEasy.Fake", "FakeItEasy.CallTo")));

        public bool IsPresent(Compilation compilation)
        {
            foreach (var reference in compilation.ReferencedAssemblyNames)
            {
                foreach (var knownAssembly in KnownAssemblyNames)
                {
                    if (string.Equals(reference.Name, knownAssembly, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            foreach (var metadataName in KnownTypeMetadataNames)
            {
                if (compilation.GetTypeByMetadataName(metadataName) is not null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
