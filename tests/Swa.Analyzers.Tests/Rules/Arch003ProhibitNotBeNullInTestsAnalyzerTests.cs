using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch003ProhibitNotBeNullInTestsAnalyzerTests
{
    [Fact]
    public async Task Reports_NotBeNull_in_test_method()
    {
        const string source = """
using System;
using FluentAssertions;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace FluentAssertions
{
    public static class AssertionExtensions
    {
        public static ObjectAssertions Should(this object? value) => new(value);
    }

    public sealed class ObjectAssertions
    {
        public ObjectAssertions(object? value) { }
        public void NotBeNull() { }
        public void NotBeNullOrEmpty() { }
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        object? value = new object();
        value.Should().NotBeNull();
    }
}
""";

        var expected = Verifier<Arch003ProhibitNotBeNullInTestsAnalyzer>.Diagnostic("ARCH003")
            .WithSpan(30, 24, 30, 33)
            .WithMessage("Avoid NotBeNull() in tests. Prefer a more specific assertion when possible.");

        await Verifier<Arch003ProhibitNotBeNullInTestsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_NotBeNull_inside_local_function_within_test_method()
    {
        const string source = """
using System;
using FluentAssertions;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace FluentAssertions
{
    public static class AssertionExtensions
    {
        public static ObjectAssertions Should(this object? value) => new(value);
    }

    public sealed class ObjectAssertions
    {
        public ObjectAssertions(object? value) { }
        public void NotBeNull() { }
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        void AssertNotNull(object? value)
        {
            value.Should().NotBeNull();
        }
    }
}
""";

        var expected = Verifier<Arch003ProhibitNotBeNullInTestsAnalyzer>.Diagnostic("ARCH003")
            .WithSpan(30, 28, 30, 37);

        await Verifier<Arch003ProhibitNotBeNullInTestsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_NotBeNull_via_conditional_access()
    {
        const string source = """
using System;
using FluentAssertions;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace FluentAssertions
{
    public static class AssertionExtensions
    {
        public static ObjectAssertions Should(this object? value) => new(value);
    }

    public sealed class ObjectAssertions
    {
        public ObjectAssertions(object? value) { }
        public void NotBeNull() { }
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        object? value = new object();
        value.Should()?.NotBeNull();
    }
}
""";

        var expected = Verifier<Arch003ProhibitNotBeNullInTestsAnalyzer>.Diagnostic("ARCH003")
            .WithSpan(29, 25, 29, 34);

        await Verifier<Arch003ProhibitNotBeNullInTestsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_NotBeNullOrEmpty()
    {
        const string source = """
using System;
using FluentAssertions;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace FluentAssertions
{
    public static class AssertionExtensions
    {
        public static ObjectAssertions Should(this object? value) => new(value);
    }

    public sealed class ObjectAssertions
    {
        public ObjectAssertions(object? value) { }
        public void NotBeNullOrEmpty() { }
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        object? value = new object();
        value.Should().NotBeNullOrEmpty();
    }
}
""";

        await Verifier<Arch003ProhibitNotBeNullInTestsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_NotBeNull_when_not_in_test_method()
    {
        const string source = """
using System;
using FluentAssertions;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace FluentAssertions
{
    public static class AssertionExtensions
    {
        public static ObjectAssertions Should(this object? value) => new(value);
    }

    public sealed class ObjectAssertions
    {
        public ObjectAssertions(object? value) { }
        public void NotBeNull() { }
    }
}

public sealed class Helper
{
    public void Validate(object? value)
    {
        value.Should().NotBeNull();
    }
}
""";

        await Verifier<Arch003ProhibitNotBeNullInTestsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Reports_NotBeNull_in_helper_method_inside_test_type()
    {
        const string source = """
using System;
using FluentAssertions;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace FluentAssertions
{
    public static class AssertionExtensions
    {
        public static ObjectAssertions Should(this object? value) => new(value);
    }

    public sealed class ObjectAssertions
    {
        public ObjectAssertions(object? value) { }
        public void NotBeNull() { }
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test() { }

    private static void AssertSomething(object? value)
    {
        value.Should().NotBeNull();
    }
}
""";

        var expected = Verifier<Arch003ProhibitNotBeNullInTestsAnalyzer>.Diagnostic("ARCH003")
            .WithSpan(30, 24, 30, 33);

        await Verifier<Arch003ProhibitNotBeNullInTestsAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_outside_test_project()
    {
        const string source = """
using FluentAssertions;

namespace FluentAssertions
{
    public static class AssertionExtensions
    {
        public static ObjectAssertions Should(this object? value) => new(value);
    }

    public sealed class ObjectAssertions
    {
        public ObjectAssertions(object? value) { }
        public void NotBeNull() { }
    }
}

public sealed class Sample
{
    public void Execute(object? value)
    {
        value.Should().NotBeNull();
    }
}
""";

        await Verifier<Arch003ProhibitNotBeNullInTestsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_other_NotBeNull_methods()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

public sealed class CustomAssertions
{
    public void NotBeNull() { }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        var assertions = new CustomAssertions();
        assertions.NotBeNull();
    }
}
""";

        await Verifier<Arch003ProhibitNotBeNullInTestsAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
