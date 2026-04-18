using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch008ProhibitManualPathCompositionAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Reliability";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.ProhibitManualPathComposition,
        title: "Prohibit manual path composition",
        messageFormat: "Avoid manual path composition for '{0}'. Use Path.Combine or Path.Join.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Manually composing file system paths with string concatenation or interpolated strings is error-prone and can break cross-platform compatibility. Prefer System.IO.Path.Combine/Path.Join (or other path APIs) when building paths.",
        helpLinkUri: "docs/rules/ARCH008.md");

    private static readonly ImmutableHashSet<string> PathParameterNames = ImmutableHashSet.Create(
        StringComparer.OrdinalIgnoreCase,
        // Common BCL names for path parameters.
        "path",
        "fileName",
        "sourceFileName",
        "destFileName",
        "destinationFileName",
        "destinationBackupFileName",
        "sourceDirName",
        "destDirName",
        "path1",
        "path2");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var sinks = SinkSymbols.Create(compilationContext.Compilation);
            if (!sinks.HasAny)
            {
                return;
            }

            compilationContext.RegisterOperationAction(operationContext => AnalyzeInvocation(operationContext, sinks), OperationKind.Invocation);
            compilationContext.RegisterOperationAction(operationContext => AnalyzeObjectCreation(operationContext, sinks), OperationKind.ObjectCreation);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, SinkSymbols sinks)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var containingType = invocation.TargetMethod.ContainingType;

        if (!sinks.IsInvocationSinkType(containingType))
        {
            return;
        }

        foreach (var argument in invocation.Arguments)
        {
            if (!IsRelevantPathArgument(argument))
            {
                continue;
            }

            if (!IsManualPathComposition(argument.Value!))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                argument.Value!.Syntax.GetLocation(),
                argument.Parameter!.Name));
        }
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context, SinkSymbols sinks)
    {
        var creation = (IObjectCreationOperation)context.Operation;

        if (!sinks.IsObjectCreationSinkType(creation.Type))
        {
            return;
        }

        foreach (var argument in creation.Arguments)
        {
            if (!IsRelevantPathArgument(argument))
            {
                continue;
            }

            if (!IsManualPathComposition(argument.Value!))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                argument.Value!.Syntax.GetLocation(),
                argument.Parameter!.Name));
        }
    }

    private static bool IsRelevantPathArgument(IArgumentOperation argument)
    {
        if (argument.Parameter is null)
        {
            return false;
        }

        if (argument.Value is null)
        {
            return false;
        }

        if (argument.Parameter.Type.SpecialType != SpecialType.System_String)
        {
            return false;
        }

        return PathParameterNames.Contains(argument.Parameter.Name);
    }

    private static bool IsManualPathComposition(IOperation value)
    {
        var unwrapped = Unwrap(value);

        if (unwrapped is IBinaryOperation binary
            && binary.OperatorKind == BinaryOperatorKind.Add
            && binary.Type?.SpecialType == SpecialType.System_String)
        {
            return true;
        }

        if (unwrapped is IInterpolatedStringOperation interpolatedString
            && IsMeaningfulInterpolatedString(interpolatedString))
        {
            return true;
        }

        return false;
    }

    private static bool IsMeaningfulInterpolatedString(IInterpolatedStringOperation interpolatedString)
    {
        // Avoid noise for identity interpolations like $"{path}".
        if (interpolatedString.Syntax is InterpolatedStringExpressionSyntax syntax)
        {
            if (syntax.Contents.Count == 1 && syntax.Contents[0] is InterpolationSyntax)
            {
                return false;
            }
        }

        return true;
    }

    private static IOperation Unwrap(IOperation operation)
    {
        IOperation? current = operation;

        while (current is not null)
        {
            switch (current)
            {
                case IConversionOperation conversion:
                    current = conversion.Operand;
                    continue;

                case IParenthesizedOperation parenthesized:
                    current = parenthesized.Operand;
                    continue;
            }

            break;
        }

        return current ?? operation;
    }

    private readonly struct SinkSymbols
    {
        public SinkSymbols(
            INamedTypeSymbol? file,
            INamedTypeSymbol? directory,
            INamedTypeSymbol? fileInfo,
            INamedTypeSymbol? directoryInfo,
            INamedTypeSymbol? fileStream,
            INamedTypeSymbol? streamReader,
            INamedTypeSymbol? streamWriter)
        {
            File = file;
            Directory = directory;
            FileInfo = fileInfo;
            DirectoryInfo = directoryInfo;
            FileStream = fileStream;
            StreamReader = streamReader;
            StreamWriter = streamWriter;
        }

        public INamedTypeSymbol? File
        {
            get;
        }
        public INamedTypeSymbol? Directory
        {
            get;
        }
        public INamedTypeSymbol? FileInfo
        {
            get;
        }
        public INamedTypeSymbol? DirectoryInfo
        {
            get;
        }
        public INamedTypeSymbol? FileStream
        {
            get;
        }
        public INamedTypeSymbol? StreamReader
        {
            get;
        }
        public INamedTypeSymbol? StreamWriter
        {
            get;
        }

        public static SinkSymbols Create(Compilation compilation) => new(
            compilation.GetTypeByMetadataName("System.IO.File"),
            compilation.GetTypeByMetadataName("System.IO.Directory"),
            compilation.GetTypeByMetadataName("System.IO.FileInfo"),
            compilation.GetTypeByMetadataName("System.IO.DirectoryInfo"),
            compilation.GetTypeByMetadataName("System.IO.FileStream"),
            compilation.GetTypeByMetadataName("System.IO.StreamReader"),
            compilation.GetTypeByMetadataName("System.IO.StreamWriter"));

        public bool HasAny => File is not null
            || Directory is not null
            || FileInfo is not null
            || DirectoryInfo is not null
            || FileStream is not null
            || StreamReader is not null
            || StreamWriter is not null;

        public bool IsInvocationSinkType(INamedTypeSymbol? containingType)
        {
            if (containingType is null)
            {
                return false;
            }

            return (File is not null && SymbolEqualityComparer.Default.Equals(containingType, File))
                || (Directory is not null && SymbolEqualityComparer.Default.Equals(containingType, Directory))
                || (FileInfo is not null && SymbolEqualityComparer.Default.Equals(containingType, FileInfo))
                || (DirectoryInfo is not null && SymbolEqualityComparer.Default.Equals(containingType, DirectoryInfo));
        }

        public bool IsObjectCreationSinkType(ITypeSymbol? type)
        {
            if (type is null)
            {
                return false;
            }

            return (FileInfo is not null && SymbolEqualityComparer.Default.Equals(type, FileInfo))
                || (DirectoryInfo is not null && SymbolEqualityComparer.Default.Equals(type, DirectoryInfo))
                || (FileStream is not null && SymbolEqualityComparer.Default.Equals(type, FileStream))
                || (StreamReader is not null && SymbolEqualityComparer.Default.Equals(type, StreamReader))
                || (StreamWriter is not null && SymbolEqualityComparer.Default.Equals(type, StreamWriter));
        }
    }
}
