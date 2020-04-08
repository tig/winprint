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
using System.Threading.Tasks;
using Serilog;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Core.ContentTypeEngines {
    /// <summary>
    /// Base class for Content/File Type Engines (CTEs)
    /// </summary>
    public abstract class ContentTypeEngineBase : ModelBase, INotifyPropertyChanged {
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
        public virtual string GetContentTypeName() {
            return _contentTypeName;
        }
        private static readonly string _contentTypeName = "base";

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
        public string Document {
            get => document; set =>
                //LogService.TraceMessage($"Document is {document.Length} chars.");
                SetField(ref document, value);
        }

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

        internal string document = null;

        public static readonly StringFormat StringFormat = new StringFormat(StringFormat.GenericTypographic) {
            FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.LineLimit | StringFormatFlags.FitBlackBox |
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
        /// <returns></returns>
        public static async Task<ContentTypeEngineBase> CreateContentTypeEngine(string contentType) {
            ContentTypeEngineBase cte = null;

            Debug.Assert(!string.IsNullOrEmpty(contentType));

            switch (contentType) {
                case "text/html":
                    cte = HtmlCte.Create();
                    break;

                case "text/plain":
                    cte = TextCte.Create();
                    break;

                // TODO: Figure out if we really want to use the sourcecode CTE.
                //case "text/sourcecode":
                //    cte = CodeCte.Create();
                //    ((CodeCte)cte).Language = contentType;
                //    break;

                default:
                    // It must be a language. Verify node.js and Prism are installed
                    if (await ServiceLocator.Current.NodeService.IsInstalled()) {
                        // contentType == Language
                        cte = PrismCte.Create();
                        ((PrismCte)cte).Language = contentType;
                    }
                    else {
                        Log.Information("Node.js must be installed for Prism-based ({lang}) syntax highlighting. Using {def} instead.", contentType, "text/plain");
                        cte = TextCte.Create();
                    }
                    break;
            }

            Debug.Assert(cte != null);
            return cte;
        }

        /// <summary>
        /// Returns the content type name (e.g. "text/plain") given a file path. If the content type
        /// cannot be determiend from FilesAssocaitons the default of "text/plain" is returned.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>The content type</returns>
        public static string GetContentType(string filePath) {
            var contentType = "text/plain";

            if (string.IsNullOrEmpty(filePath)) {
                return contentType;
            }

            // Expand path
            filePath = Path.GetFullPath(filePath);

            // If there's a file extension get the content type from the file type association mapper
            var ext = Path.GetExtension(filePath).ToLower();
            if (ext != string.Empty) {
                if (ModelLocator.Current.Associations.FilesAssociations.TryGetValue("*" + ext, out var ct)) {
                    contentType = ct;
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
