namespace Swa.Analyzers.SampleApp.Arch007;

internal static class StringConcatenationInsideLoop_Invalid
{
    // Exemplos intencionais que DEVEM gerar diagnóstico ARCH007.

    public static string BuildCsv(IEnumerable<string> items)
    {
        var csv = string.Empty;

        foreach (var item in items)
        {
            csv += item + ",";
        }

        return csv;
    }
}
