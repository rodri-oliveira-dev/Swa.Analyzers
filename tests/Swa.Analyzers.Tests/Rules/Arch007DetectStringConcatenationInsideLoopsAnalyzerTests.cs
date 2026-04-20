using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch007DetectStringConcatenationInsideLoopsAnalyzerTests
{
    [Fact]
    public async Task Reports_string_concatenation_in_for_loop_using_plus_equals()
    {
        const string source = """
using System;

public sealed class Sample
{
    public string Build()
    {
        var result = "";

        for (var i = 0; i < 10; i++)
        {
            {|#0:result|} += i.ToString();
        }

        return result;
    }
}
""";

        var expected = Verifier<Arch007DetectStringConcatenationInsideLoopsAnalyzer>.Diagnostic("ARCH007")
            .WithLocation(0)
            .WithArguments("result")
            .WithMessage("Avoid string concatenation for 'result' inside loops. Consider using StringBuilder.");

        await Verifier<Arch007DetectStringConcatenationInsideLoopsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_string_concatenation_in_foreach_loop_using_self_referencing_assignment()
    {
        const string source = """
using System;
using System.Collections.Generic;

public sealed class Sample
{
    private string _result = "";

    public string Build(IEnumerable<string> items)
    {
        foreach (var item in items)
        {
            {|#0:_result|} = _result + item;
        }

        return _result;
    }
}
""";

        var expected = Verifier<Arch007DetectStringConcatenationInsideLoopsAnalyzer>.Diagnostic("ARCH007")
            .WithLocation(0)
            .WithArguments("_result");

        await Verifier<Arch007DetectStringConcatenationInsideLoopsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_string_concatenation_in_while_loop_using_interpolated_string()
    {
        const string source = """
using System;

public sealed class Sample
{
    public string Build()
    {
        var i = 0;
        var result = "";

        while (i < 10)
        {
            {|#0:result|} = $"{result}{i}";
            i++;
        }

        return result;
    }
}
""";

        var expected = Verifier<Arch007DetectStringConcatenationInsideLoopsAnalyzer>.Diagnostic("ARCH007")
            .WithLocation(0)
            .WithArguments("result");

        await Verifier<Arch007DetectStringConcatenationInsideLoopsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_string_concatenation_in_do_while_loop_using_string_concat()
    {
        const string source = """
using System;

public sealed class Sample
{
    public string Build()
    {
        var i = 0;
        var result = "";

        do
        {
            {|#0:result|} = string.Concat(result, i.ToString());
            i++;
        }
        while (i < 10);

        return result;
    }
}
""";

        var expected = Verifier<Arch007DetectStringConcatenationInsideLoopsAnalyzer>.Diagnostic("ARCH007")
            .WithLocation(0)
            .WithArguments("result");

        await Verifier<Arch007DetectStringConcatenationInsideLoopsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_when_concatenating_a_local_declared_inside_the_loop_body()
    {
        const string source = """
using System;

public sealed class Sample
{
    public void Execute()
    {
        for (var i = 0; i < 10; i++)
        {
            var perItem = "";
            perItem += i.ToString();
            Console.WriteLine(perItem);
        }
    }
}
""";

        await Verifier<Arch007DetectStringConcatenationInsideLoopsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_assignment_does_not_reference_the_target()
    {
        const string source = """
using System;

public sealed class Sample
{
    public void Execute()
    {
        var result = "";
        for (var i = 0; i < 10; i++)
        {
            result = i.ToString() + "x";
        }

        Console.WriteLine(result);
    }
}
""";

        await Verifier<Arch007DetectStringConcatenationInsideLoopsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_loop_condition_is_constant_false()
    {
        const string source = """
using System;

public sealed class Sample
{
    public void Execute()
    {
        var result = "";

        while (false)
        {
            result += "x";
        }

        Console.WriteLine(result);
    }
}
""";

        await Verifier<Arch007DetectStringConcatenationInsideLoopsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_do_while_condition_is_constant_false()
    {
        const string source = """
using System;

public sealed class Sample
{
    public void Execute()
    {
        var result = "";

        do
        {
            result += "x";
        }
        while (false);

        Console.WriteLine(result);
    }
}
""";

        await Verifier<Arch007DetectStringConcatenationInsideLoopsAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
