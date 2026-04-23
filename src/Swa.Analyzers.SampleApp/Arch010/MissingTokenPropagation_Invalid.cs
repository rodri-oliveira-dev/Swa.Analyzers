namespace Swa.Analyzers.SampleApp.Arch010;

internal static class MissingTokenPropagation_Invalid
{
    // Exemplos intencionais que DEVEM gerar diagnóstico ARCH010.

    public static async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        // ARCH010: Pass the available CancellationToken to 'DoWorkAsync'.
        await service.DoWorkAsync(1);
    }

    public static async Task ExecuteWithOptionalAsync(Service service, CancellationToken cancellationToken)
    {
        // ARCH010: Pass the available CancellationToken to 'DoWorkAsync'.
        await service.DoWorkWithOptionalAsync(1);
    }

    public static async Task ExecuteWithLocalTokenAsync(Service service)
    {
        var cancellationToken = new CancellationToken();

        // ARCH010: Pass the available CancellationToken to 'DoWorkAsync'.
        await service.DoWorkAsync(1);
    }
}

internal sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkAsync(int id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkWithOptionalAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
