namespace Swa.Analyzers.SampleApp.Arch012;

internal static class DateTimeOffsetUsage_Valid
{
    // Exemplos que NÃO devem gerar diagnóstico ARCH012.

    public static DateTimeOffset GetTimestamp()
    {
        return DateTimeOffset.UtcNow;
    }

    public static void Process(DateTimeOffset timestamp)
    {
        _ = timestamp;
    }

    public static DateTimeOffset[] GetTimestamps()
    {
        return [];
    }
}
