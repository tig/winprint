using System.Collections.Generic;
namespace WinPrint.Core.Models;

/// <summary>
///     https://stackoverflow.com/questions/59516258/database-of-file-extensions-to-file-type-language-mappings
/// </summary>
public class FileTypeMapping : ModelBase
{
    //"filesAssociations": {
    //    "*.myphp": "php"
    //}

    //"contentTypes": [{
    //    "id": "text/x-java",
    //    "title": "java",
    //    "extensions": [ ".java", ".jav" ],
    //    "aliases": [ "Java", "java" ]
    //}]

    public Dictionary<string, string> FilesAssociations { get; set; } = new ();

    public IList<ContentType> ContentTypes { get; set; } = [];
}

public class ContentType
{
    [SafeForTelemetry]
    public string Id { get; set; } = string.Empty;

    [SafeForTelemetry]
    public IList<string> Extensions { get; set; } = [];

    [SafeForTelemetry]
    public IList<string> Aliases { get; set; } = [];

    [SafeForTelemetry]
    public string Title { get; set; } = string.Empty;

    public override int GetHashCode ()
    {
        return Id?.GetHashCode () ?? 0;
    }

    public override bool Equals (object? obj)
    {
        return obj is ContentType other && string.Equals (Id, other.Id, System.StringComparison.Ordinal);
    }
}
