using System.Threading.Tasks;
using Swa.Analyzers.Core;
using Xunit;

namespace Swa.Analyzers.Tests;

public sealed class Arch001OnlyNSubstituteAnalyzerTests
{
    [Fact]
    public async Task ReportsDiagnostic_WhenBlockedNamespaceIsImportedInTestProject()
    {
        var source = """
            using {|#0:Moq|};

            namespace Moq
            {
                public sealed class Mock<T> { }
            }

            public class CustomerTests { }
            """;

        var test = Verifier<Arch001OnlyNSubstituteAnalyzer>.Test();
        test.TestCode = source;
        test.ExpectedDiagnostics.Add(
            Verifier<Arch001OnlyNSubstituteAnalyzer>.Diagnostic(Arch001OnlyNSubstituteAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments("Moq"));

        await test.RunAsync();
    }

    [Fact]
    public async Task DoesNotReportDiagnostic_ForStringMentions()
    {
        var source = """
            public class CustomerTests
            {
                public string Name => "Moq FakeItEasy Rhino.Mocks";
            }
            """;

        var test = Verifier<Arch001OnlyNSubstituteAnalyzer>.Test();
        test.TestCode = source;

        await test.RunAsync();
    }

    [Fact]
    public async Task DoesNotReportDiagnostic_WhenOnlyTestProjectsIsEnabledAndAssemblyIsNotTest()
    {
        var source = """
            using Moq;

            namespace Moq
            {
                public sealed class Mock<T> { }
            }

            public class CustomerService { }
            """;

        var test = Verifier<Arch001OnlyNSubstituteAnalyzer>.Test();
        test.TestState.AssemblyName = "Production.App";
        test.TestCode = source;

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_WhenOnlyTestProjectsIsDisabledViaEditorConfig()
    {
        var source = """
            using {|#0:Moq|};

            namespace Moq
            {
                public sealed class Mock<T> { }
            }

            public class CustomerService { }
            """;

        var test = Verifier<Arch001OnlyNSubstituteAnalyzer>.Test();
        test.TestState.AssemblyName = "Production.App";
        test.TestCode = source;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", """
            root=true

            [*.cs]
            dotnet_diagnostic.ARCH001.only_test_projects = false
            """));

        test.ExpectedDiagnostics.Add(
            Verifier<Arch001OnlyNSubstituteAnalyzer>.Diagnostic(Arch001OnlyNSubstituteAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments("Moq"));

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsDiagnostic_WhenBlockedNamespaceIsOverriddenViaEditorConfig()
    {
        var source = """
            using {|#0:MyCustomMocks|};

            namespace MyCustomMocks
            {
                public static class Fake {
                    public static T Create<T>() => default!;
                }
            }

            public class CustomerTests { }
            """;

        var test = Verifier<Arch001OnlyNSubstituteAnalyzer>.Test();
        test.TestCode = source;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", """
            root=true

            [*.cs]
            dotnet_diagnostic.ARCH001.blocked_namespaces = MyCustomMocks
            """));

        test.ExpectedDiagnostics.Add(
            Verifier<Arch001OnlyNSubstituteAnalyzer>.Diagnostic(Arch001OnlyNSubstituteAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments("MyCustomMocks"));

        await test.RunAsync();
    }
}
