namespace Swa.Analyzers.SampleApp.Arch011;

internal sealed class AsyncFactory_Valid
{
    // Exemplos que NÃO devem gerar diagnóstico ARCH011.

    internal sealed class ServiceWithAsyncFactory
    {
        private readonly int _value;

        private ServiceWithAsyncFactory(int value)
        {
            _value = value;
        }

        public static async Task<ServiceWithAsyncFactory> CreateAsync(Task<int> fetcher)
        {
            var value = await fetcher;
            return new ServiceWithAsyncFactory(value);
        }
    }

    internal sealed class ServiceWithTaskAssignment
    {
        private readonly Task _task;

        public ServiceWithTaskAssignment(Task task)
        {
            _task = task;
        }
    }

    internal sealed class ServiceWithVariableAssignment
    {
        public ServiceWithVariableAssignment()
        {
            var task = AsyncMethod();
        }

        private static Task AsyncMethod() => Task.CompletedTask;
    }
}
