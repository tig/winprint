using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace LiteHtmlSharp
{
    public class Document
    {
        public DocumentCalls Calls = new DocumentCalls();
        public IntPtr Container;

        public event Action ViewElementsNeedLayout;


        public bool HasLoadedHtml { get; set; }

        public bool HasRendered { get; set; }

        public void SetMasterCSS(string css)
        {
            var cssStr = Utf8Util.StringToHGlobalUTF8(css);
            try
            {
                Calls.SetMasterCSS(Calls.ID, cssStr);
            }
            finally
            {
#if !AUTO_UTF8
                Marshal.FreeHGlobal(cssStr);
#endif
            }
        }

        public void CreateFromString(string html)
        {
            if (html == null)
            {
                throw new Exception("Cannot render a null string.");
            }
            else
            {
                var htmlStr = Utf8Util.StringToHGlobalUTF8(html);
                try
                {
                    Calls.CreateFromString(Calls.ID, htmlStr);
                }
                finally
                {
#if !AUTO_UTF8
                    Marshal.FreeHGlobal(htmlStr);
#endif
                }
                HasLoadedHtml = true;
            }
        }

        public virtual void Draw(int x, int y, position clip)
        {
            Calls.Draw(Calls.ID, x, y, clip);

            if (ViewElementsNeedLayout != null)
            {
                ViewElementsNeedLayout();
            }
        }

        public bool OnMouseMove(int x, int y)
        {
            return Calls.OnMouseMove(Calls.ID, x, y);
        }

        public bool OnMouseLeave()
        {
            return Calls.OnMouseLeave(Calls.ID);
        }

        public int Render(int maxWidth)
        {
            HasRendered = true;
            return Calls.Render(Calls.ID, maxWidth);
        }

        public void OnMediaChanged()
        {
            Calls.OnMediaChanged(Calls.ID);
        }

        public bool OnLeftButtonDown(int x, int y)
        {
            return Calls.OnLeftButtonDown(Calls.ID, x, y);
        }

        public bool OnLeftButtonUp(int x, int y)
        {
            return Calls.OnLeftButtonUp(Calls.ID, x, y);
        }

        public ElementInfo GetElementInfo(int ID)
        {
            IntPtr ptr = Calls.GetElementInfo(Calls.ID, ID);
            if (ptr == IntPtr.Zero)
            {
                return null;
            }
            ElementInfoStruct info = (ElementInfoStruct)Marshal.PtrToStructure(ptr, typeof(ElementInfoStruct));
            var el = new ElementInfo(info);
            return el;
        }

        public void TriggerTestCallback(int number, string text)
        {
            Calls.TriggerTestCallback(Calls.ID, number, Utf8Util.StringToHGlobalUTF8(text));
        }

        public int Height()
        {
            return Calls.GetHeight(Calls.ID);
        }

        public int Width()
        {
            return Calls.GetWidth(Calls.ID);
        }

    }
}
