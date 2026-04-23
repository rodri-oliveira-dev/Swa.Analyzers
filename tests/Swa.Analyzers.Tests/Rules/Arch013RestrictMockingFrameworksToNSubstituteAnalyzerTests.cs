using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch013RestrictMockingFrameworksToNSubstituteAnalyzerTests
{
    #region Invalid scenarios

    [Fact]
    public async Task Reports_using_Moq_namespace()
    {
        const string source = """
using System;

using {|#0:Moq|};

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace Moq
{
    public sealed class Mock<T> { }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test() { }
}
""";

        var expected = Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.Diagnostic("ARCH013")
            .WithLocation(0)
            .WithArguments("Moq");

        await Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_new_Moq_Mock_object_creation()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace Moq
{
    public sealed class Mock<T> { }
}

public interface IFoo { }

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        var mock = new Moq.{|#0:Mock|}<IFoo>();
    }
}
""";

        var expected = Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.Diagnostic("ARCH013")
            .WithLocation(0)
            .WithArguments("Moq");

        await Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_Moq_It_IsAny_invocation()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace Moq
{
    public static class It
    {
        public static T IsAny<T>() => default!;
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        _ = Moq.It.{|#0:IsAny|}<int>();
    }
}
""";

        var expected = Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.Diagnostic("ARCH013")
            .WithLocation(0)
            .WithArguments("Moq");

        await Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_FakeItEasy_A_Fake_invocation()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace FakeItEasy
{
    public static class A
    {
        public static T Fake<T>() => default!;
    }
}

public interface IFoo { }

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        _ = FakeItEasy.A.{|#0:Fake|}<IFoo>();
    }
}
""";

        var expected = Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.Diagnostic("ARCH013")
            .WithLocation(0)
            .WithArguments("FakeItEasy");

        await Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_disallowed_framework_type_in_field_declaration()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace Moq
{
    public sealed class Mock<T> { }
}

public interface IFoo { }

public sealed class SampleTests
{
    private {|#0:Moq.Mock<IFoo>|} _mock;

    [Xunit.Fact]
    public void Test() { }
}
""";

        var expected = Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.Diagnostic("ARCH013")
            .WithLocation(0)
            .WithArguments("Moq");

        await Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    #endregion

    #region Edge cases

    [Fact]
    public async Task Reports_target_typed_new_for_disallowed_framework_type()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace Moq
{
    public sealed class Mock<T> { }
}

public interface IFoo { }

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        {|#0:Moq.Mock<IFoo>|} mock = new();
    }
}
""";

        var expected = Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.Diagnostic("ARCH013")
            .WithLocation(0)
            .WithArguments("Moq");

        await Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_using_static_for_disallowed_framework_type()
    {
        const string source = """
using System;

using static {|#0:Moq|}.It;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace Moq
{
    public static class It
    {
        public static T IsAny<T>() => default!;
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test() { }
}
""";

        var expected = Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.Diagnostic("ARCH013")
            .WithLocation(0)
            .WithArguments("Moq");

        await Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    #endregion

    #region Valid scenarios

    [Fact]
    public async Task Does_not_report_NSubstitute_usage()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Substitute
    {
        public static T For<T>() => default!;
    }
}

public interface IFoo { }

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        _ = NSubstitute.Substitute.For<IFoo>();
    }
}
""";

        await Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_outside_test_project()
    {
        const string source = """
namespace Moq
{
    public sealed class Mock<T> { }
}

public interface IFoo { }

public sealed class Sample
{
    public void Execute()
    {
        var mock = new Moq.Mock<IFoo>();
    }
}
""";

        await Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_similar_namespaces_or_types()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

// Make the real framework "present" for the analyzer.
namespace Moq
{
    public sealed class Mock<T> { }
}

namespace MyCompany.Moq
{
    public sealed class Mock { }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        _ = new MyCompany.Moq.Mock();
    }
}
""";

        await Verifier<Arch013RestrictMockingFrameworksToNSubstituteAnalyzer>.VerifyAnalyzerAsync(source);
    }

    #endregion
}
