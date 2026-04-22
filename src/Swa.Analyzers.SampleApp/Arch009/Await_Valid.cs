namespace Swa.Analyzers.SampleApp.Arch009;

internal static class Await_Valid
{
    // Exemplos que NÃO devem gerar diagnóstico ARCH009.

    public static async Task<int> FetchAsync(Task<int> task)
    {
        return await task;
    }

    public static async Task ExecuteAsync(Task task)
    {
        await task;
    }
}
