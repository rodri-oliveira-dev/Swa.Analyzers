using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch008ProhibitManualPathCompositionAnalyzerTests
{
    [Fact]
    public async Task Reports_when_using_string_concatenation_in_File_sink()
    {
        const string source = """
using System.IO;

public sealed class Sample
{
    public string Read(string dir, string name)
    {
        return File.ReadAllText({|#0:dir + "/" + name|});
    }
}
""";

        var expected = Verifier<Arch008ProhibitManualPathCompositionAnalyzer>.Diagnostic("ARCH008")
            .WithLocation(0)
            .WithArguments("path")
            .WithMessage("Avoid manual path composition for 'path'. Use Path.Combine or Path.Join.");

        await Verifier<Arch008ProhibitManualPathCompositionAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_when_using_interpolated_string_in_Directory_sink()
    {
        const string source = """
using System.IO;

public sealed class Sample
{
    public void Create(string root, string folder)
    {
        Directory.CreateDirectory({|#0:$"{root}/{folder}"|});
    }
}
""";

        var expected = Verifier<Arch008ProhibitManualPathCompositionAnalyzer>.Diagnostic("ARCH008")
            .WithLocation(0)
            .WithArguments("path");

        await Verifier<Arch008ProhibitManualPathCompositionAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_when_using_string_concatenation_in_FileInfo_constructor()
    {
        const string source = """
using System.IO;

public sealed class Sample
{
    public FileInfo Get(string dir, string file)
    {
        return new FileInfo({|#0:dir + "\\" + file|});
    }
}
""";

        var expected = Verifier<Arch008ProhibitManualPathCompositionAnalyzer>.Diagnostic("ARCH008")
            .WithLocation(0)
            .WithArguments("fileName");

        await Verifier<Arch008ProhibitManualPathCompositionAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_when_using_Path_DirectorySeparatorChar_in_concatenation()
    {
        const string source = """
using System.IO;

public sealed class Sample
{
    public void Write(string dir, string name)
    {
        File.WriteAllText({|#0:dir + Path.DirectorySeparatorChar + name|}, "content");
    }
}
""";

        var expected = Verifier<Arch008ProhibitManualPathCompositionAnalyzer>.Diagnostic("ARCH008")
            .WithLocation(0)
            .WithArguments("path");

        await Verifier<Arch008ProhibitManualPathCompositionAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_when_using_Path_Combine()
    {
        const string source = """
using System.IO;

public sealed class Sample
{
    public string Read(string dir, string name)
    {
        return File.ReadAllText(Path.Combine(dir, name));
    }
}
""";

        await Verifier<Arch008ProhibitManualPathCompositionAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_composing_string_outside_filesystem_sink()
    {
        const string source = """
using System;

public sealed class Sample
{
    public void Log(string dir, string name)
    {
        var path = dir + "/" + name;
        Console.WriteLine(path);
    }
}
""";

        await Verifier<Arch008ProhibitManualPathCompositionAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_argument_is_already_a_path_variable()
    {
        const string source = """
using System.IO;

public sealed class Sample
{
    public string Read(string path)
    {
        return File.ReadAllText(path);
    }
}
""";

        await Verifier<Arch008ProhibitManualPathCompositionAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_interpolated_string_is_identity()
    {
        const string source = """
using System.IO;

public sealed class Sample
{
    public string Read(string path)
    {
        return File.ReadAllText($"{path}");
    }
}
""";

        await Verifier<Arch008ProhibitManualPathCompositionAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_interpolated_string_is_used_for_non_path_parameter()
    {
        const string source = """
using System.Collections.Generic;
using System.IO;

public sealed class Sample
{
    public IEnumerable<string> Enumerate(string path, string pattern)
    {
        return Directory.EnumerateFiles(path, $"{pattern}");
    }
}
""";

        await Verifier<Arch008ProhibitManualPathCompositionAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_for_lookalike_custom_File_type()
    {
        const string source = """
namespace CustomIO
{
    public static class File
    {
        public static void ReadAllText(string path) { }
    }
}

public sealed class Sample
{
    public void Execute(string dir, string name)
    {
        CustomIO.File.ReadAllText(dir + "/" + name);
    }
}
""";

        await Verifier<Arch008ProhibitManualPathCompositionAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
