using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WinPrint.Core.Models;

namespace WinPrint.Core.Services {
    public class FileAssociationsService {
        // Factory - creates 
        static public FileAssociations Create() {
            // 
            Debug.WriteLine("FileAssociationsService.Create()");
            return ServiceLocator.Current.FileAssociationsService.LoadAssociations();
        }

        /// <summary>
        /// Loads file assocations (extentions to language mapping) from resources. Should be
        /// called after settings file has been loaded because it merges any associations 
        /// defined there in.
        /// </summary>
        /// <returns></returns>
        public FileAssociations LoadAssociations() {
            FileAssociations associations = null;

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
            associations = JsonSerializer.Deserialize<FileAssociations>(Properties.Resources.languages, jsonOptions);

            // TODO: Consider callilng into lang-map to update extensions at runtime?
            // https://github.com/jonschlinkert/lang-map

            // Merge in any assocations set in settings file
            foreach (var fa in ModelLocator.Current.Settings.LanguageAssociations.FilesAssociations) {
                associations.FilesAssociations[fa.Key] = fa.Value;
            }

            List<Langauge> langs = new List<Langauge>(associations.Languages);
            List<Langauge> langsSettings = new List<Langauge>(ModelLocator.Current.Settings.LanguageAssociations.Languages);

            // TODO: overide Equals and GetHashCode for Langauge
            var result = langsSettings.Union(langs).ToList();

            associations.Languages = result;

            return associations;
        }
    }
}
