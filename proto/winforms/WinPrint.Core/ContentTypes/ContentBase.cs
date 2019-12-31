using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WinPrint.Core.Models;

namespace WinPrint.Core.ContentTypes {
    /// <summary>
    /// base class for Content/File types
    /// </summary>
    public abstract class ContentBase : ModelBase, INotifyPropertyChanged {
        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected new bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            OnSettingsChanged(true);
            return true;
        }

        // if bool is true, reflow. Otherwise just paint
        public event EventHandler<bool> SettingsChanged;
        protected void OnSettingsChanged(bool reflow) => SettingsChanged?.Invoke(this, reflow);

        /// <summary>
        /// ContentType identifier (shorthand for class name). 
        /// </summary>
        public static string ContentType = "base";

        /// <summary>
        /// Calculated page size. Set by Sheet view model.
        /// </summary>
        public SizeF PageSize;

        /// <summary>
        /// Default content font for this content type
        /// </summary>
        private Core.Models.Font font;
        public Core.Models.Font Font { get => font; 
            set => SetField(ref font, value); }


        internal int numPages = 0;
        public int NumPages {
            get => numPages;
            set => SetField(ref numPages, value);
        }

        internal string filePath = null;
        internal string document = null;

        public async virtual Task<string> LoadAsync(string filePath) {
            using StreamReader streamToPrint = new StreamReader(filePath);
            document = await streamToPrint.ReadToEndAsync();
            this.filePath = filePath;
            return document;
        } 

        /// <summary>
        /// Get total count of pages. Set any local page-size related values (e.g. linesPerPage).
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public async virtual Task<int> RenderAsync(System.Drawing.Printing.PrinterResolution printerResolution) {
            if (document == null) throw new ArgumentNullException("document can't be null for Render");
            NumPages = 0;
            return NumPages;
        }

        /// <summary>
        /// Paints a single page
        /// </summary>
        /// <param name="g">Graphics with 0,0 being the origin of the Page</param>
        /// <param name="pageNum">Page number to print</param>
        public abstract void PaintPage(Graphics g, int pageNum);

    }
}
