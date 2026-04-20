namespace Swa.Analyzers.SampleApp.Arch002;

internal static class Await_Valid
{
    // Exemplos que NÃO devem gerar diagnóstico ARCH002.

    public static async Task ProcessAsync()
    {
        await Task.Delay(1);
        Console.WriteLine("done");
    }
}
