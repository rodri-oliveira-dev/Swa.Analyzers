namespace Swa.Analyzers.SampleApp.Arch001;

internal static class AsyncTask_Valid
{
    // Exemplos que NÃO devem gerar diagnóstico ARCH001.

    public static async Task AwaitableAsync()
    {
        await Task.Delay(1);
    }
}
