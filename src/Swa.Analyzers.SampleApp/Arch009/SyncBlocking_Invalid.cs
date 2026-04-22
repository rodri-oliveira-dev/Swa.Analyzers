namespace Swa.Analyzers.SampleApp.Arch009;

internal static class SyncBlocking_Invalid
{
    // Exemplos intencionais que DEVEM gerar diagnóstico ARCH009.

    public static int FetchResult(Task<int> task)
    {
        return task.Result;
    }

    public static void WaitTask(Task task)
    {
        task.Wait();
    }

    public static int FetchViaAwaiter(Task<int> task)
    {
        return task.GetAwaiter().GetResult();
    }
}
