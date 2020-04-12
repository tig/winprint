using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Globalization;
using Serilog;

#if AUTO_UTF8
using Utf8Str = System.String;
#else
using Utf8Str = System.IntPtr;
#endif

namespace LiteHtmlSharp
{

    public delegate bool ShouldCreateElementDelegate(string tag);
    public delegate int CreateElementDelegate(string tag, string attributes, ElementInfo elementInfo);
    public delegate string ImportCssDelegate(string url, string baseurl);

    public abstract class Container
    {
        int _testNumber = 0;
        string _testText = null;

        // By holding this _callbacks field we keep the ref returned through InitCallbacks 
        // alive for lifetime of this Container
        private Callbacks _callbacks;
        public Document Document;

        public int ScaleFactor = 1;

        public ShouldCreateElementDelegate ShouldCreateElementCallback;
        public CreateElementDelegate CreateElementCallback;

        public ImportCssDelegate ImportCssRequest;

        public event Action<string> AnchorClicked;

        ILibInterop _libInterop;

        public Container(string masterCssData, ILibInterop libInterop)
        {
            _libInterop = libInterop;

            Document = new Document();
            libInterop.InitDocument(ref Document.Calls, InitCallbacks);
            Document.SetMasterCSS(masterCssData);

#if !FDEBUG
            TestFramework();
#endif
        }

        public virtual void Render(string html) { }

        private void InitCallbacks(ref Callbacks callbacks)
        {
            callbacks.DrawBorders = DrawBordersScaled;
            callbacks.DrawBackground = DrawBackgroundScaled;
            callbacks.GetImageSize = GetImageSizeCallback;

            callbacks.DrawText = DrawTextScaled;
            callbacks.GetTextWidth = GetTextWidthCallback;
            callbacks.CreateFont = CreateFontWrapper;

            callbacks.ImportCss = ImportCssCallback;

            callbacks.GetClientRect = GetClientRect;
            callbacks.GetMediaFeatures = GetMediaFeatures;

            callbacks.SetBaseURL = SetBaseURLCallback;
            callbacks.OnAnchorClick = OnAnchorClickHandler;

            callbacks.PTtoPX = PTtoPX;
            callbacks.ShouldCreateElement = ShouldCreateElement;
            callbacks.CreateElement = CreateElementWrapper;

            callbacks.SetCursor = SetCursorCallback;
            callbacks.DrawListMarker = DrawListMarkerCallback;

            callbacks.TransformText = TransformTextCallback;
            callbacks.TestCallback = TestCallback;

            callbacks.SetCaption = SetCaptionCallback;
            callbacks.GetDefaultFontName = GetDefaultFontNameWrapper;
            callbacks.GetDefaultFontSize = GetDefaultFontSize;

            // By holding this _callbacks field we keep the ref returned through InitCallbacks 
            // alive for lifetime of this Container
            _callbacks = callbacks;
        }

        void TestCallback(int number, Utf8Str text)
        {
            _testText = Utf8Util.Utf8PtrToString(text);
            _testNumber = number;
        }

        void TestFramework()
        {
            string testStringResult = "Test 1234 ....  \U0001D11E 𝄞 𩸽, ₤ · ₥ · ₦ · ₮ · ₯ · ₹";
            var input = Utf8Util.StringToHGlobalUTF8(testStringResult);

            var echoTest = _libInterop.LibEchoTest(input);

            var echoResult = Utf8Util.Utf8PtrToString(echoTest);


            if (testStringResult != echoResult)
            {
                throw new Exception("Utf8 string corrupted through boundary!");
            }

            Document.TriggerTestCallback(50, testStringResult);
            if (_testText != testStringResult || _testNumber != 50)
            {
                throw new Exception("Container instance callback test failed. Something is broken!");
            }
        }

        // -----

        private void DrawBackgroundScaled(UIntPtr hdc, Utf8Str image, background_repeat repeat, ref web_color color, ref position pos, ref border_radiuses borderRadiuses, ref position borderBox, bool isRoot)
        {
            pos.Scale(ScaleFactor);
            borderRadiuses.Scale(ScaleFactor);
            borderBox.Scale(ScaleFactor);
            DrawBackground(hdc, Utf8Util.Utf8PtrToString(image), repeat, ref color, ref pos, ref borderRadiuses, ref borderBox, isRoot);
        }

        protected abstract void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos, ref border_radiuses borderRadiuses, ref position borderBox, bool isRoot);

        // -----

        private void DrawBordersScaled(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root)
        {
            borders.Scale(ScaleFactor);
            draw_pos.Scale(ScaleFactor);
            DrawBorders(hdc, ref borders, ref draw_pos, root);
        }

        protected abstract void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root);

        // -----

        private void DrawTextScaled(Utf8Str text, UIntPtr font, ref web_color color, ref position pos)
        {
            pos.Scale(ScaleFactor);
            DrawText(Utf8Util.Utf8PtrToString(text), font, ref color, ref pos);
        }

        protected abstract void DrawText(string text, UIntPtr font, ref web_color color, ref position pos);

        // -----

        private void GetImageSizeCallback(Utf8Str image, ref size size)
        {
            GetImageSize(Utf8Util.Utf8PtrToString(image), ref size);
        }

        protected abstract void GetImageSize(string image, ref size size);

        private int GetTextWidthCallback(Utf8Str text, UIntPtr font)
        {
            return GetTextWidth(Utf8Util.Utf8PtrToString(text), font);
        }

        protected abstract int GetTextWidth(string text, UIntPtr font);

        protected abstract void GetClientRect(ref position client);

        private void SetCaptionCallback(Utf8Str caption)
        {
            SetCaption(Utf8Util.Utf8PtrToString(caption));
        }

        protected abstract void SetCaption(string caption);

        protected abstract int GetDefaultFontSize();

        private Utf8Str GetDefaultFontNameWrapper()
        {
            return Utf8Util.StringToHGlobalUTF8(GetDefaultFontName());
        }

        protected abstract string GetDefaultFontName();

        protected UIntPtr CreateFontWrapper(Utf8Str faceName, int size, int weight, font_style italic, uint decoration, ref font_metrics fm)
        {
            return CreateFont(Utf8Util.Utf8PtrToString(faceName), size, weight, italic, (font_decoration)decoration, ref fm);
        }

        protected abstract UIntPtr CreateFont(string faceName, int size, int weight, font_style italic, font_decoration decoration, ref font_metrics fm);

        private Utf8Str ImportCssCallback(Utf8Str url, Utf8Str baseurl)
        {
            return Utf8Util.StringToHGlobalUTF8(ImportCss(Utf8Util.Utf8PtrToString(url), Utf8Util.Utf8PtrToString(baseurl)));
        }

        protected virtual string ImportCss(string url, string baseurl)
        {
            if (ImportCssRequest == null)
            {
                throw new Exception("ImportCss must be overridden or the ImportCssRequest delegate set");
            }
            return ImportCssRequest(url, baseurl);
        }

        protected abstract void GetMediaFeatures(ref media_features media);

        void SetBaseURLCallback(Utf8Str base_url)
        {
            SetBaseURL(Utf8Util.Utf8PtrToString(base_url));
        }

        protected abstract void SetBaseURL(string base_url);

        protected void OnAnchorClickHandler(Utf8Str url)
        {
            if (AnchorClicked != null)
            {
                AnchorClicked(Utf8Util.Utf8PtrToString(url));
            }
        }

        // Used when the parent has a custom tag (View) that works with an href attribute
        public void TriggerAnchorClicked(string url)
        {
            AnchorClicked?.Invoke(url);
        }

        protected abstract int PTtoPX(int pt);

        private bool ShouldCreateElement(Utf8Str tag)
        {
            if (ShouldCreateElementCallback != null)
            {
                return ShouldCreateElementCallback(Utf8Util.Utf8PtrToString(tag));
            }
            return false;
        }

        private int CreateElementWrapper(Utf8Str tag, Utf8Str attributes, [Out, In] ref ElementInfoStruct elementInfo)
        {
            if (CreateElementCallback != null)
            {
                return CreateElementCallback(Utf8Util.Utf8PtrToString(tag), Utf8Util.Utf8PtrToString(attributes), new ElementInfo(elementInfo));
            }
            else
            {
                return 0;
            }
        }

        private void SetCursorCallback(Utf8Str cursor)
        {
            SetCursor(Utf8Util.Utf8PtrToString(cursor));
        }

        protected abstract void SetCursor(string cursor);

        private void DrawListMarkerCallback(Utf8Str image, Utf8Str baseURL, list_style_type marker_type, ref web_color color, ref position pos)
        {
            DrawListMarker(Utf8Util.Utf8PtrToString(image), Utf8Util.Utf8PtrToString(baseURL), marker_type, ref color, ref pos);
        }

        protected abstract void DrawListMarker(string image, string baseURL, list_style_type marker_type, ref web_color color, ref position pos);

        private Utf8Str TransformTextCallback(Utf8Str text, text_transform t)
        {
            return Utf8Util.StringToHGlobalUTF8(TransformText(Utf8Util.Utf8PtrToString(text), t));
        }

        protected virtual string TransformText(string text, text_transform t)
        {
            switch (t)
            {
                case text_transform.text_transform_capitalize:
                    return System.Text.RegularExpressions.Regex.Replace(text, @"(^\w)|(\s\w)", m => m.Value.ToUpper());
                case text_transform.text_transform_lowercase:
                    return text.ToLower();
                case text_transform.text_transform_uppercase:
                    return text.ToUpper();
                default:
                    return text;
            }
        }
    }
}
