namespace Swa.Analyzers.SampleApp.Arch007;

using System.Text;

internal static class StringBuilderInsideLoop_Valid
{
    // Exemplos que NÃO devem gerar diagnóstico ARCH007.

    public static string BuildCsv(IEnumerable<string> items)
    {
        var builder = new StringBuilder();

        foreach (var item in items)
        {
            builder.Append(item);
            builder.Append(',');
        }

        return builder.ToString();
    }
}
