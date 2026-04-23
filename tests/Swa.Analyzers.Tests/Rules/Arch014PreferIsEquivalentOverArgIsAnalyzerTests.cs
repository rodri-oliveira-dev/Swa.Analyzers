using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch014PreferIsEquivalentOverArgIsAnalyzerTests
{
    [Fact]
    public async Task Reports_ArgIs_when_used_in_test_method()
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
    using System;

    public static class Arg
    {
        public static bool Is<T>(Func<T, bool> predicate) => false;
    }

    public static class SubstituteExtensions
    {
        public static T Received<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(bool value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.Received().Do(NSubstitute.Arg.Is<int>(_ => true));
    }
}
""";

        var expected = Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.Diagnostic("ARCH014")
            .WithSpan(35, 50, 35, 57)
            .WithMessage("Prefer Is.Equivalent from the team's standard library instead of NSubstitute Arg.Is for value matching");

        await Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_ArgIs_with_simple_value_matching()
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
        public static bool Is<T>(T value) => false;
    }

    public static class SubstituteExtensions
    {
        public static T Received<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(bool value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.Received().Do(NSubstitute.Arg.Is(42));
    }
}
""";

        var expected = Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.Diagnostic("ARCH014")
            .WithSpan(33, 50, 33, 52)
            .WithMessage("Prefer Is.Equivalent from the team's standard library instead of NSubstitute Arg.Is for value matching");

        await Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_outside_test_project()
    {
        const string source = """
using System;

namespace NSubstitute
{
    public static class Arg
    {
        public static bool Is<T>(Func<T, bool> predicate) => false;
    }
}

public sealed class Sample
{
    public void Execute()
    {
        _ = NSubstitute.Arg.Is<int>(_ => true);
    }
}
""";

        await Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.VerifyAnalyzerAsync(source);
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
        public static bool Is<T>(Func<T, bool> predicate) => false;
    }
}

public sealed class Helper
{
    public void Execute()
    {
        _ = NSubstitute.Arg.Is<int>(_ => true);
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test() { }
}
""";

        await Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_other_ArgIs_methods_when_not_NSubstitute()
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
        public static bool Is<T>(Func<T, bool> predicate) => false;
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        _ = CustomSubstitute.Arg.Is<int>(_ => true);
    }
}
""";

        await Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Reports_ArgIs_when_used_with_NUnit_test_attribute()
    {
        const string source = """
using System;
using NSubstitute;

namespace NUnit.Framework
{
    public sealed class TestAttribute : Attribute { }
}

namespace NSubstitute
{
    using System;

    public static class Arg
    {
        public static bool Is<T>(Func<T, bool> predicate) => false;
    }

    public static class SubstituteExtensions
    {
        public static T Received<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(bool value);
}

public sealed class SampleTests
{
    [NUnit.Framework.Test]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.Received().Do(NSubstitute.Arg.Is<int>(_ => true));
    }
}
""";

        var expected = Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.Diagnostic("ARCH014")
            .WithSpan(35, 50, 35, 57)
            .WithMessage("Prefer Is.Equivalent from the team's standard library instead of NSubstitute Arg.Is for value matching");

        await Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_ArgIs_when_used_with_MSTest_test_attribute()
    {
        const string source = """
using System;
using NSubstitute;

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    public sealed class TestMethodAttribute : Attribute { }
}

namespace NSubstitute
{
    using System;

    public static class Arg
    {
        public static bool Is<T>(Func<T, bool> predicate) => false;
    }

    public static class SubstituteExtensions
    {
        public static T Received<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(bool value);
}

public sealed class SampleTests
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.Received().Do(NSubstitute.Arg.Is<int>(_ => true));
    }
}
""";

        var expected = Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.Diagnostic("ARCH014")
            .WithSpan(35, 50, 35, 57)
            .WithMessage("Prefer Is.Equivalent from the team's standard library instead of NSubstitute Arg.Is for value matching");

        await Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_ArgIs_when_used_with_alias()
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
    using System;

    public static class Arg
    {
        public static bool Is<T>(Func<T, bool> predicate) => false;
    }

    public static class SubstituteExtensions
    {
        public static T Received<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(bool value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.Received().Do(A.Is<int>(_ => true));
    }
}
""";

        var expected = Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.Diagnostic("ARCH014")
            .WithSpan(36, 36, 36, 43)
            .WithMessage("Prefer Is.Equivalent from the team's standard library instead of NSubstitute Arg.Is for value matching");

        await Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_ArgIs_when_used_in_test_type_helper_method()
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
    using System;

    public static class Arg
    {
        public static bool Is<T>(Func<T, bool> predicate) => false;
    }

    public static class SubstituteExtensions
    {
        public static T Received<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(bool value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test() => Helper();

    private void Helper()
    {
        IDependency substitute = null!;
        substitute.Received().Do(NSubstitute.Arg.Is<int>(_ => true));
    }
}
""";

        var expected = Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.Diagnostic("ARCH014")
            .WithSpan(37, 50, 37, 57)
            .WithMessage("Prefer Is.Equivalent from the team's standard library instead of NSubstitute Arg.Is for value matching");

        await Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_ArgIs_when_used_in_test_type_helper_method_without_test_attribute()
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
    using System;

    public static class Arg
    {
        public static bool Is<T>(Func<T, bool> predicate) => false;
    }

    public static class SubstituteExtensions
    {
        public static T Received<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(bool value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test() { }

    private void Setup()
    {
        IDependency substitute = null!;
        substitute.Received().Do(NSubstitute.Arg.Is<int>(_ => true));
    }
}
""";

        // Reports because the usage is inside a test type (a type that contains test methods)
        var expected = Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.Diagnostic("ARCH014")
            .WithSpan(37, 50, 37, 57)
            .WithMessage("Prefer Is.Equivalent from the team's standard library instead of NSubstitute Arg.Is for value matching");

        await Verifier<Arch014PreferIsEquivalentOverArgIsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }
}
