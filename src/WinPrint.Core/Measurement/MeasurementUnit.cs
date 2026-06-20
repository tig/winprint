namespace WinPrint.Core.Measurement;

/// <summary>
///     Unit a size/length value is <em>displayed and edited</em> in. winprint stores sizes canonically
///     in hundredths of an inch (e.g. <c>PrintMargins</c>); the editing layer converts to/from one of
///     these units for presentation. <see cref="Inch" /> is wired today; <see cref="Centimeter" /> is
///     declared so metric support can be added later, but conversion is not implemented yet.
/// </summary>
public enum MeasurementUnit
{
    /// <summary>Inches, shown with two decimal places (e.g. <c>0.75</c>). The default editing unit.</summary>
    Inch,

    /// <summary>Centimeters. Reserved for future metric support; not yet converted.</summary>
    Centimeter
}
