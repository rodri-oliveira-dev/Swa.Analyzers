namespace Swa.Analyzers.SampleApp.Arch013;

internal sealed class NSubstitute_Valid
{
    // Exemplo intencional que NÃO deve gerar diagnóstico ARCH013.

    [Xunit.Fact]
    public void ShouldUseNSubstitute()
    {
        var substitute = NSubstitute.Substitute.For<IService>();
        _ = substitute;
    }

    internal interface IService
    {
        void DoWork();
    }
}
