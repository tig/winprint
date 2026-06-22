namespace WinPrint.Core.Models;

public class Footer : HeaderFooter
{
    public override void CopyPropertiesFrom(ModelBase? source)
    {
        if (source is Footer src)
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
