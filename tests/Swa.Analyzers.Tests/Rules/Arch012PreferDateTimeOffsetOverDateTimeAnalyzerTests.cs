using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch012PreferDateTimeOffsetOverDateTimeAnalyzerTests
{
    #region Invalid scenarios

    [Fact]
    public async Task Reports_DateTime_Field()
    {
        const string source = """
using System;

public sealed class Sample
{
    private DateTime _createdAt;
}
""";

        var expected = Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.Diagnostic("ARCH012")
            .WithSpan(5, 13, 5, 21);

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_DateTime_Property()
    {
        const string source = """
using System;

public sealed class Sample
{
    public DateTime CreatedAt { get; set; }
}
""";

        var expected = Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.Diagnostic("ARCH012")
            .WithSpan(5, 12, 5, 20);

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_DateTime_Method_Return()
    {
        const string source = """
using System;

public sealed class Sample
{
    public DateTime GetNow() => DateTime.UtcNow;
}
""";

        var expected = Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.Diagnostic("ARCH012")
            .WithSpan(5, 12, 5, 20);

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_DateTime_Method_Parameter()
    {
        const string source = """
using System;

public sealed class Sample
{
    public void Process(DateTime value) { }
}
""";

        var expected = Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.Diagnostic("ARCH012")
            .WithSpan(5, 25, 5, 33);

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_DateTime_Local_Variable()
    {
        const string source = """
using System;

public sealed class Sample
{
    public void Process()
    {
        DateTime value = DateTime.UtcNow;
    }
}
""";

        var expected = Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.Diagnostic("ARCH012")
            .WithSpan(7, 9, 7, 17);

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_Nullable_DateTime_Property()
    {
        const string source = """
using System;

public sealed class Sample
{
    public DateTime? CreatedAt { get; set; }
}
""";

        var expected = Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.Diagnostic("ARCH012")
            .WithSpan(5, 12, 5, 21);

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_DateTime_Array_Parameter()
    {
        const string source = """
using System;

public sealed class Sample
{
    public void Process(DateTime[] values) { }
}
""";

        var expected = Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.Diagnostic("ARCH012")
            .WithSpan(5, 25, 5, 35);

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_DateTime_In_For_Statement()
    {
        const string source = """
using System;

public sealed class Sample
{
    public void Process()
    {
        for (DateTime dt = DateTime.MinValue; dt < DateTime.MaxValue; dt = dt.AddDays(1))
        {
        }
    }
}
""";

        var expected = Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.Diagnostic("ARCH012")
            .WithSpan(7, 14, 7, 22);

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_DateTime_In_Abstract_Base_Method()
    {
        const string source = """
using System;

public abstract class Base
{
    public abstract DateTime GetDateTime();
}
""";

        var expected = Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.Diagnostic("ARCH012")
            .WithSpan(5, 21, 5, 29);

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_DateTime_In_Interface()
    {
        const string source = """
using System;

public interface IDateTimeProvider
{
    DateTime GetDateTime();
}
""";

        var expected = Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.Diagnostic("ARCH012")
            .WithSpan(5, 5, 5, 13);

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    #endregion

    #region Valid scenarios

    [Fact]
    public async Task Does_not_report_DateTimeOffset_Field()
    {
        const string source = """
using System;

public sealed class Sample
{
    private DateTimeOffset _createdAt;
}
""";

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_DateTimeOffset_Property()
    {
        const string source = """
using System;

public sealed class Sample
{
    public DateTimeOffset CreatedAt { get; set; }
}
""";

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_DateTime_in_var_declaration()
    {
        const string source = """
using System;

public sealed class Sample
{
    public void Process()
    {
        var value = DateTime.UtcNow;
    }
}
""";

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_DateTime_in_override()
    {
        const string source = """
using System;

public abstract class Base
{
    public abstract DateTime GetDateTime();
}

public sealed class Derived : Base
{
    public override DateTime GetDateTime() => DateTime.UtcNow;
}
""";

        // Only the abstract base declaration is reported; the override is not.
        var expected = Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.Diagnostic("ARCH012")
            .WithSpan(5, 21, 5, 29);

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_DateTime_in_explicit_interface()
    {
        const string source = """
using System;

public interface IDateTimeProvider
{
    DateTime GetDateTime();
}

public sealed class Provider : IDateTimeProvider
{
    DateTime IDateTimeProvider.GetDateTime() => DateTime.UtcNow;
}
""";

        // Only the interface declaration is reported; the explicit implementation is not.
        var expected = Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.Diagnostic("ARCH012")
            .WithSpan(5, 5, 5, 13);

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_DateTime_in_implicit_interface()
    {
        const string source = """
using System;

public interface IDateTimeProvider
{
    DateTime GetDateTime();
}

public sealed class Provider : IDateTimeProvider
{
    public DateTime GetDateTime() => DateTime.UtcNow;
}
""";

        // Only the interface declaration is reported; the implicit implementation is not.
        var expected = Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.Diagnostic("ARCH012")
            .WithSpan(5, 5, 5, 13);

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_DateTime_in_attribute()
    {
        const string source = """
using System;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CustomAttribute : Attribute
{
    public DateTime CreatedAt { get; set; }
}
""";

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_DateTime_in_extension_method_this_parameter()
    {
        const string source = """
using System;

public static class Extensions
{
    public static void Process(this DateTime value) { }
}
""";

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_int_local_variable()
    {
        const string source = """
public sealed class Sample
{
    public void Process()
    {
        int value = 42;
    }
}
""";

        await Verifier<Arch012PreferDateTimeOffsetOverDateTimeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    #endregion
}
