using Swa.Analyzers.Core.Rules;


namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch001AvoidAsyncVoidAnalyzerTests
{
    [Fact]
    public async Task Reports_async_void_method()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public async void DoWork()
    {
        await Task.Delay(1);
    }
}
""";

        var expected = Verifier<Arch001AvoidAsyncVoidAnalyzer>.Diagnostic("ARCH001")
            .WithSpan(5, 23, 5, 29)
            .WithArguments("DoWork");

        await Verifier<Arch001AvoidAsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_async_task_method()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public async Task DoWorkAsync()
    {
        await Task.Delay(1);
    }
}
""";

        await Verifier<Arch001AvoidAsyncVoidAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_standard_event_handler_method()
    {
        const string source = """
using System;
using System.Threading.Tasks;

public sealed class Sample
{
    public async void OnClick(object? sender, EventArgs e)
    {
        await Task.Delay(1);
    }
}
""";

        await Verifier<Arch001AvoidAsyncVoidAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Reports_async_void_local_function()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public async Task ExecuteAsync()
    {
        async void Local()
        {
            await Task.Delay(1);
        }

        Local();
    }
}
""";

        var expected = Verifier<Arch001AvoidAsyncVoidAnalyzer>.Diagnostic("ARCH001")
            .WithSpan(7, 20, 7, 25)
            .WithArguments("Local");

        await Verifier<Arch001AvoidAsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_async_lambda_assigned_to_action()
    {
        const string source = """
using System;
using System.Threading.Tasks;

public sealed class Sample
{
    public void Execute()
    {
        Action action = async () =>
        {
            await Task.Delay(1);
        };
    }
}
""";

        var expected = Verifier<Arch001AvoidAsyncVoidAnalyzer>.Diagnostic("ARCH001")
            .WithSpan(8, 25, 8, 30)
            .WithArguments("anonymous function");

        await Verifier<Arch001AvoidAsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_async_lambda_assigned_to_func_of_task()
    {
        const string source = """
using System;
using System.Threading.Tasks;

public sealed class Sample
{
    public void Execute()
    {
        Func<Task> action = async () =>
        {
            await Task.Delay(1);
        };
    }
}
""";

        await Verifier<Arch001AvoidAsyncVoidAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_standard_event_handler_lambda()
    {
        const string source = """
using System;
using System.Threading.Tasks;

public sealed class Publisher
{
    public event EventHandler? Clicked;
}

public sealed class Sample
{
    public void WireUp(Publisher publisher)
    {
        publisher.Clicked += async (sender, e) =>
        {
            await Task.Delay(1);
        };
    }
}
""";

        await Verifier<Arch001AvoidAsyncVoidAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
