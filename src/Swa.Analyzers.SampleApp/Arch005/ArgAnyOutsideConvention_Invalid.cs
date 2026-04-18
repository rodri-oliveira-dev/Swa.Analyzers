namespace Swa.Analyzers.SampleApp.Arch005;

internal sealed class ArgAnyOutsideConvention_Invalid
{
    // Exemplos intencionais que DEVEM gerar diagnóstico ARCH005.

    private readonly IMessageSender _sender = NSubstitute.Substitute.For<IMessageSender>();

    [Xunit.Fact]
    public void ShouldAvoidArgAny_InGeneralAssertions()
    {
        // ARCH005: Arg.Any() fora da convenção permitida.
        _sender.Send(NSubstitute.Arg.Any<string>());
    }

    internal interface IMessageSender
    {
        void Send(string message);
    }
}
