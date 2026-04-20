using FluentAssertions;

namespace Swa.Analyzers.SampleApp.Arch003;

internal sealed class NotBeNull_Invalid
{
    // Exemplos intencionais que DEVEM gerar diagnóstico ARCH003.

    [Xunit.Fact]
    public void ShouldAvoidNotBeNull()
    {
        object? value = new object();

        // ARCH003: NotBeNull() é uma assertiva fraca.
        value.Should().NotBeNull();
    }
}
