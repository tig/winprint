using System;

namespace WinPrint.Core.Models;

[AttributeUsage (AttributeTargets.Property)]
public class SafeForTelemetry : Attribute
{
}
