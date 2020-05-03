// Copyright Kindel Systems, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Serilog;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Core.ContentTypeEngines {
    /// <summary>
    /// Base class for Content/File Type Engines (CTEs)
    /// </summary>
    public abstract class ContentTypeEngineBase : ModelBase, INotifyPropertyChanged {
        public static string DefaultContentType = "text/plain";
        public static string DefaultCteClassName = "AnsiCte";
        public static string DefaultSyntaxHighlighterCteNameClassName = "AnsiCte";

        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected new bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(field, value)) {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            OnSettingsChanged(true);
            return true;
        }

        // if bool is true, reflow. Otherwise just paint
        public event EventHandler<bool> SettingsChanged;
        protected void OnSettingsChanged(bool reflow) {
            SettingsChanged?.Invoke(this, reflow);
        }

        /// <summary>
        /// ContentType identifier (shorthand for class name). 
        /// </summary>
        public virtual string[] SupportedContentTypes => _supportedContentTypes;
        private static readonly string[] _supportedContentTypes = null;

        /// <summary>
        /// Calculated page size. Set by Sheet view model.
        /// </summary>
        public SizeF PageSize;

        /// <summary>
        /// Holds content settings for the CTE. These are used as defaults when a Sheet does not
        /// specify any.
        /// </summary>
        public ContentSettings ContentSettings { get => contentSettings; set => SetField(ref contentSettings, value); }
        private ContentSettings contentSettings;// = new ContentSettings();

        /// <summary>
        /// The contents of the file to be printed.
        /// </summary>
        [JsonIgnore]
        public string Document {
            get => _document; set =>
                //LogService.TraceMessage($"Document is {document.Length} chars.");
                SetField(ref _document, value);
        }
        internal string _document = null;

        /// <summary>
        /// The contents encdding of the file to be printed.
        /// </summary>
        [JsonIgnore]
        public Encoding Encoding { get => _encoding; set => SetField(ref _encoding, value); }
        private Encoding _encoding = Encoding.Default;

        /// <summary>
        /// https://stackoverflow.com/questions/5411694/get-all-inherited-classes-of-an-abstract-class
        /// </summary>
        public static ICollection<ContentTypeEngineBase> GetDerivedClassesCollection() {
            var objects = new List<ContentTypeEngineBase>();
            foreach (var type in typeof(ContentTypeEngineBase).Assembly.GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(ContentTypeEngineBase)))) {
                objects.Add((ContentTypeEngineBase)Activator.CreateInstance(type));
            }
            return objects;
        }

        /// <summary>
        /// These are the global StringFormat settings; set here to ensure all rendering and measuring uses same settings
        /// </summary>
        public static readonly StringFormat StringFormat = new StringFormat(StringFormat.GenericTypographic) {
            FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.LineLimit | //StringFormatFlags.FitBlackBox |
                            StringFormatFlags.DisplayFormatControl | StringFormatFlags.MeasureTrailingSpaces,
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.None
        };
        public static readonly TextRenderingHint TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        /// <summary>
        /// Get total count of pages. Set any local page-size related values (e.g. linesPerPage).
        /// </summary>
        /// <param name="e"></param>
        /// <returns>Number of sheets.</returns>
        public virtual async Task<int> RenderAsync(System.Drawing.Printing.PrinterResolution printerResolution, EventHandler<string> reflowProgress) {
            if (Document == null) {
                throw new ArgumentNullException("Document can't be null for Render");
            }

            return await Task.FromResult(0);
        }

        /// <summary>
        /// Paints a single page
        /// </summary>
        /// <param name="g">Graphics with 0,0 being the origin of the Page</param>
        /// <param name="pageNum">Page number to print</param>
        public abstract void PaintPage(Graphics g, int pageNum);

        /// <summary>
        /// Creates the appropriate Content Type Engine instance given a content type string.
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns>ContentEngine, ContentType, Language</returns>
        public static (ContentTypeEngineBase cte, string languageId, string Language) CreateContentTypeEngine(string contentType) {
            Debug.Assert(!string.IsNullOrEmpty(contentType));
            Debug.Assert(ModelLocator.Current.FileTypeMapping != null);
            Debug.Assert(ModelLocator.Current.FileTypeMapping.ContentTypes != null);

            // If contentType matches one of our CTE Names, this will succeed.
            ContentTypeEngineBase cte = GetDerivedClassesCollection().FirstOrDefault(c => contentType.Equals(c.GetType().Name, StringComparison.OrdinalIgnoreCase));
            string language = string.Empty;
            string languageId = string.Empty;

            if (cte != null) {
                languageId = cte.SupportedContentTypes[0];
                language = ModelLocator.Current.FileTypeMapping.ContentTypes.FirstOrDefault(lang => lang.Id.Equals(languageId, StringComparison.OrdinalIgnoreCase)).Title;
                return (cte, languageId, language);
            }

            //  {
            //  "id": "text/ansi",
            //  "aliases": [
            //    "ansi",
            //    "term"
            //              ],
            //  "title": "ANSI Encoded",
            //  "extensions": [
            //    "*.an",
            //    "*.ansi",
            //    "*.ans"
            // },
            // Is it a file extension? (*.an)

            var extension = ModelLocator.Current.FileTypeMapping.ContentTypes
                .FirstOrDefault(l => l.Extensions
                    .Where(i => CultureInfo.CurrentCulture.CompareInfo.Compare(i, contentType, CompareOptions.IgnoreCase) == 0).Count() > 0);
            if (extension != null && !string.IsNullOrEmpty(extension.Id)) {
                // Is Id directly supported by a Cte?
                cte = GetDerivedClassesCollection().FirstOrDefault(c => c.SupportedContentTypes.Contains(extension.Id));
                if (cte != null) {
                    return (cte, extension.Id, extension.Title);
                }

                // It is a language. Needs to be Syntax Highlighted. Use the default Syntax Highlighter CTE
                languageId = extension.Id;
                language = extension.Title;
            }
            else {
                // Is it a content type (Landuage.Id)? (text/ansi)
                var lang = ModelLocator.Current.FileTypeMapping.ContentTypes.FirstOrDefault(l => l.Id.Equals(contentType, StringComparison.OrdinalIgnoreCase));
                if (lang != null) {
                    languageId = lang.Id;
                    language = lang.Title;
                }

                // Is it a language Title?
                lang = ModelLocator.Current.FileTypeMapping.ContentTypes.FirstOrDefault(l => l.Title.Equals(contentType, StringComparison.OrdinalIgnoreCase));
                if (lang != null) {
                    languageId = lang.Id;
                    language = lang.Title;
                }

                // Is it a language name found in a Language alias? (ansi)
                lang = ModelLocator.Current.FileTypeMapping.ContentTypes
                    .FirstOrDefault(l => l.Aliases
                        .Where(i => CultureInfo.CurrentCulture.CompareInfo.Compare(i, contentType, CompareOptions.IgnoreCase) == 0).Count() > 0);
                if (lang != null) {
                    languageId = lang.Id;
                    language = lang.Title;
                }

                if (!string.IsNullOrEmpty(language) && !string.IsNullOrEmpty(languageId)) {
                    // Is the Id supported directly (e.g. text/html is supported directly by HtmlCte) 
                    // If supported by muplitple, pick the default.
                    var ctes = GetDerivedClassesCollection().Where(c => c.SupportedContentTypes.Contains(languageId.ToLower()));
                    if (ctes != null) {
                        if (ctes.Count() > 1) {
                            cte = ctes.First(c => c.GetType().Name == DefaultCteClassName);
                        }
                        else {
                            cte = ctes.FirstOrDefault();
                        }
                        if (cte != null) {
                            return (cte, languageId, ModelLocator.Current.FileTypeMapping.ContentTypes.FirstOrDefault(l => l.Id.Equals(languageId, StringComparison.OrdinalIgnoreCase)).Title);
                        }
                    }

                    // It is a language. Needs to be Syntax Highlighted. Use the default Syntax Highlighter CTE
                    //languageId = lang.Id;
                    //language = lang.Title;
                }
            }

            if (string.IsNullOrEmpty(languageId)) {
                // Didn't find a content type so use default CTE
                cte = GetDerivedClassesCollection().FirstOrDefault(c => c.SupportedContentTypes.Contains(DefaultContentType));
                languageId = cte.SupportedContentTypes[0];
                language = ModelLocator.Current.FileTypeMapping.ContentTypes.FirstOrDefault(l => l.Id.Equals(languageId, StringComparison.OrdinalIgnoreCase)).Title;
            }
            else {
                // It is a language. Needs to be Syntax Highlighted. Use the default Syntax Highlighter CTE
                cte = GetDerivedClassesCollection().FirstOrDefault(c => c.GetType().Name.Equals(DefaultSyntaxHighlighterCteNameClassName, StringComparison.OrdinalIgnoreCase));
                //if (string.IsNullOrWhiteSpace(language)) {
                //    language = contentType;
                //}
            }

            return (cte, languageId, language);
        }

        /// <summary>
        /// Returns the content type name (e.g. "text/plain") given a file path. If the content type
        /// cannot be determiend from FilesAssocaitons the default of "text/plain" is returned.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>The content type</returns>
        public static string GetContentType(string filePath) {
            var contentType = DefaultContentType;

            if (string.IsNullOrEmpty(filePath)) {
                return contentType;
            }

            // Expand path
            filePath = Path.GetFullPath(filePath);

            // If there's a file extension get the content type from the file type association mapper
            var ext = Path.GetExtension(filePath).ToLower();
            if (ext != string.Empty) {
                // BUGBUG: This assumes all extensions in FilesAssociations are lowercase
                if (ModelLocator.Current.FileTypeMapping.FilesAssociations.TryGetValue("*" + ext, out var ct)) {
                    // Now find Id in Languages
                    contentType = ModelLocator.Current.FileTypeMapping.ContentTypes
                        .Where(lang => lang.Id.Equals(ct, StringComparison.OrdinalIgnoreCase))
                        .DefaultIfEmpty(new ContentType() { Id = DefaultContentType })
                        .First().Id;
                }
                else {
                    // No direct file extension, look in Languages
                    contentType = ModelLocator.Current.FileTypeMapping.ContentTypes
                        .Where(lang => lang.Extensions.Where(i => CultureInfo.CurrentCulture.CompareInfo.Compare(i, "*" + ext, CompareOptions.IgnoreCase) == 0 ||
                                CultureInfo.CurrentCulture.CompareInfo.Compare(i, ext, CompareOptions.IgnoreCase) == 0).Count() > 0)
                        .DefaultIfEmpty(new ContentType() { Id = DefaultContentType })
                        .First().Id;
                }
            }
            else {
                // Empty means no extension (e.g. .\.ssh\config) - use filename
                if (ModelLocator.Current.FileTypeMapping.FilesAssociations.TryGetValue("*" + Path.GetFileName(filePath), out var ct)) {
                    contentType = ct;
                }
                else {
                    // No direct file extension, look in Languages
                    contentType = ModelLocator.Current.FileTypeMapping.ContentTypes
                        .Where(lang => lang.Extensions.Where(i => CultureInfo.CurrentCulture.CompareInfo.Compare(i, Path.GetFileName(filePath), CompareOptions.IgnoreCase) == 0).Count() > 0)
                        .DefaultIfEmpty(new ContentType() { Id = DefaultContentType })
                        .First().Id;
                }
            }

            // If not text or html, is it a language?
            //if (!contentType.Equals("text/plain") && !contentType.Equals("text/html")) {
            //    // Technically, because we got the assocation from FilesAssocation, this should always work 
            //    if (!((List<Language>)ModelLocator.Current.Associations.Languages).Exists(lang => lang.Id == contentType))
            //        contentType = "text/plain";
            //}
            return contentType;
        }

        public abstract Task<bool> SetDocumentAsync(string document);
    }
}
