// Copyright Kindel Systems, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
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
        /// <returns>ContentEngine, Language</returns>
        public static (ContentTypeEngineBase cte, string language) CreateContentTypeEngine(string contentType) {
            Debug.Assert(!string.IsNullOrEmpty(contentType));
            Debug.Assert(ModelLocator.Current.Associations != null);
            Debug.Assert(ModelLocator.Current.Associations.Languages != null);

            // If contentType matches one of our CTE Names, this will succeed.
            ContentTypeEngineBase cte = GetDerivedClassesCollection().FirstOrDefault(c => contentType == c.GetType().Name);
            string language = string.Empty;

            if (cte == null) {
                //  {
                //  "id": "text/ansi",
                //  "aliases": [
                //    "ansi",
                //    "term"
                //              ],
                //  "title": "ANSI Encoded",
                //  "extensions": [
                //    ".an",
                //    ".ansi",
                //    ".ans"
                // },
                // Is it a file extension?
                var extLanguage = ModelLocator.Current.Associations.Languages
                    .Where(lang => lang.Extensions.Contains(contentType))
                    .FirstOrDefault();
                if (extLanguage != null && !string.IsNullOrEmpty(extLanguage.Id)) {
                    // Is Id a Cte Name?
                    cte = GetDerivedClassesCollection().FirstOrDefault(c => c.SupportedContentTypes.Contains(extLanguage.Id));

                    if (cte != null) {
                        return (cte, string.Empty);
                    }
                    // It is a language. Needs to be Syntax Highlighted. Use the default Syntax Highlighter CTE
                    language = extLanguage.Id;
                }
                else {
                    // Is it found in a langauge alias?
                    var alias = ModelLocator.Current.Associations.Languages
                        .Where(lang => lang.Id == contentType || lang.Aliases.Contains(contentType))
                        .FirstOrDefault();
                    if (alias != null) {
                        // Is the Id supported directly? 
                        // If by muplitple, pick the default.
                        var collection = GetDerivedClassesCollection()
                            .Where(c => c.SupportedContentTypes.Contains(alias.Id));
                        if (collection != null) {
                            if (collection.Count() > 1) {
                                cte = collection.Where(c => c.GetType().Name == DefaultCteClassName).First();
                            }
                            else {
                                cte = collection.FirstOrDefault();
                            }
                        }
                        if (cte != null) {
                            return (cte, string.Empty);
                        }
                        // It is a language. Needs to be Syntax Highlighted. Use the default Syntax Highlighter CTE
                        language = contentType;
                    }
                }

                if (string.IsNullOrEmpty(language)) {
                    cte = GetDerivedClassesCollection()
                        .Where(c => c.SupportedContentTypes.Contains(DefaultCteClassName))
                        .FirstOrDefault();
                }
                else {
                    // It is a language. Needs to be Syntax Highlighted. Use the default Syntax Highlighter CTE
                    cte = GetDerivedClassesCollection()
                        .Where(c => c.GetType().Name == DefaultSyntaxHighlighterCteNameClassName)
                        .First();
                }
            }

            Debug.Assert(cte != null);
            return (cte, language);
        }

        /// <summary>
        /// Returns the content type name (e.g. "text/plain") given a file path. If the content type
        /// cannot be determiend from FilesAssocaitons the default of "text/plain" is returned.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>The content type</returns>
        public static string GetContentTypeOrLanguage(string filePath) {
            var contentType = DefaultCteClassName;

            if (string.IsNullOrEmpty(filePath)) {
                return contentType;
            }

            // Expand path
            filePath = Path.GetFullPath(filePath);

            // If there's a file extension get the content type from the file type association mapper
            var ext = Path.GetExtension(filePath).ToLower();
            if (ext != string.Empty) {
                if (ModelLocator.Current.Associations.FilesAssociations.TryGetValue("*" + ext, out var ct)) {
                    // Now find Id in Languages
                    contentType = ModelLocator.Current.Associations.Languages
                        .Where(lang => lang.Id == ct)
                        .DefaultIfEmpty(new Langauge() { Id = DefaultContentType })
                        .First().Id;
                }
                else {
                    // No direct file extension, look in Languages
                    contentType = ModelLocator.Current.Associations.Languages
                        .Where(lang => lang.Extensions.Contains(ext))
                        .DefaultIfEmpty(new Langauge() { Id = DefaultContentType })
                        .First().Id;
                }
            }
            else {
                // Empty means no extension (e.g. .\.ssh\config) - use filename
                if (ModelLocator.Current.Associations.FilesAssociations.TryGetValue("*" + Path.GetFileName(filePath), out var ct)) {
                    contentType = ct;
                }
            }

            // If not text or html, is it a language?
            //if (!contentType.Equals("text/plain") && !contentType.Equals("text/html")) {
            //    // Technically, because we got the assocation from FilesAssocation, this should always work 
            //    if (!((List<Langauge>)ModelLocator.Current.Associations.Languages).Exists(lang => lang.Id == contentType))
            //        contentType = "text/plain";
            //}
            return contentType;
        }

        public abstract Task<bool> SetDocumentAsync(string document);
    }
}
