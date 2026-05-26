using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace WinPrint.Core.Models;

[AttributeUsage (AttributeTargets.Property)]
public class SafeForTelemetry : Attribute
{
}
