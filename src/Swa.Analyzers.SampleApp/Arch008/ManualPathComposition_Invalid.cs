namespace Swa.Analyzers.SampleApp.Arch008;

internal static class ManualPathComposition_Invalid
{
    // Exemplos intencionais que DEVEM gerar diagnóstico ARCH008.
    // Os métodos não são executados; servem apenas como referência e validação manual.

    public static string FileReadAllText_WithStringConcatenation(string directoryPath, string fileName)
    {
        // ARCH008: composição manual de path via concatenação em sink de filesystem (File.*)
        return File.ReadAllText(directoryPath + "/" + fileName);
    }

    public static DirectoryInfo DirectoryCreateDirectory_WithInterpolation(string root, string folder)
    {
        // ARCH008: composição manual de path via interpolação em sink de filesystem (Directory.*)
        return Directory.CreateDirectory($"{root}/{folder}");
    }

    public static FileInfo FileInfoCtor_WithStringConcatenation(string directoryPath, string fileName)
    {
        // ARCH008: composição manual de path via concatenação em sink de filesystem (new FileInfo(...))
        return new FileInfo(directoryPath + "\\" + fileName);
    }
}
