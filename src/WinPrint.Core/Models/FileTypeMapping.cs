﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WinPrint.Core.Models {
    /// <summary>
    /// https://stackoverflow.com/questions/59516258/database-of-file-extensions-to-file-type-language-mappings
    /// </summary>
    /// 
    public class FileTypeMapping : ModelBase {
        //"files.associations": {
        //    "*.myphp": "php"
        //}

        //"languages": [{
        //    "id": "text/x-java",
        //    "title": "java",
        //    "extensions": [ ".java", ".jav" ],
        //    "aliases": [ "Java", "java" ]
        //}]

        [JsonPropertyName("files.associations")]
        public Dictionary<string, string> FilesAssociations { get; set; }

        // DO NOT RENAME THIS - Legacy
        [JsonPropertyName("languages")]
        public IList<ContentType> ContentTypes { get; set; }
    }

    public class ContentType {
        [JsonPropertyName("id")]
        [SafeForTelemetry]
        public string Id { get; set; }
        [JsonPropertyName("extensions")]
        [SafeForTelemetry]
        public IList<string> Extensions { get; set; }
        [JsonPropertyName("aliases")]
        [SafeForTelemetry]
        public IList<string> Aliases { get; set; }

        [JsonPropertyName("title")]
        [SafeForTelemetry]
        public string Title { get; set; }

        public override int GetHashCode() {
            return (Id).GetHashCode();
        }

        public override bool Equals(object obj) {
            return Id.Equals(((ContentType)obj).Id);
        }
    }
}
