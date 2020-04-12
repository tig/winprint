using System;

namespace LiteHtmlSharp
{
    public struct LiteHtmlSize
    {
        public double Width;
        public double Height;
        public LiteHtmlSize(double width, double height)
        {
            Width = width;
            Height = height;
        }
    }

    public struct LiteHtmlPoint
    {
        public double X;
        public double Y;
        public LiteHtmlPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public struct LiteHtmlRect
    {
        public LiteHtmlPoint Point;
        public LiteHtmlSize Size;
    }


    public abstract class ViewportContainer : Container
    {
        public LiteHtmlSize Size;

        public LiteHtmlPoint ScrollOffset;

        public event Action<LiteHtmlSize> DocumentSizeKnown;

        LiteHtmlSize _desiredSize;
        LiteHtmlPoint _desiredScrollOffset;
        bool _hasCustomViewport;
        public bool HasCustomViewport => _hasCustomViewport;


        public ViewportContainer(string masterCssData, ILibInterop libInterop) : base(masterCssData, libInterop)
        {
        }

        public void ResetViewport()
        {
            Document.HasLoadedHtml = false;
            Document.HasRendered = false;
            Size = default(LiteHtmlSize);
            ScrollOffset = default(LiteHtmlPoint);
            _desiredSize = default(LiteHtmlSize);
            _desiredScrollOffset = default(LiteHtmlPoint);
            _hasCustomViewport = false;
        }

        public void Draw()
        {
            Document.Draw((int)-ScrollOffset.X, (int)-ScrollOffset.Y, new position
            {
                x = 0,
                y = 0,
                width = (int)Size.Width,
                height = (int)Size.Height
            });
        }

        public void Render()
        {
            Document.Render((int)Size.Width);
            DocumentSizeKnown?.Invoke(new LiteHtmlSize(Document.Width(), Document.Height()));
        }

        protected override void GetClientRect(ref position client)
        {
            client.width = (int)Size.Width;
            client.height = (int)Size.Height;
        }

        protected override void GetMediaFeatures(ref media_features media)
        {
            media.width = media.device_width = (int)Size.Width;
            media.height = media.device_height = (int)Size.Height;
        }


        // If true then a redraw is needed
        public bool CheckViewportChange(bool forceRender = false)
        {
            if (forceRender
                || (int)Size.Width != (int)_desiredSize.Width
                || (int)Size.Height != (int)_desiredSize.Height)
            {
                Size = _desiredSize;
                ScrollOffset = _desiredScrollOffset;
                Document.OnMediaChanged();
                Render();
                return true;
            }

            if ((int)ScrollOffset.Y != (int)_desiredScrollOffset.Y || (int)ScrollOffset.X != (int)_desiredScrollOffset.X)
            {
                ScrollOffset = _desiredScrollOffset;
                return true;
            }

            return false;
        }


        // custom viewport is used for offsetting/scrolling the canvas on this view
        public bool SetViewport(LiteHtmlPoint scrollOffset, LiteHtmlSize size)
        {
            _hasCustomViewport = true;
            _desiredScrollOffset = scrollOffset;
            _desiredSize = size;

            if (!Document.HasLoadedHtml)
            {
                return false;
            }

            return CheckViewportChange();
        }

    }
}