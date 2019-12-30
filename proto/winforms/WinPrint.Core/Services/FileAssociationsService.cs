using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;
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

        //    internal static string GetDocType() {

        //        string ext = Path.GetExtension(File);
        //        //Debug.WriteLine(FileExtentionInfo(AssocStr.Command, ext), "Command");
        //        //Debug.WriteLine(FileExtentionInfo(AssocStr.DDEApplication, ext), "DDEApplication");
        //        //Debug.WriteLine(FileExtentionInfo(AssocStr.DDEIfExec, ext), "DDEIfExec");
        //        //Debug.WriteLine(FileExtentionInfo(AssocStr.DDETopic, ext), "DDETopic");
        //        //Debug.WriteLine(FileExtentionInfo(AssocStr.Executable, ext), "Executable");
        //        //Debug.WriteLine(FileExtentionInfo(AssocStr.FriendlyAppName, ext), "FriendlyAppName");
        //        //Debug.WriteLine(FileExtentionInfo(AssocStr.FriendlyDocName, ext), "FriendlyDocName");
        //        //Debug.WriteLine(FileExtentionInfo(AssocStr.NoOpen, ext), "NoOpen");
        //        //Debug.WriteLine(FileExtentionInfo(AssocStr.ShellNewValue, ext), "ShellNewValue");

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
        //    //    Debug.WriteLine(FileExtentionInfo(AssocStr.Command, ext), "Command");
        //    //    Debug.WriteLine(FileExtentionInfo(AssocStr.DDEApplication, ext), "DDEApplication");
        //    //    Debug.WriteLine(FileExtentionInfo(AssocStr.DDEIfExec, ext), "DDEIfExec");
        //    //    Debug.WriteLine(FileExtentionInfo(AssocStr.DDETopic, ext), "DDETopic");
        //    //    Debug.WriteLine(FileExtentionInfo(AssocStr.Executable, ext), "Executable");
        //    //    Debug.WriteLine(FileExtentionInfo(AssocStr.FriendlyAppName, ext), "FriendlyAppName");
        //    //    Debug.WriteLine(FileExtentionInfo(AssocStr.FriendlyDocName, ext), "FriendlyDocName");
        //    //    Debug.WriteLine(FileExtentionInfo(AssocStr.NoOpen, ext), "NoOpen");
        //    //    Debug.WriteLine(FileExtentionInfo(AssocStr.ShellNewValue, ext), "ShellNewValue");

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
