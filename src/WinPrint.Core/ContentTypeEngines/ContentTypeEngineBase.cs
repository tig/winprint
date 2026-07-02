// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
#if WINDOWS
using System.Drawing.Printing;
using System.Drawing.Text;
#endif
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     Base class for Content/File Type Engines (CTEs)
/// </summary>
public abstract class ContentTypeEngineBase : ModelBase, INotifyPropertyChanged
{
    // These can be overidden in Settings
    //public static string DefaultContentType = "text/plain";
    public static string DefaultCteClassName = "AnsiCte";
    public static string DefaultSyntaxHighlighterCteNameClassName = "TextMateCte";
    private static readonly string[] s_supportedContentTypes = [];

#if WINDOWS
    /// <summary>
    ///     These are the global StringFormat settings; set here to ensure all rendering and measuring uses same settings.
    ///     Constructing a <see cref="System.Drawing.StringFormat" /> calls into GDI+, so it is lazily initialized:
    ///     this lets the type be loaded on a host without GDI+ (e.g. Linux CI running the Windows-targeted test
    ///     assembly) as long as this member is not actually used.
    /// </summary>
    private static readonly Lazy<StringFormat> s_stringFormat = new(() =>
        new StringFormat(StringFormat.GenericTypographic)
        {
            FormatFlags = StringFormatFlags.NoClip |
                          StringFormatFlags.LineLimit |
                          //StringFormatFlags.FitBlackBox |
                          StringFormatFlags.MeasureTrailingSpaces |
                          StringFormatFlags.DisplayFormatControl,
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.None
        });

    public static StringFormat StringFormat => s_stringFormat.Value;
#endif

    public static readonly GraphicsStringFormat GraphicsStringFormat = new()
    {
        FormatFlags = GraphicsStringFormatFlags.NoClip |
                      GraphicsStringFormatFlags.LineLimit |
                      GraphicsStringFormatFlags.MeasureTrailingSpaces |
                      GraphicsStringFormatFlags.DisplayFormatControl,
        Alignment = GraphicsTextAlignment.Near,
        LineAlignment = GraphicsTextAlignment.Near,
        Trimming = GraphicsStringTrimming.None
    };

#if WINDOWS
    public static readonly TextRenderingHint TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
#endif

    public static readonly GraphicsTextRenderingMode GraphicsTextRenderingMode =
        GraphicsTextRenderingMode.ClearTypeGridFit;

    private ContentSettings? _contentSettings;
    private string? _document;
    private Encoding? _encoding = Encoding.Default;

    /// <summary>
    ///     Calculated page size. Set by Sheet view model.
    /// </summary>
    public SizeF PageSize { get; set; }

    /// <summary>
    ///     Optional graphics context used to measure text during <see cref="RenderAsync" /> (reflow).
    ///     When null, <see cref="RenderAsync" /> creates a platform-default context (System.Drawing on
    ///     Windows). Tests inject a platform-neutral context (e.g. a recording/fake) so that rendering
    ///     can be exercised and verified cross-platform.
    /// </summary>
    [JsonIgnore]
    public IGraphicsContext? MeasurementContext { get; set; }

    /// <summary>
    ///     ContentType identifier (shorthand for class name).
    /// </summary>
    public virtual string[] SupportedContentTypes => s_supportedContentTypes;

    /// <summary>
    ///     Holds content settings for the CTE. These are used as defaults when a Sheet does not
    ///     specify any.
    /// </summary>
    public ContentSettings? ContentSettings
    {
        get => _contentSettings;
        set => SetField(ref _contentSettings, value);
    }

    /// <summary>
    ///     The contents of the file to be printed.
    /// </summary>
    [JsonIgnore]
    public string? Document
    {
        get => _document;
        set =>
            //LogService.TraceMessage($"Document is {document.Length} chars.");
            SetField(ref _document, value);
    }

    /// <summary>
    ///     Path of the source file being rendered, when known. Used to resolve document-relative
    ///     references (e.g. local images in Markdown). May be empty/null for string-loaded content.
    /// </summary>
    [JsonIgnore]
    public string? SourceFileName { get; set; }

    /// <summary>
    ///     The contents encoding of the file to be printed.
    /// </summary>
    [JsonIgnore]
    public Encoding? Encoding
    {
        get => _encoding;
        set => SetField(ref _encoding, value);
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected new bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        OnSettingsChanged(true);
        return true;
    }

    // if bool is true, reflow. Otherwise, just paint
    public event EventHandler<bool>? SettingsChanged;

    protected void OnSettingsChanged(bool reflow)
    {
        SettingsChanged?.Invoke(this, reflow);
    }

    /// <summary>
    ///     All concrete content-type engines shipped in this assembly (see
    ///     <see cref="ContentTypeEngineRegistry" />).
    /// </summary>
    public static IReadOnlyList<ContentTypeEngineBase> GetDerivedClassesCollection()
    {
        return ContentTypeEngineRegistry.CreateAll();
    }

    /// <summary>
    ///     Resolves the <see cref="IGraphicsContext" /> used to measure text during reflow. Returns the
    ///     injected <see cref="MeasurementContext" /> when set; otherwise creates a platform-default
    ///     context (System.Drawing on Windows). When a context is created here, <paramref name="owner" />
    ///     receives the object that must be disposed once reflow completes (null when the caller-supplied
    ///     context is returned).
    /// </summary>
    protected IGraphicsContext ResolveMeasurementContext(int dpiX, int dpiY, out IDisposable? owner)
    {
        if (MeasurementContext is not null)
        {
            owner = null;
            return MeasurementContext;
        }

#if WINDOWS
        var windowsContext = new WindowsMeasurementContext(dpiX, dpiY);
        owner = windowsContext;
        return windowsContext.Context;
#else
        owner = null;
        throw new InvalidOperationException(
            $"{GetType().Name}.RenderAsync requires a MeasurementContext to be set on non-Windows platforms.");
#endif
    }

    /// <summary>
    ///     Get total count of pages. Set any local page-size related values (e.g. linesPerPage).
    /// </summary>
    /// <param name="e"></param>
    /// <param name="printerResolution"></param>
    /// <param name="reflowProgress"></param>
    /// <returns>Number of sheets.</returns>
    public virtual async Task<int> RenderAsync(PrintResolution? printerResolution,
        EventHandler<string>? reflowProgress)
    {
        if (Document == null)
        {
            throw new InvalidOperationException("Document can't be null for Render");
        }

        return await Task.FromResult(0);
    }

    /// <summary>
    ///     Paints a single page
    /// </summary>
    /// <param name="g">Graphics context with 0,0 being the origin of the Page</param>
    /// <param name="pageNum">Page number to print</param>
    public abstract void PaintPage(IGraphicsContext g, int pageNum);

    /// <summary>
    ///     Creates the appropriate Content Type Engine instance given a content type string.
    /// </summary>
    /// <param name="contentType"></param>
    /// <returns>ContentEngine, ContentType, Language</returns>
    public static (ContentTypeEngineBase? cte, string languageId, string language) CreateContentTypeEngine(
        string? contentType)
    {
        LogService.TraceMessage();

        contentType = string.IsNullOrEmpty(contentType)
            ? ModelLocator.Current.Settings.DefaultContentType
            : contentType;
        Debug.Assert(ModelLocator.Current.FileTypeMapping != null);
        Debug.Assert(ModelLocator.Current.FileTypeMapping.ContentTypes != null);

        // If contentType matches one of our CTE Names, this will succeed.
        ContentTypeEngineBase? cte = GetDerivedClassesCollection()
            .FirstOrDefault(c => contentType.Equals(c.GetType().Name, StringComparison.OrdinalIgnoreCase));
        string language = string.Empty;
        string languageId = string.Empty;

        if (cte != null)
        {
            languageId = cte.SupportedContentTypes[0];
            language = ModelLocator.Current.FileTypeMapping.ContentTypes.FirstOrDefault(lang =>
                lang.Id.Equals(languageId, StringComparison.OrdinalIgnoreCase))?.Title ?? languageId;
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

        ContentType? extension = ModelLocator.Current.FileTypeMapping.ContentTypes
            .FirstOrDefault(l => l.Extensions.Any(i =>
                CultureInfo.CurrentCulture.CompareInfo.Compare(i, contentType, CompareOptions.IgnoreCase) == 0));
        if (extension != null && !string.IsNullOrEmpty(extension.Id))
        {
            // Is Id directly supported by a Cte?
            cte = GetDerivedClassesCollection().FirstOrDefault(c => c.SupportedContentTypes.Contains(extension.Id));
            if (cte != null)
            {
                return (cte, extension.Id, extension.Title);
            }

            // It is a language. Needs to be Syntax Highlighted. Use the default Syntax Highlighter CTE
            languageId = extension.Id;
            language = extension.Title;
        }
        else
        {
            // Is it a content type (Landuage.Id)? (text/ansi)
            ContentType? lang = ModelLocator.Current.FileTypeMapping.ContentTypes.FirstOrDefault(l =>
                l.Id.Equals(contentType, StringComparison.OrdinalIgnoreCase));
            if (lang != null)
            {
                languageId = lang.Id;
                language = lang.Title;
            }

            // Is it a language Title?
            lang = ModelLocator.Current.FileTypeMapping.ContentTypes.FirstOrDefault(l =>
                l.Title.Equals(contentType, StringComparison.OrdinalIgnoreCase));
            if (lang != null)
            {
                languageId = lang.Id;
                language = lang.Title;
            }

            // Is it a language name found in a Language alias? (ansi)
            lang = ModelLocator.Current.FileTypeMapping.ContentTypes
                .FirstOrDefault(l => l.Aliases.Any(i => CultureInfo.CurrentCulture.CompareInfo.Compare(i,
                    contentType,
                    CompareOptions.IgnoreCase) == 0));
            if (lang != null)
            {
                languageId = lang.Id;
                language = lang.Title;
            }

            if (!string.IsNullOrEmpty(language) && !string.IsNullOrEmpty(languageId))
            {
                // Is the Id supported directly (e.g. text/html is supported directly by HtmlCte) 
                // If supported by multiple, pick the default.
                string id = languageId;
                IEnumerable<ContentTypeEngineBase> ctes = GetDerivedClassesCollection()
                    .Where(c => c.SupportedContentTypes.Contains(id.ToLower()));
                ContentTypeEngineBase[] contentTypeEngineBases = ctes as ContentTypeEngineBase[] ?? [.. ctes];
                cte = contentTypeEngineBases.Count() > 1
                    ? contentTypeEngineBases.First(c =>
                        c.GetType().Name == ModelLocator.Current.Settings.DefaultCteClassName)
                    : contentTypeEngineBases.FirstOrDefault();

                if (cte != null)
                {
                    return (cte, languageId,
                        ModelLocator.Current.FileTypeMapping.ContentTypes.FirstOrDefault(l =>
                            l.Id.Equals(languageId, StringComparison.OrdinalIgnoreCase))!.Title);
                }

                // It is a language. Needs to be Syntax Highlighted. Use the default Syntax Highlighter CTE
                //languageId = lang.Id;
                //language = lang.Title;
            }
        }

        if (string.IsNullOrEmpty(languageId))
        {
            // Didn't find a content type so use default CTE
            cte = GetDerivedClassesCollection().FirstOrDefault(c =>
                c.SupportedContentTypes.Contains(ModelLocator.Current.Settings.DefaultContentType));
            languageId = cte?.SupportedContentTypes[0] ?? ModelLocator.Current.Settings.DefaultContentType;
            language = ModelLocator.Current.FileTypeMapping.ContentTypes
                .FirstOrDefault(l => l.Id.Equals(languageId, StringComparison.OrdinalIgnoreCase))
                ?.Title ?? languageId;
        }
        else
        {
            // It is a language. Needs to be Syntax Highlighted. Use the default Syntax Highlighter CTE
            cte = GetDerivedClassesCollection().FirstOrDefault(c =>
                c.GetType().Name.Equals(ModelLocator.Current.Settings.DefaultSyntaxHighlighterCteNameClassName,
                    StringComparison.OrdinalIgnoreCase));
            //if (string.IsNullOrWhiteSpace(language)) {
            //    language = contentType;
            //}
        }

        return (cte, languageId, language);
    }

    /// <summary>
    ///     Returns the content type name (e.g. "text/plain") given a file path. If the content type
    ///     cannot be determined from FilesAssociations the default of "text/plain" is returned.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>The content type</returns>
    public static string GetContentType(string filePath)
    {
        string contentType = ModelLocator.Current.Settings.DefaultContentType;

        if (string.IsNullOrEmpty(filePath))
        {
            return contentType;
        }

        // Expand path
        filePath = Path.GetFullPath(filePath);

        // If there's a file extension get the content type from the file type association mapper
        string ext = Path.GetExtension(filePath).ToLower();
        if (ext != string.Empty)
        {
            // BUGBUG: This assumes all extensions in FilesAssociations are lowercase
            if (ModelLocator.Current.FileTypeMapping.FilesAssociations.TryGetValue("*" + ext, out string? ct))
            {
                // Now find Id in Languages
                contentType = ModelLocator.Current.FileTypeMapping.ContentTypes
                    .Where(lang => lang.Id.Equals(ct, StringComparison.OrdinalIgnoreCase))
                    .DefaultIfEmpty(new ContentType { Id = ModelLocator.Current.Settings.DefaultContentType })
                    .First().Id;
            }
            else
            {
                // No direct file extension, look in Languages
                contentType = ModelLocator.Current.FileTypeMapping.ContentTypes
                    .Where(lang => lang.Extensions
                        .Count(i => CultureInfo.CurrentCulture.CompareInfo.Compare(i, "*" + ext,
                                        CompareOptions.IgnoreCase) ==
                                    0 ||
                                    CultureInfo.CurrentCulture.CompareInfo.Compare(i, ext,
                                        CompareOptions.IgnoreCase) ==
                                    0) > 0)
                    .DefaultIfEmpty(new ContentType { Id = ModelLocator.Current.Settings.DefaultContentType })
                    .First().Id;
            }
        }
        else
        {
            // Empty means no extension (e.g. .\.ssh\config) - use filename
            if (ModelLocator.Current.FileTypeMapping.FilesAssociations.TryGetValue("*" + Path.GetFileName(filePath),
                    out string? ct))
            {
                contentType = ct;
            }
            else
            {
                // No direct file extension, look in Languages
                contentType = ModelLocator.Current.FileTypeMapping.ContentTypes
                    .Where(lang => lang.Extensions.Count(i => CultureInfo.CurrentCulture.CompareInfo.Compare(i,
                        Path.GetFileName(filePath),
                        CompareOptions.IgnoreCase) == 0) > 0)
                    .DefaultIfEmpty(new ContentType { Id = ModelLocator.Current.Settings.DefaultContentType })
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

    public override void CopyPropertiesFrom(ModelBase? source)
    {
        if (source is not ContentTypeEngineBase src)
        {
            return;
        }

        PageSize = src.PageSize;
        MeasurementContext = src.MeasurementContext;
        Document = src.Document;
        SourceFileName = src.SourceFileName;
        Encoding = src.Encoding;

        if (src.ContentSettings is null)
        {
            ContentSettings = null;
        }
        else
        {
            ContentSettings ??= new ContentSettings();
            ContentSettings.CopyPropertiesFrom(src.ContentSettings);
        }
    }
}
