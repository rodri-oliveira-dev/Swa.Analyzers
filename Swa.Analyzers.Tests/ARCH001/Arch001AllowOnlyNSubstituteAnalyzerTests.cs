using System.Threading.Tasks;
using Swa.Analyzers.Core.Analyzers;
using Swa.Analyzers.Tests.Verifier;
using Xunit;

namespace Swa.Analyzers.Tests.ARCH001;

public sealed class Arch001AllowOnlyNSubstituteAnalyzerTests
{
    [Fact]
    public async Task Reports_diagnostic_for_blocked_using_in_test_project()
    {
        var source = """
            using [|Moq|];

            namespace Sample.Tests;

            public class MyTests { }
            """;

        var test = new AnalyzerVerifier<Arch001AllowOnlyNSubstituteAnalyzer>.Test
        {
            TestCode = source,
            TestState = { AssemblyName = "Sample.Tests" }
        };

        test.ExpectedDiagnostics.Add(
            AnalyzerVerifier<Arch001AllowOnlyNSubstituteAnalyzer>
                .Diagnostic(Arch001AllowOnlyNSubstituteAnalyzer.DiagnosticId)
                .WithSpan(1, 7, 1, 10)
                .WithArguments("Moq"));

        await test.RunAsync();
    }

    [Fact]
    public async Task Reports_diagnostic_for_object_creation_with_blocked_namespace()
    {
        var source = """
            namespace Moq
            {
                public class Mock<T> { }
            }

            namespace Sample.Tests;

            public class MyTests
            {
                public void Arrange()
                {
                    var mock = new [|Moq.Mock<int>|]();
                }
            }
            """;

        var test = new AnalyzerVerifier<Arch001AllowOnlyNSubstituteAnalyzer>.Test
        {
            TestCode = source,
            TestState = { AssemblyName = "Sample.Tests" }
        };

        test.ExpectedDiagnostics.Add(
            AnalyzerVerifier<Arch001AllowOnlyNSubstituteAnalyzer>
                .Diagnostic(Arch001AllowOnlyNSubstituteAnalyzer.DiagnosticId)
                .WithSpan(12, 20, 12, 34)
                .WithArguments("Moq"));

        await test.RunAsync();
    }

    [Fact]
    public async Task Does_not_report_for_nsubstitute_usage()
    {
        var source = """
            namespace NSubstitute
            {
                public static class Substitute
                {
                    public static T For<T>() where T : class => default!;
                }
            }

            namespace Sample.Tests;

            public interface IService { }

            public class MyTests
            {
                public void Arrange()
                {
                    var service = NSubstitute.Substitute.For<IService>();
                }
            }
            """;

        var test = new AnalyzerVerifier<Arch001AllowOnlyNSubstituteAnalyzer>.Test
        {
            TestCode = source,
            TestState = { AssemblyName = "Sample.Tests" }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task Does_not_report_in_non_test_project_by_default()
    {
        var source = """
            using Moq;

            namespace Moq
            {
                public class Mock<T> { }
            }

            namespace Sample.App;

            public class MyService
            {
                public void Build()
                {
                    var mock = new Mock<int>();
                }
            }
            """;

        var test = new AnalyzerVerifier<Arch001AllowOnlyNSubstituteAnalyzer>.Test
        {
            TestCode = source,
            TestState = { AssemblyName = "Sample.App" }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task Reports_in_non_test_project_when_configured()
    {
        var source = """
            using [|Moq|];

            namespace Sample.App;

            public class MyService { }
            """;

        var test = new AnalyzerVerifier<Arch001AllowOnlyNSubstituteAnalyzer>.Test
        {
            TestCode = source,
            TestState =
            {
                AssemblyName = "Sample.App",
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig", """
                        root = true

                        [*.cs]
                        dotnet_diagnostic.ARCH001.only_test_projects = false
                        """)
                }
            }
        };

        test.ExpectedDiagnostics.Add(
            AnalyzerVerifier<Arch001AllowOnlyNSubstituteAnalyzer>
                .Diagnostic(Arch001AllowOnlyNSubstituteAnalyzer.DiagnosticId)
                .WithSpan(1, 7, 1, 10)
                .WithArguments("Moq"));

        await test.RunAsync();
    }

    [Fact]
    public async Task String_mentions_are_ignored()
    {
        var source = """
            namespace Sample.Tests;

            public class MyTests
            {
                public string Value => "Moq should not trigger diagnostics";
            }
            """;

        var test = new AnalyzerVerifier<Arch001AllowOnlyNSubstituteAnalyzer>.Test
        {
            TestCode = source,
            TestState = { AssemblyName = "Sample.Tests" }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task Supports_custom_blocked_namespace_from_editorconfig()
    {
        var source = """
            using [|MyCompany.Fakes|];

            namespace Sample.Tests;

            public class MyTests { }
            """;

        var test = new AnalyzerVerifier<Arch001AllowOnlyNSubstituteAnalyzer>.Test
        {
            TestCode = source,
            TestState =
            {
                AssemblyName = "Sample.Tests",
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig", """
                        root = true

                        [*.cs]
                        dotnet_diagnostic.ARCH001.blocked_namespaces = MyCompany.Fakes
                        """)
                }
            }
        };

        test.ExpectedDiagnostics.Add(
            AnalyzerVerifier<Arch001AllowOnlyNSubstituteAnalyzer>
                .Diagnostic(Arch001AllowOnlyNSubstituteAnalyzer.DiagnosticId)
                .WithSpan(1, 7, 1, 22)
                .WithArguments("MyCompany.Fakes"));

        await test.RunAsync();
    }
}
