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

            // Load assocations from resources
            //var assembly = Assembly.GetExecutingAssembly();
            //var resourceName = "MyCompany.MyProduct.MyFile.txt";

            //using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            //using (StreamReader reader = new StreamReader(stream)) {
            //    string result = reader.ReadToEnd();
            //}
            var jsonOptions = new JsonSerializerOptions {
                WriteIndented = true,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                //PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            //string s = System.Text.Encoding.Default.GetString(Properties.Resources.languages) ;
            associations = JsonSerializer.Deserialize<FileTypeMapping>(Properties.Resources.languages, jsonOptions);

            // TODO: Consider callilng into lang-map to update extensions at runtime?
            // https://github.com/jonschlinkert/lang-map

            // Merge in any assocations set in settings file
            Debug.Assert(ModelLocator.Current.Settings.FileTypeMapping != null);
            Debug.Assert(ModelLocator.Current.Settings.FileTypeMapping.Languages != null);

            foreach (var fa in ModelLocator.Current.Settings.FileTypeMapping.FilesAssociations) {
                associations.FilesAssociations[fa.Key] = fa.Value;
            }

            var langs = new List<Langauge>(associations.Languages);
            var langsSettings = new List<Langauge>(ModelLocator.Current.Settings.FileTypeMapping.Languages);

            // TODO: overide Equals and GetHashCode for Langauge
            var result = langsSettings.Union(langs).ToList();

            associations.Languages = result;

            return associations;
        }
    }
}
