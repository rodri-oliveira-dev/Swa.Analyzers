using NSubstitute;

namespace Swa.Analyzers.SampleApp.Arch005;

internal sealed class ArgAnyAllowedConvention_Valid
{
    // Exemplos que NÃO devem gerar diagnóstico ARCH005.

    private readonly IMessageSender _sender = NSubstitute.Substitute.For<IMessageSender>();

    [Xunit.Fact]
    public void PreferConcreteValues_WhenPossible()
    {
        _sender.Send("hello");
    }

    [Xunit.Fact]
    public void ArgAny_IsAllowed_InDidNotReceiveConvention()
    {
        // Convenção permitida: Arg.Any() como argumento direto em chamada precedida por DidNotReceive().
        _sender.DidNotReceive().Send(Arg.Any<string>());
    }

    internal interface IMessageSender
    {
        void Send(string message);
    }
}
