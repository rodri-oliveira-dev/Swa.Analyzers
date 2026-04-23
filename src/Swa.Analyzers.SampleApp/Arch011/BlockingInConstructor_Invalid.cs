namespace Swa.Analyzers.SampleApp.Arch011;

internal sealed class BlockingInConstructor_Invalid
{
    // Exemplos intencionais que DEVEM gerar diagnóstico ARCH011.

    internal sealed class ServiceWithTaskResult
    {
        private readonly int _value;

        public ServiceWithTaskResult(Task<int> fetcher)
        {
            _value = fetcher.Result;
        }
    }

    internal sealed class ServiceWithTaskWait
    {
        public ServiceWithTaskWait(Task task)
        {
            task.Wait();
        }
    }

    internal sealed class ServiceWithGetAwaiterGetResult
    {
        private readonly int _value;

        public ServiceWithGetAwaiterGetResult(Task<int> fetcher)
        {
            _value = fetcher.GetAwaiter().GetResult();
        }
    }

    internal sealed class ServiceWithFireAndForget
    {
        public ServiceWithFireAndForget()
        {
            StartAsync();
        }

        private static Task StartAsync() => Task.CompletedTask;
    }
}
