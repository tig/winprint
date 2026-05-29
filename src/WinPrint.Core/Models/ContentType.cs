using System;
using System.Collections.Generic;

namespace WinPrint.Core.Models;

public class ContentType
{
    [SafeForTelemetry] public string Id { get; set; } = string.Empty;

    [SafeForTelemetry] public IList<string> Extensions { get; set; } = [];

    [SafeForTelemetry] public IList<string> Aliases { get; set; } = [];

    [SafeForTelemetry] public string Title { get; set; } = string.Empty;

    public override int GetHashCode()
    {
        return Id?.GetHashCode() ?? 0;
    }

    public override bool Equals(object? obj)
    {
        return obj is ContentType other && string.Equals(Id, other.Id, StringComparison.Ordinal);
    }
}
