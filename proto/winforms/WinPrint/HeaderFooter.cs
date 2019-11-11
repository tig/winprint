using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace WinPrint {
    public abstract class HeaderFooter {
        private float height;

        public float GetHeight() {
            return font.GetHeight(100);
        }

        public void SetHeight(int value) {
            height = value;
        }

        public string Text { get => text; set => text = value; }
        public Font Font { get => font; set => font = value; }

        private Font font;

        private string text;

        internal Page containingPage;

        public abstract void Paint(Graphics g);

        public HeaderFooter(Page containingPage) {
            this.containingPage = containingPage;
            Font = new Font("Lucida Sans", 10, FontStyle.Italic, GraphicsUnit.Point);
        }
    }

    public class Header : HeaderFooter {
        public Header(Page containingPage) : base(containingPage) {
        }

        public override void Paint(Graphics g) {
            g.DrawString(Text, Font, Brushes.Black, containingPage.Margins.Left, containingPage.Margins.Top, new StringFormat());
        }
    }
    public class Footer : HeaderFooter {
        public Footer(Page containingPage) : base(containingPage) {
        }

        public override void Paint(Graphics g) {
            g.DrawString(Text, Font, Brushes.Black, containingPage.Margins.Left, containingPage.Bounds.Bottom - containingPage.Margins.Bottom - (int)GetHeight(), new StringFormat());
        }
    }
}
