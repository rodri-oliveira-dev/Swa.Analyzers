using FluentAssertions;

namespace Swa.Analyzers.SampleApp.Arch003;

internal sealed class SpecificAssertion_Valid
{
    // Exemplos que NÃO devem gerar diagnóstico ARCH003.

    [Xunit.Fact]
    public void PreferMoreSpecificAssertions()
    {
        string? value = "abc";

        value.Should().NotBeNullOrEmpty();
        value.Should().BeOfType<string>();
    }
}
