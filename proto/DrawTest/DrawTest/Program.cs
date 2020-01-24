using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace DrawTest
{
    class Program
    {
        const string fontFamily = "Source Code Pro";
        const float fontSize = 10;
        const FontStyle fontStyle = FontStyle.Regular;

        [DllImport("libgdiplus", ExactSpelling = true)]
        internal static extern string GetLibgdiplusVersion();

        static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Console.WriteLine($"Using the real GDI+");
            else
                Console.WriteLine($"Using libgdiplus version: {GetLibgdiplusVersion()}");

            int dpi = 96; // Typical screen dpi
            Console.WriteLine($"------ Test @ {dpi} dpi ------");
            DoTest(dpi, GraphicsUnit.Pixel);
            //DoTest(dpi, GraphicsUnit.Display);

            //dpi = 100; // low resolution printer dpi
            //Console.WriteLine($"------ Test @ {dpi} dpi ------");
            //DoTest(dpi, GraphicsUnit.Pixel);
            ////DoTest(dpi, GraphicsUnit.Display);

            dpi = 600; // high res printer dpi
            Console.WriteLine($"------ Test @ {dpi} dpi ------");
            DoTest(dpi, GraphicsUnit.Pixel);
            //DoTest(dpi, GraphicsUnit.Display);
        }

        private static void DoTest(int res, GraphicsUnit unit)
        {
            using Bitmap bitmap = new Bitmap(1, 1);
            bitmap.SetResolution(res, res);
            var g = Graphics.FromImage(bitmap);
            g.PageUnit = unit;

            Console.WriteLine($"Graphics: PageUnit = {g.PageUnit}, PageScale = {g.PageScale}, DPI = {g.DpiX} x {g.DpiY}");

            // Calculate the number of lines per page.
            var font = new System.Drawing.Font(fontFamily, fontSize, fontStyle, GraphicsUnit.Point);
            //var font = new System.Drawing.Font(fontFamily, fontSize / 72F * 96F, fontStyle, GraphicsUnit.Pixel);
            Console.WriteLine($"Font: {font.Name}, {font.Size} in {font.Unit}s ({font.SizeInPoints}pts), {font.Style}.");

            var fi = new FontInfo(g, font);
            fi.Dump();
            
            // Font.GetHeight() assumes 96dpi
            Console.WriteLine($"  Height: GetHeight() = {font.GetHeight()}");

            // Font.GetHeight(res) should use res (and does on both Windows and libgdiplus 6.1)
            float fExpectedHeight = font.GetHeight(res);
            Console.WriteLine($"          GetHeight({res}) = {fExpectedHeight}");

            // Font.GetHeight(Graphics) honors Graphics.DpiY on Windows, but with libgdiplus 6.1 uses 96
            Console.WriteLine($"          GetHeight(g) = {font.GetHeight(g)}" + (fExpectedHeight == font.GetHeight(g) ? "" : " --- FAIL!"));

            var lineSize = g.MeasureString(font.Name, font);
            Console.WriteLine($"Size of \"{font.Name}\": {lineSize.Width} x {lineSize.Height}");

            // Set character ranges to "First" and "Second".
            CharacterRange[] characterRanges = { new CharacterRange(0, 6) };

            // Set string format.
            StringFormat stringFormat = new StringFormat();
            stringFormat.SetMeasurableCharacterRanges(characterRanges);

            Region[] rg = g.MeasureCharacterRanges(font.Name, font, new RectangleF(0, 0, 1000, 1000), stringFormat);
            Console.WriteLine($"rg.Size = {rg.Length}");
            for (int i = 0; i < rg.Length;i++)
            {
                Console.WriteLine($"{font.Name.Substring(0, 6)} - {rg[i].GetBounds(g).Width} x {rg[i].GetBounds(g).Height}");
            }

        }
    }

    public class FontInfo 
    {
        // Heights and positions in pixels.
        public float EmHeightPixels;
        public float AscentPixels;
        public float DescentPixels;
        public float CellHeightPixels;
        public float InternalLeadingPixels;
        public float LineSpacingPixels;
        public float ExternalLeadingPixels;

        // Distances from the top of the cell in pixels.
        public float RelTop;
        public float RelBaseline;
        public float RelBottom;

        public void Dump()
        {
            Console.WriteLine($"        EmHeightPixels = {EmHeightPixels}");
            Console.WriteLine($"         AscentPixels = {AscentPixels}");
            Console.WriteLine($"        DescentPixels = {DescentPixels}");
            Console.WriteLine($"     CellHeightPixels = {CellHeightPixels}");
            Console.WriteLine($"InternalLeadingPixels = {InternalLeadingPixels}");
            Console.WriteLine($"    LineSpacingPixels = {LineSpacingPixels}");
            Console.WriteLine($"ExternalLeadingPixels = {ExternalLeadingPixels}");
            Console.WriteLine($"               RelTop = {RelTop}");
            Console.WriteLine($"          RelBaseline = {RelBaseline}");
            Console.WriteLine($"            RelBottom = {RelBottom}");
        }

        // Initialize the properties.
        public FontInfo(Graphics gr, Font the_font)
        {
            float em_height = the_font.FontFamily.GetEmHeight(the_font.Style);
            EmHeightPixels = ConvertUnits(gr, the_font.Size, the_font.Unit, GraphicsUnit.Pixel);
            float design_to_pixels = EmHeightPixels / em_height;

            AscentPixels = design_to_pixels * the_font.FontFamily.GetCellAscent(the_font.Style);
            DescentPixels = design_to_pixels * the_font.FontFamily.GetCellDescent(the_font.Style);
            CellHeightPixels = AscentPixels + DescentPixels;
            InternalLeadingPixels = CellHeightPixels - EmHeightPixels;
            LineSpacingPixels = design_to_pixels * the_font.FontFamily.GetLineSpacing(the_font.Style);
            ExternalLeadingPixels = LineSpacingPixels - CellHeightPixels;

            RelTop = InternalLeadingPixels;
            RelBaseline = AscentPixels;
            RelBottom = CellHeightPixels;
        }

        // Convert from one type of unit to another.
        // I don't know how to do Display or World.
        private float ConvertUnits(Graphics gr, float value, GraphicsUnit from_unit, GraphicsUnit to_unit)
        {
            if (from_unit == to_unit) return value;

            // Convert to pixels. 
            switch (from_unit)
            {
                case GraphicsUnit.Document:
                    value *= gr.DpiX / 300;
                    break;
                case GraphicsUnit.Inch:
                    value *= gr.DpiX;
                    break;
                case GraphicsUnit.Millimeter:
                    value *= gr.DpiX / 25.4F;
                    break;
                case GraphicsUnit.Pixel:
                    // Do nothing.
                    break;
                case GraphicsUnit.Point:
                    value *= gr.DpiX / 72;
                    break;
                default:
                    throw new Exception("Unknown input unit " + from_unit.ToString() + " in FontInfo.ConvertUnits");
            }

            // Convert from pixels to the new units. 
            switch (to_unit)
            {
                case GraphicsUnit.Document:
                    value /= gr.DpiX / 300;
                    break;
                case GraphicsUnit.Inch:
                    value /= gr.DpiX;
                    break;
                case GraphicsUnit.Millimeter:
                    value /= gr.DpiX / 25.4F;
                    break;
                case GraphicsUnit.Pixel:
                    // Do nothing.
                    break;
                case GraphicsUnit.Point:
                    value /= gr.DpiX / 72;
                    break;
                default:
                    throw new Exception("Unknown output unit " + to_unit.ToString() + " in FontInfo.ConvertUnits");
            }

            return value;
        }
    }

}
