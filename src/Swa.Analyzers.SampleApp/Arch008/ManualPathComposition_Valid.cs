namespace Swa.Analyzers.SampleApp.Arch008;

internal static class ManualPathComposition_Valid
{
    // Exemplos que NÃO devem gerar diagnóstico ARCH008.

    public static string FileReadAllText_WithPathCombine(string directoryPath, string fileName)
    {
        return File.ReadAllText(Path.Combine(directoryPath, fileName));
    }

    public static DirectoryInfo DirectoryCreateDirectory_WithPathJoin(string root, string folder)
    {
        return Directory.CreateDirectory(Path.Join(root, folder));
    }

    public static FileInfo FileInfoCtor_WithPathCombine(string directoryPath, string fileName)
    {
        return new FileInfo(Path.Combine(directoryPath, fileName));
    }
}
