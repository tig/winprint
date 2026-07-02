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

    public Dictionary<string, string> FilesAssociations { get; set; } = [];

    public IList<ContentType> ContentTypes { get; set; } = [];

    public override void CopyPropertiesFrom(ModelBase? source)
    {
        if (source is not FileTypeMapping src)
        {
            return;
        }

        FilesAssociations = new Dictionary<string, string>(src.FilesAssociations);
        ContentTypes = [.. src.ContentTypes];
    }
}
