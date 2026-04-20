namespace Swa.Analyzers.SampleApp.Arch002;

internal static class ContinueWith_Invalid
{
    // Exemplos intencionais que DEVEM gerar diagnóstico ARCH002.

    public static Task ProcessAsync()
    {
        return Task.Delay(1)
            .ContinueWith(_ => Console.WriteLine("done"));
    }
}
