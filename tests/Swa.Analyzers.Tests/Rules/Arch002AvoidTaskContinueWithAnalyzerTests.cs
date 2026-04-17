using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch002AvoidTaskContinueWithAnalyzerTests
{
    [Fact]
    public async Task Reports_Task_ContinueWith()
    {
        const string source = """
using System;
using System.Threading.Tasks;

public sealed class Sample
{
    public Task ExecuteAsync()
    {
        return Task.Delay(1).ContinueWith(_ => { });
    }
}
""";

        var expected = Verifier<Arch002AvoidTaskContinueWithAnalyzer>.Diagnostic("ARCH002")
            .WithSpan(8, 30, 8, 42);

        await Verifier<Arch002AvoidTaskContinueWithAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_TaskOfT_ContinueWith()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public Task ExecuteAsync()
    {
        return Task.FromResult(1).ContinueWith(t => t.Result);
    }
}
""";

        var expected = Verifier<Arch002AvoidTaskContinueWithAnalyzer>.Diagnostic("ARCH002")
            .WithSpan(7, 35, 7, 47);

        await Verifier<Arch002AvoidTaskContinueWithAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_ContinueWith_via_conditional_access()
    {
        const string source = """
using System;
using System.Threading.Tasks;

public sealed class Sample
{
    public void Execute(Task? task)
    {
        task?.ContinueWith(_ => { });
    }
}
""";

        var expected = Verifier<Arch002AvoidTaskContinueWithAnalyzer>.Diagnostic("ARCH002")
            .WithSpan(8, 15, 8, 27);

        await Verifier<Arch002AvoidTaskContinueWithAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_when_using_await()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public async Task ExecuteAsync()
    {
        await Task.Delay(1);
    }
}
""";

        await Verifier<Arch002AvoidTaskContinueWithAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_other_types_named_ContinueWith()
    {
        const string source = """
public sealed class CustomTask
{
    public CustomTask ContinueWith() => this;
}

public sealed class Sample
{
    public void Execute()
    {
        var task = new CustomTask();
        task.ContinueWith();
    }
}
""";

        await Verifier<Arch002AvoidTaskContinueWithAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
