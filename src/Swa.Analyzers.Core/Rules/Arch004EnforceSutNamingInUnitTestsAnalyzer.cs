using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch004EnforceSutNamingInUnitTestsAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "TestQuality";
    private const string ExpectedSutFieldName = "_sut";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.EnforceSutNamingInUnitTests,
        title: "Enforce _sut naming in unit tests",
        messageFormat: "Rename the system under test field '{0}' to '_sut'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "To improve readability and consistency across unit tests, name the primary system-under-test field '_sut'.",
        helpLinkUri: "docs/rules/ARCH004.md");

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

            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeNamedType(symbolContext, testMethodAttributes, isTestTypeCache),
                SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (type.TypeKind != TypeKind.Class)
        {
            return;
        }

        if (!IsTestType(type, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        if (!TryInferSutTypeNameFromTestTypeName(type.Name, out var inferredSutTypeName))
        {
            // Intentionally conservative: if we cannot infer the SUT type from the test type name,
            // the analyzer stays silent to avoid false positives.
            return;
        }

        ImmutableArray<IFieldSymbol> sutCandidates = GetSutFieldCandidates(type, inferredSutTypeName);
        if (sutCandidates.Length != 1)
        {
            // If there is no clear single candidate, stay silent to avoid noise
            // for helper fields, fixtures, and multiple-subject test types.
            return;
        }

        var sutField = sutCandidates[0];

        if (string.Equals(sutField.Name, ExpectedSutFieldName, StringComparison.Ordinal))
        {
            return;
        }

        var location = GetFieldIdentifierLocation(sutField);
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, sutField.Name));
    }

    private static ImmutableArray<IFieldSymbol> GetSutFieldCandidates(INamedTypeSymbol testType, string inferredSutTypeName)
    {
        var builder = ImmutableArray.CreateBuilder<IFieldSymbol>();

        foreach (var member in testType.GetMembers())
        {
            if (member is not IFieldSymbol field)
            {
                continue;
            }

            if (field.IsConst || field.IsStatic)
            {
                continue;
            }

            // We intentionally use a naming heuristic based on the test type name.
            // For example: `OrderServiceTests` -> SUT type name `OrderService`.
            if (!string.Equals(field.Type.Name, inferredSutTypeName, StringComparison.Ordinal))
            {
                continue;
            }

            // Keep the analyzer predictable by reporting only for fields declared in source.
            if (field.DeclaringSyntaxReferences.IsDefaultOrEmpty)
            {
                continue;
            }

            builder.Add(field);
        }

        return builder.ToImmutable();
    }

    private static bool TryInferSutTypeNameFromTestTypeName(string testTypeName, out string inferredSutTypeName)
    {
        // This list is intentionally small; broader patterns can be added later if needed.
        ReadOnlySpan<string> suffixes = ["Tests", "Test", "Specs", "Spec"];

        foreach (var suffix in suffixes)
        {
            if (!testTypeName.EndsWith(suffix, StringComparison.Ordinal))
            {
                continue;
            }

            var baseName = testTypeName.Substring(0, testTypeName.Length - suffix.Length);
            if (baseName.Length == 0)
            {
                inferredSutTypeName = string.Empty;
                return false;
            }

            inferredSutTypeName = baseName;
            return true;
        }

        inferredSutTypeName = string.Empty;
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
        // Same heuristic as ARCH003: the analyzer runs only when at least one known test-framework
        // attribute type is available in the compilation.
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

    private static Location GetFieldIdentifierLocation(IFieldSymbol field)
    {
        foreach (var syntaxReference in field.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax();
            if (syntax is VariableDeclaratorSyntax declarator)
            {
                return declarator.Identifier.GetLocation();
            }
        }

        return field.Locations.IsDefaultOrEmpty ? Location.None : field.Locations[0];
    }
}
