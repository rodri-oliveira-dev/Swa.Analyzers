using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Swa.Analyzers.Core.Common;

internal static class AnalyzerConfigOptionsExtensions
{
    public static bool GetBooleanOption(this AnalyzerConfigOptions options, string key, bool defaultValue)
    {
        if (options.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    public static ImmutableArray<string> GetCsvOption(this AnalyzerConfigOptions options, string key, IEnumerable<string> defaults)
    {
        if (!options.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return Normalize(defaults);
        }

        return Normalize(raw.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries));
    }

    private static ImmutableArray<string> Normalize(IEnumerable<string> values)
    {
        var builder = ImmutableArray.CreateBuilder<string>();
        foreach (var value in values)
        {
            var normalized = value.Trim();
            if (!string.IsNullOrEmpty(normalized))
            {
                builder.Add(normalized);
            }
        }

        return builder.ToImmutable();
    }
}
