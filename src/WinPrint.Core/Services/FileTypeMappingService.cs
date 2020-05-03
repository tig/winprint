using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using WinPrint.Core.Models;

namespace WinPrint.Core.Services {
    public class FileTypeMappingService {
        // Factory - creates 
        static public FileTypeMapping Create() {
            // 
            LogService.TraceMessage("FileAssociationsService.Create()");
            return ServiceLocator.Current.FileTypeMappingService.Load();
        }

        /// <summary>
        /// Loads file assocations (extentions to language mapping) from resources. Should be
        /// called after settings file has been loaded because it merges any associations 
        /// defined there in.
        /// </summary>
        /// <returns></returns>
        public FileTypeMapping Load() {
            FileTypeMapping associations = null;
            var jsonOptions = new JsonSerializerOptions {
                WriteIndented = true,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                //PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            associations = JsonSerializer.Deserialize<FileTypeMapping>(Properties.Resources.languages, jsonOptions);

            // TODO: Consider callilng into lang-map and/or Pygments to update extensions at runtime?
            // https://github.com/jonschlinkert/lang-map

            // Merge in any assocations set in settings file
            Debug.Assert(ModelLocator.Current.Settings.FileTypeMapping != null);
            Debug.Assert(ModelLocator.Current.Settings.FileTypeMapping.ContentTypes != null);
            foreach (var fa in ModelLocator.Current.Settings.FileTypeMapping.FilesAssociations) {
                associations.FilesAssociations[fa.Key] = fa.Value;
            }

            // Merge in any language defintions set in settings file
            var langs = new List<ContentType>(associations.ContentTypes);
            var langsSettings = new List<ContentType>(ModelLocator.Current.Settings.FileTypeMapping.ContentTypes);
            // TODO: overide Equals and GetHashCode for Langauge
            var result = langsSettings.Union(langs).ToList();

            associations.ContentTypes = result;

            return associations;
        }
    }
}
