namespace WinPrint.Core.Models;

public class Header : HeaderFooter
{
    public override void CopyPropertiesFrom(ModelBase? source)
    {
        if (source is Header src)
        {
            CopyHeaderFooterFrom(src);
        }
    }

    public override IDictionary<string, string?> GetTelemetryDictionary()
    {
        Dictionary<string, string?> dictionary = TelemetryCollector.Create();
        AddHeaderFooterTelemetry(dictionary);
        return dictionary;
    }
}
