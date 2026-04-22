using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzerTests
{
    #region Invalid scenarios

    [Fact]
    public async Task Reports_Task_Result()
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

        var expected = Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.Diagnostic("ARCH009")
            .WithSpan(7, 21, 7, 27)
            .WithArguments(".Result");

        await Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_Task_Wait()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public void Execute(Task task)
    {
        task.Wait();
    }
}
""";

        var expected = Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.Diagnostic("ARCH009")
            .WithSpan(7, 14, 7, 18)
            .WithArguments(".Wait()");

        await Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_Task_GetAwaiter_GetResult()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public void Execute(Task task)
    {
        task.GetAwaiter().GetResult();
    }
}
""";

        var expected = Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.Diagnostic("ARCH009")
            .WithSpan(7, 27, 7, 36)
            .WithArguments(".GetAwaiter().GetResult()");

        await Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_TaskOfT_GetAwaiter_GetResult()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public int Execute(Task<int> task)
    {
        return task.GetAwaiter().GetResult();
    }
}
""";

        var expected = Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.Diagnostic("ARCH009")
            .WithSpan(7, 34, 7, 43)
            .WithArguments(".GetAwaiter().GetResult()");

        await Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_ValueTask_Result()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public int Execute(ValueTask<int> task)
    {
        return task.Result;
    }
}
""";

        var expected = Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.Diagnostic("ARCH009")
            .WithSpan(7, 21, 7, 27)
            .WithArguments(".Result");

        await Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_ValueTask_GetAwaiter_GetResult()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public void Execute(ValueTask task)
    {
        task.GetAwaiter().GetResult();
    }
}
""";

        var expected = Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.Diagnostic("ARCH009")
            .WithSpan(7, 27, 7, 36)
            .WithArguments(".GetAwaiter().GetResult()");

        await Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_Task_Wait_with_timeout()
    {
        const string source = """
using System;
using System.Threading.Tasks;

public sealed class Sample
{
    public void Execute(Task task)
    {
        task.Wait(TimeSpan.FromSeconds(1));
    }
}
""";

        var expected = Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.Diagnostic("ARCH009")
            .WithSpan(8, 14, 8, 18)
            .WithArguments(".Wait()");

        await Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_via_conditional_access_Result()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public int? Execute(Task<int>? task)
    {
        return task?.Result;
    }
}
""";

        var expected = Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.Diagnostic("ARCH009")
            .WithSpan(7, 21, 7, 28)
            .WithArguments(".Result");

        await Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_via_conditional_access_Wait()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public void Execute(Task? task)
    {
        task?.Wait();
    }
}
""";

        var expected = Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.Diagnostic("ARCH009")
            .WithSpan(7, 15, 7, 19)
            .WithArguments(".Wait()");

        await Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_via_conditional_access_GetAwaiter_GetResult()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public void Execute(Task? task)
    {
        task?.GetAwaiter().GetResult();
    }
}
""";

        var expected = Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.Diagnostic("ARCH009")
            .WithSpan(7, 28, 7, 37)
            .WithArguments(".GetAwaiter().GetResult()");

        await Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    #endregion

    #region Valid scenarios

    [Fact]
    public async Task Does_not_report_await_usage()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public async Task<int> ExecuteAsync(Task<int> task)
    {
        return await task;
    }
}
""";

        await Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_await_ValueTask()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public async Task<int> ExecuteAsync(ValueTask<int> task)
    {
        return await task;
    }
}
""";

        await Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_custom_type_with_Result()
    {
        const string source = """
public sealed class Response
{
    public int Result { get; set; }
}

public sealed class Sample
{
    public int Execute(Response response)
    {
        return response.Result;
    }
}
""";

        await Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_custom_type_with_Wait()
    {
        const string source = """
public sealed class CustomSynchronization
{
    public void Wait() { }
}

public sealed class Sample
{
    public void Execute(CustomSynchronization sync)
    {
        sync.Wait();
    }
}
""";

        await Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_custom_awaitable_with_GetAwaiter_GetResult()
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
    public int Execute()
    {
        return new CustomAwaitable().GetAwaiter().GetResult();
    }
}
""";

        await Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_Wait_on_EventWaitHandle()
    {
        const string source = """
using System.Threading;

public sealed class Sample
{
    public void Execute(EventWaitHandle handle)
    {
        handle.WaitOne();
    }
}
""";

        await Verifier<Arch009ProhibitSyncOverAsyncBlockingCallsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    #endregion
}
