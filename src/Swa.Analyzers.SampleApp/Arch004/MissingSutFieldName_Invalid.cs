namespace Swa.Analyzers.SampleApp.Arch004;

internal sealed class CalculatorTests
{
    // Exemplos intencionais que DEVEM gerar diagnóstico ARCH004.
    // O campo principal do SUT deveria se chamar `_sut`.

    private readonly Calculator _calculator = new();

    [Xunit.Fact]
    public void Add_ShouldSum()
    {
        _ = _calculator.Add(1, 2);
    }
}
