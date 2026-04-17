using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch004EnforceSutNamingInUnitTestsAnalyzerTests
{
    [Fact]
    public async Task Reports_when_single_sut_candidate_field_is_not_named__sut()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

public sealed class Calculator
{
    public int Add(int a, int b) => a + b;
}

public sealed class CalculatorTests
{
    private readonly Calculator _calculator = new();

    [Xunit.Fact]
    public void Adds()
    {
        _calculator.Add(1, 2);
    }
}
""";

        var expected = Verifier<Arch004EnforceSutNamingInUnitTestsAnalyzer>.Diagnostic("ARCH004")
            .WithSpan(15, 33, 15, 44)
            .WithArguments("_calculator")
            .WithMessage("Rename the system under test field '_calculator' to '_sut'");

        await Verifier<Arch004EnforceSutNamingInUnitTestsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_when_single_sut_candidate_field_is_named__sut()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

public sealed class Calculator
{
    public int Add(int a, int b) => a + b;
}

public sealed class CalculatorTests
{
    private readonly Calculator _sut = new();

    [Xunit.Fact]
    public void Adds()
    {
        _sut.Add(1, 2);
    }
}
""";

        await Verifier<Arch004EnforceSutNamingInUnitTestsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_test_type_name_does_not_match_supported_suffixes()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

public sealed class Calculator
{
    public int Add(int a, int b) => a + b;
}

public sealed class CalculatorTestSuite
{
    private readonly Calculator _calculator = new();

    [Xunit.Fact]
    public void Adds()
    {
        _calculator.Add(1, 2);
    }
}
""";

        await Verifier<Arch004EnforceSutNamingInUnitTestsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_multiple_fields_match_inferred_sut_type_name()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

public sealed class Calculator
{
    public int Add(int a, int b) => a + b;
}

public sealed class CalculatorTests
{
    private readonly Calculator _calculator = new();
    private readonly Calculator _expected = new();

    [Xunit.Fact]
    public void Adds()
    {
        _calculator.Add(1, 2);
        _expected.Add(1, 2);
    }
}
""";

        await Verifier<Arch004EnforceSutNamingInUnitTestsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_type_is_not_a_test_type_even_inside_test_project()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

public sealed class Calculator
{
    public int Add(int a, int b) => a + b;
}

public sealed class CalculatorTests
{
    private readonly Calculator _calculator = new();

    public void Helper()
    {
        _calculator.Add(1, 2);
    }
}

public sealed class MarkerTestType
{
    [Xunit.Fact]
    public void Test() { }
}
""";

        await Verifier<Arch004EnforceSutNamingInUnitTestsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_outside_test_project()
    {
        const string source = """
public sealed class Calculator
{
    public int Add(int a, int b) => a + b;
}

public sealed class CalculatorTests
{
    private readonly Calculator _calculator = new();
}
""";

        await Verifier<Arch004EnforceSutNamingInUnitTestsAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
