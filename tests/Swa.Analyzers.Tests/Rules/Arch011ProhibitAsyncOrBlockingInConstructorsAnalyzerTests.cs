using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzerTests
{
    #region Invalid scenarios

    [Fact]
    public async Task Reports_Task_Result_In_Constructor()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public Sample(Task<int> task)
    {
        var value = task.Result;
    }
}
""";

        var expected = Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.Diagnostic("ARCH011")
            .WithSpan(7, 26, 7, 32)
            .WithArguments("synchronous blocking with .Result");

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_Task_Wait_In_Constructor()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public Sample(Task task)
    {
        task.Wait();
    }
}
""";

        var expected = Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.Diagnostic("ARCH011")
            .WithSpan(7, 14, 7, 18)
            .WithArguments("synchronous blocking with .Wait()");

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_Task_GetAwaiter_GetResult_In_Constructor()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public Sample(Task<int> task)
    {
        var value = task.GetAwaiter().GetResult();
    }
}
""";

        var expected = Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.Diagnostic("ARCH011")
            .WithSpan(7, 39, 7, 48)
            .WithArguments("synchronous blocking with .GetAwaiter().GetResult()");

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_ValueTask_Result_In_Constructor()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public Sample(ValueTask<int> task)
    {
        var value = task.Result;
    }
}
""";

        var expected = Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.Diagnostic("ARCH011")
            .WithSpan(7, 26, 7, 32)
            .WithArguments("synchronous blocking with .Result");

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_Unawaited_Async_Call_In_Constructor()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public Sample()
    {
        AsyncMethod();
    }

    private static Task AsyncMethod() => Task.CompletedTask;
}
""";

        var expected = Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.Diagnostic("ARCH011")
            .WithSpan(7, 9, 7, 22)
            .WithArguments("unawaited asynchronous calls");

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_Unawaited_ValueTask_Call_In_Constructor()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public Sample()
    {
        AsyncMethod();
    }

    private static ValueTask AsyncMethod() => default;
}
""";

        var expected = Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.Diagnostic("ARCH011")
            .WithSpan(7, 9, 7, 22)
            .WithArguments("unawaited asynchronous calls");

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_Static_Constructor_Task_Result()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    static Sample()
    {
        var task = Task.FromResult(1);
        var value = task.Result;
    }
}
""";

        var expected = Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.Diagnostic("ARCH011")
            .WithSpan(8, 26, 8, 32)
            .WithArguments("synchronous blocking with .Result");

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_Task_Wait_With_Timeout_In_Constructor()
    {
        const string source = """
using System;
using System.Threading.Tasks;

public sealed class Sample
{
    public Sample(Task task)
    {
        task.Wait(TimeSpan.FromSeconds(1));
    }
}
""";

        var expected = Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.Diagnostic("ARCH011")
            .WithSpan(8, 14, 8, 18)
            .WithArguments("synchronous blocking with .Wait()");

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_Task_Result_Via_Conditional_Access_In_Constructor()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public Sample(Task<int>? task)
    {
        var value = task?.Result;
    }
}
""";

        var expected = Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.Diagnostic("ARCH011")
            .WithSpan(7, 26, 7, 33)
            .WithArguments("synchronous blocking with .Result");

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    #endregion

    #region Valid scenarios

    [Fact]
    public async Task Does_not_report_in_regular_method()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public int Execute(Task<int> task)
    {
        return task.Result;
    }
}
""";

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_task_is_assigned_to_field()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    private readonly Task _task;

    public Sample(Task task)
    {
        _task = task;
    }
}
""";

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_task_is_assigned_to_variable()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public Sample()
    {
        var task = AsyncMethod();
    }

    private static Task AsyncMethod() => Task.CompletedTask;
}
""";

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_value_task_is_assigned_to_variable()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public Sample()
    {
        var task = AsyncMethod();
    }

    private static ValueTask AsyncMethod() => default;
}
""";

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_custom_type_with_Result_in_constructor()
    {
        const string source = """
public sealed class Response
{
    public int Result { get; set; }
}

public sealed class Sample
{
    public Sample(Response response)
    {
        var value = response.Result;
    }
}
""";

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_custom_type_with_Wait_in_constructor()
    {
        const string source = """
public sealed class CustomSynchronization
{
    public void Wait() { }
}

public sealed class Sample
{
    public Sample(CustomSynchronization sync)
    {
        sync.Wait();
    }
}
""";

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_custom_awaitable_GetAwaiter_GetResult_in_constructor()
    {
        const string source = """
public struct CustomAwaitable
{
    public CustomAwaiter GetAwaiter() => new CustomAwaiter();
}

public struct CustomAwaiter
{
    public bool IsCompleted => true;
    public int GetResult() => 42;
}

public sealed class Sample
{
    public Sample()
    {
        var value = new CustomAwaitable().GetAwaiter().GetResult();
    }
}
""";

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_trivial_constructor()
    {
        const string source = """
public sealed class Sample
{
    public Sample()
    {
        var x = 1 + 1;
    }
}
""";

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_task_passed_as_argument()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public Sample()
    {
        Helper(AsyncMethod());
    }

    private static void Helper(Task task) { }
    private static Task AsyncMethod() => Task.CompletedTask;
}
""";

        await Verifier<Arch011ProhibitAsyncOrBlockingInConstructorsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    #endregion
}
