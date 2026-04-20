using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch006WarnOnExcludingInBeEquivalentToAnalyzerTests
{
    [Fact]
    public async Task Reports_Excluding_in_BeEquivalentTo_options()
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
        public void BeEquivalentTo(object? expected, Func<Equivalency.EquivalencyAssertionOptions, Equivalency.EquivalencyAssertionOptions> config)
        {
        }
    }

    namespace Equivalency
    {
        public sealed class EquivalencyAssertionOptions
        {
            public EquivalencyAssertionOptions Excluding(Func<object, bool> predicate) => this;
        }
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        var actual = new { A = 1, B = 2 };
        var expected = new { A = 1, B = 999 };

        actual.Should().BeEquivalentTo(expected, options => options.{|#0:Excluding|}(x => true));
    }
}
""";

        var expected = Verifier<Arch006WarnOnExcludingInBeEquivalentToAnalyzer>.Diagnostic("ARCH006")
            .WithLocation(0)
            .WithArguments("Excluding")
            .WithMessage("Avoid using 'Excluding' in BeEquivalentTo() options. Exclusions can reduce test precision.");

        await Verifier<Arch006WarnOnExcludingInBeEquivalentToAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_ExcludingMissingMembers_in_BeEquivalentTo_options()
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
        public void BeEquivalentTo(object? expected, Func<Equivalency.EquivalencyAssertionOptions, Equivalency.EquivalencyAssertionOptions> config)
        {
        }
    }

    namespace Equivalency
    {
        public sealed class EquivalencyAssertionOptions
        {
            public EquivalencyAssertionOptions ExcludingMissingMembers() => this;
        }
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        var actual = new { A = 1, B = 2 };
        var expected = new { A = 1 };

        actual.Should().BeEquivalentTo(expected, options => options.{|#0:ExcludingMissingMembers|}());
    }
}
""";

        var expected = Verifier<Arch006WarnOnExcludingInBeEquivalentToAnalyzer>.Diagnostic("ARCH006")
            .WithLocation(0)
            .WithArguments("ExcludingMissingMembers");

        await Verifier<Arch006WarnOnExcludingInBeEquivalentToAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_when_no_excluding_is_used()
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
        public void BeEquivalentTo(object? expected, Func<Equivalency.EquivalencyAssertionOptions, Equivalency.EquivalencyAssertionOptions> config)
        {
        }
    }

    namespace Equivalency
    {
        public sealed class EquivalencyAssertionOptions
        {
            public EquivalencyAssertionOptions Using(Func<object, object> action) => this;
        }
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        var actual = new { A = 1 };
        var expected = new { A = 1 };

        actual.Should().BeEquivalentTo(expected, options => options.Using(x => x));
    }
}
""";

        await Verifier<Arch006WarnOnExcludingInBeEquivalentToAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_not_in_test_project()
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
        public void BeEquivalentTo(object? expected, System.Func<Equivalency.EquivalencyAssertionOptions, Equivalency.EquivalencyAssertionOptions> config)
        {
        }
    }

    namespace Equivalency
    {
        public sealed class EquivalencyAssertionOptions
        {
            public EquivalencyAssertionOptions Excluding(System.Func<object, bool> predicate) => this;
        }
    }
}

public sealed class Sample
{
    public void Execute()
    {
        var actual = new { A = 1, B = 2 };
        var expected = new { A = 1, B = 999 };

        actual.Should().BeEquivalentTo(expected, options => options.Excluding(x => true));
    }
}
""";

        await Verifier<Arch006WarnOnExcludingInBeEquivalentToAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_other_BeEquivalentTo_lookalike_APIs()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace CustomAssertions
{
    public sealed class ObjectAssertions
    {
        public void BeEquivalentTo(object expected, Func<EquivalencyAssertionOptions, EquivalencyAssertionOptions> config) { }
    }

    public sealed class EquivalencyAssertionOptions
    {
        public EquivalencyAssertionOptions Excluding(Func<object, bool> predicate) => this;
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        var assertions = new CustomAssertions.ObjectAssertions();
        assertions.BeEquivalentTo(new object(), options => options.Excluding(x => true));
    }
}
""";

        await Verifier<Arch006WarnOnExcludingInBeEquivalentToAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_Excluding_outside_BeEquivalentTo_options_delegate()
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
        public void BeEquivalentTo(object? expected) { }
    }

    namespace Equivalency
    {
        public sealed class EquivalencyAssertionOptions
        {
            public EquivalencyAssertionOptions Excluding(Func<object, bool> predicate) => this;
        }
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        // This is intentionally outside any BeEquivalentTo options delegate.
        var options = new FluentAssertions.Equivalency.EquivalencyAssertionOptions();
        options.Excluding(x => true);
    }
}
""";

        await Verifier<Arch006WarnOnExcludingInBeEquivalentToAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
