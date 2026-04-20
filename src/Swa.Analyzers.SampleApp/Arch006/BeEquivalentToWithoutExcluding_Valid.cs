using FluentAssertions;

namespace Swa.Analyzers.SampleApp.Arch006;

internal sealed class BeEquivalentToWithoutExcluding_Valid
{
    // Exemplos que NÃO devem gerar diagnóstico ARCH006.

    [Xunit.Fact]
    public void PreferStrictEquivalency()
    {
        var actual = new CustomerDto("Ana", "123");
        var expected = new CustomerDto("Ana", "123");

        actual.Should().BeEquivalentTo(expected);
    }

    /*
     * Observação didática:
     * Em alguns casos, uma exclusão pode ser justificável (ex.: campos gerados como timestamps/IDs
     * quando o objetivo do teste é outro). Quando for inevitável, documente o motivo claramente.
     */

    private sealed record CustomerDto(string Name, string Document);
}
