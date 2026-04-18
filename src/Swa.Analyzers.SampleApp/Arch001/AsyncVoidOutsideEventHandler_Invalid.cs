namespace Swa.Analyzers.SampleApp.Arch001;

internal static class AsyncVoidOutsideEventHandler_Invalid
{
    // Exemplos intencionais que DEVEM gerar diagnóstico ARCH001.

    public static async void FireAndForgetAsync()
    {
        await Task.Delay(1);
    }
}
