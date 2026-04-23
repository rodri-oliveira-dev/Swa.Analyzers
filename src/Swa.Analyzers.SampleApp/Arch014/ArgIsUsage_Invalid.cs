namespace Swa.Analyzers.SampleApp.Arch014;

internal sealed class ArgIsUsage_Invalid
{
    // Exemplos intencionais que DEVEM gerar diagnóstico ARCH014 quando NSubstitute for uma dependência real.

    [Xunit.Fact]
    public void ShouldNotUseArgIsWithValue()
    {
        var substitute = NSubstitute.Substitute.For<IService>();

        // ARCH014: Usar Arg.Is com valor; prefira Is.Equivalent
        substitute.DoWork(NSubstitute.Arg.Is<string>("expected"));
    }

    [Xunit.Fact]
    public void ShouldNotUseArgIsWithPredicate()
    {
        var substitute = NSubstitute.Substitute.For<IService>();

        // ARCH014: Usar Arg.Is com predicado; prefira Is.Equivalent
        substitute.DoWork(NSubstitute.Arg.Is<string>(s => s == "expected"));
    }

    [Xunit.Fact]
    public void ShouldNotUseArgIsInHelperMethod()
    {
        SetupSubstitute();
    }

    private void SetupSubstitute()
    {
        var substitute = NSubstitute.Substitute.For<IService>();

        // ARCH014: Usar Arg.Is em método auxiliar dentro de tipo de teste
        substitute.DoWork(NSubstitute.Arg.Is<string>("expected"));
    }

    [Xunit.Fact]
    public void ShouldNotUseArgIsInSetUp()
    {
        // ARCH014: Usar Arg.Is em método de setup
        var substitute = NSubstitute.Substitute.For<IService>();
        substitute.DoWork(NSubstitute.Arg.Is<string>("setup"));
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
