namespace Swa.Analyzers.SampleApp.Arch001;

internal static class AsyncVoidEventHandler_Valid
{
    // Event handlers com assinatura padrão (object, EventArgs) são permitidos.

    public static async void OnSomethingHappenedAsync(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        await Task.Delay(1);
    }
}
