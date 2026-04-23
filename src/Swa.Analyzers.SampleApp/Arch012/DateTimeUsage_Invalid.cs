namespace Swa.Analyzers.SampleApp.Arch012;

internal static class DateTimeUsage_Invalid
{
    // Exemplos intencionais que DEVEM gerar diagnóstico ARCH012.

    public static DateTime GetTimestamp()
    {
        return DateTime.UtcNow;
    }

    public static void Process(DateTime timestamp)
    {
        _ = timestamp;
    }

    public static DateTime[] GetTimestamps()
    {
        return [];
    }
}
