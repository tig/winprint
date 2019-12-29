using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace WinPrint.Core.Models {

    public class Langauge {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("extensions")]
        public IList<string> Extensions { get; set; }
        [JsonPropertyName("aliases")]
        public IList<string> Aliases { get; set; }
    }
    /// <summary>
    /// https://stackoverflow.com/questions/59516258/database-of-file-extensions-to-file-type-language-mappings
    /// </summary>
    /// 
    public class LanguageAssociations: ModelBase {
        //"files.associations": {
        //    "*.myphp": "php"
        //}

        //"languages": [{
        //    "id": "java",
        //    "extensions": [ ".java", ".jav" ],
        //    "aliases": [ "Java", "java" ]
        //}]

        [JsonPropertyName("files.associations")]
        public Dictionary<string, string> FilesAssociations { get; set; }

        [JsonPropertyName("languages")]
        public IList<Langauge> Languages { get; set; }

    }
}
