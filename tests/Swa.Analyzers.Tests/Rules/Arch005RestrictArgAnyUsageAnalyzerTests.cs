using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch005RestrictArgAnyUsageAnalyzerTests
{
    [Fact]
    public async Task Reports_ArgAny_when_used_outside_allowed_convention()
    {
        const string source = """
using System;
using NSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public static class SubstituteExtensions
    {
        public static T Received<T>(this T substitute) where T : class => substitute;
        public static T DidNotReceive<T>(this T substitute) where T : class => substitute;
        public static T DidNotReceiveWithAnyArgs<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(int value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.Received().Do(NSubstitute.Arg.Any<int>());
    }
}
""";

        var expected = Verifier<Arch005RestrictArgAnyUsageAnalyzer>.Diagnostic("ARCH005")
            .WithSpan(35, 50, 35, 58)
            .WithMessage("Avoid NSubstitute Arg.Any() outside the allowed convention. Use DidNotReceive/DidNotReceiveWithAnyArgs instead.");

        await Verifier<Arch005RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_ArgAny_when_used_in_DidNotReceive_call_chain()
    {
        const string source = """
using System;
using NSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public static class SubstituteExtensions
    {
        public static T DidNotReceive<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(int value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.DidNotReceive().Do(NSubstitute.Arg.Any<int>());
    }
}
""";

        await Verifier<Arch005RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_ArgAny_when_used_in_DidNotReceiveWithAnyArgs_call_chain()
    {
        const string source = """
using System;
using NSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public static class SubstituteExtensions
    {
        public static T DidNotReceiveWithAnyArgs<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(int value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.DidNotReceiveWithAnyArgs().Do(NSubstitute.Arg.Any<int>());
    }
}
""";

        await Verifier<Arch005RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_outside_test_project()
    {
        const string source = """
namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }
}

public sealed class Sample
{
    public void Execute()
    {
        _ = NSubstitute.Arg.Any<int>();
    }
}
""";

        await Verifier<Arch005RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_in_non_test_type_inside_test_project()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }
}

public sealed class Helper
{
    public void Execute()
    {
        _ = NSubstitute.Arg.Any<int>();
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test() { }
}
""";

        await Verifier<Arch005RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_other_ArgAny_methods_when_not_NSubstitute()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace CustomSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        _ = CustomSubstitute.Arg.Any<int>();
    }
}
""";

        await Verifier<Arch005RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Reports_ArgAny_when_used_in_DidNotReceive_chain_but_not_as_direct_argument_value()
    {
        const string source = """
using System;
using NSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public static class SubstituteExtensions
    {
        public static T DidNotReceive<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(int value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.DidNotReceive().Do(NSubstitute.Arg.Any<int>() + 1);
    }
}
""";

        var expected = Verifier<Arch005RestrictArgAnyUsageAnalyzer>.Diagnostic("ARCH005")
            .WithSpan(33, 55, 33, 63);

        await Verifier<Arch005RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_ArgAny_with_alias_in_allowed_chain()
    {
        const string source = """
using System;
using NSubstitute;
using A = NSubstitute.Arg;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public static class SubstituteExtensions
    {
        public static T DidNotReceive<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(int value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.DidNotReceive().Do(A.Any<int>());
    }
}
""";

        await Verifier<Arch005RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_ArgAny_in_allowed_chain_with_conditional_access()
    {
        const string source = """
using System;
using NSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public static class SubstituteExtensions
    {
        public static T DidNotReceive<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(int value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency? substitute = null;
        substitute?.DidNotReceive()?.Do(NSubstitute.Arg.Any<int>());
    }
}
""";

        await Verifier<Arch005RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Reports_ArgAny_when_exception_method_is_lookalike_not_from_NSubstitute()
    {
        const string source = """
using System;
using NSubstitute;
using CustomSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }
}

namespace CustomSubstitute
{
    public static class SubstituteExtensions
    {
        public static T DidNotReceive<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(int value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.DidNotReceive().Do(NSubstitute.Arg.Any<int>());
    }
}
""";

        var expected = Verifier<Arch005RestrictArgAnyUsageAnalyzer>.Diagnostic("ARCH005")
            .WithSpan(37, 55, 37, 63);

        await Verifier<Arch005RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }
}
