namespace Swa.Analyzers.SampleApp.Arch004;

internal sealed class CalculatorSpecs
{
    // Exemplos que NÃO devem gerar diagnóstico ARCH004.

    private readonly Calculator _sut = new();

    [Xunit.Fact]
    public void Add_ShouldSum()
    {
        _ = _sut.Add(1, 2);
    }
}
