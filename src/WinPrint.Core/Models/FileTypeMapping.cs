using System;
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
