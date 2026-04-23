namespace Swa.Analyzers.SampleApp.Arch014;

internal sealed class PreferIsEquivalent_Valid
{
    // Exemplos intencionais que NÃO devem gerar diagnóstico ARCH014.

    [Xunit.Fact]
    public void ShouldUseIsEquivalent()
    {
        var substitute = NSubstitute.Substitute.For<IService>();

        // Uso correto: Is.Equivalent ao invés de Arg.Is
        substitute.DoWork(Is.Equivalent("expected"));
    }

    internal interface IService
    {
        void DoWork(string value);
    }

    // Simulação da API da equipe padrão
    internal static class Is
    {
        public static string Equivalent(string expected) => expected;
        public static T Equivalent<T>(T expected) => expected;
    }
}
