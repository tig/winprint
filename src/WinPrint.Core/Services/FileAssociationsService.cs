using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WinPrint.Core.Models;

namespace WinPrint.Core.Services {
    public class FileAssociationsService {
        // Factory - creates 
        static public FileAssociations Create() {
            // 
            LogService.TraceMessage("FileAssociationsService.Create()");
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

        //    internal static string GetDocType() {

        //        string ext = Path.GetExtension(File);
        //        //Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.Command, ext), "Command");
        //        //Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.DDEApplication, ext), "DDEApplication");
        //        //Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.DDEIfExec, ext), "DDEIfExec");
        //        //Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.DDETopic, ext), "DDETopic");
        //        //Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.Executable, ext), "Executable");
        //        //Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.FriendlyAppName, ext), "FriendlyAppName");
        //        //Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.FriendlyDocName, ext), "FriendlyDocName");
        //        //Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.NoOpen, ext), "NoOpen");
        //        //Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.ShellNewValue, ext), "ShellNewValue");

        //        //return Native.FileExtentionInfo(Native.AssocStr.FriendlyDocName, ext);

        //        string mimeType = "application/unknown";

        //        RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(Path.GetExtension(file).ToLower());

        //        if (regKey != null) {
        //            object contentType = regKey.GetValue("Content Type");

        //            if (contentType != null)
        //                mimeType = contentType.ToString();
        //        }

        //        if (ext == ".cs")
        //            mimeType = "csharp";

        //        return mimeType;
        //    }


        //    ///// <summary>
        //    ///// The main entry point for the application.
        //    ///// </summary>
        //    //[STAThread]
        //    //static void Main() {
        //    //    Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.Command, ext), "Command");
        //    //    Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.DDEApplication, ext), "DDEApplication");
        //    //    Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.DDEIfExec, ext), "DDEIfExec");
        //    //    Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.DDETopic, ext), "DDETopic");
        //    //    Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.Executable, ext), "Executable");
        //    //    Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.FriendlyAppName, ext), "FriendlyAppName");
        //    //    Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.FriendlyDocName, ext), "FriendlyDocName");
        //    //    Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.NoOpen, ext), "NoOpen");
        //    //    Helpers.Logging.TraceMessage(FileExtentionInfo(AssocStr.ShellNewValue, ext), "ShellNewValue");

        //    //    //  DDEApplication: WinWord
        //    //    //DDEIfExec: Ñﻴ߾
        //    //    //  DDETopic: System
        //    //    //  Executable: C:\Program Files (x86)\Microsoft Office\Office12\WINWORD.EXE
        //    //    //  FriendlyAppName: Microsoft Office Word
        //    //    //  FriendlyDocName: Microsoft Office Word 97 - 2003 Document


        //    //}
        //}
    }
}
