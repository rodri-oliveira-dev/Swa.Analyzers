namespace Swa.Analyzers.SampleApp.Arch013;

internal sealed class OtherMockFramework_Invalid
{
    // Exemplos intencionais que DEVEM gerar diagnóstico ARCH013.

    [Xunit.Fact]
    public void ShouldAvoidMoq()
    {
        // ARCH013: Moq é proibido pela política; use NSubstitute.
        var mock = new Moq.Mock<IService>();
        _ = mock;

        _ = Moq.It.IsAny<int>();
    }

    [Xunit.Fact]
    public void ShouldAvoidFakeItEasy()
    {
        // ARCH013: FakeItEasy é proibido pela política; use NSubstitute.
        var fake = FakeItEasy.A.Fake<IService>();
        _ = fake;
    }

    internal interface IService
    {
        void DoWork();
    }
}
