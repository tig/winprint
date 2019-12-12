using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using WinPrint.Core.Models;

namespace WinPrint.Core.ContentTypes {
    /// <summary>
    /// base class for Content/File types
    /// </summary>
    public abstract class ContentBase : ModelBase, INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            OnSettingsChanged(true);
            return true;
        }

        // if bool is true, reflow. Otherwise just paint
        public event EventHandler<bool> SettingsChanged;
        protected void OnSettingsChanged(bool reflow) => SettingsChanged?.Invoke(this, reflow);

        //public string Type { get => type; set => type = value; }
        //internal string type = "none";
        public static string Type = "none";

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
        public int GetNumPages() { return numPages; }

        /// <summary>
        /// Get total count of pages. Set any local page-size related values (e.g. linesPerPage).
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public abstract int CountPages(StreamReader streamToPrint, System.Drawing.Printing.PrinterResolution printerResolution);

        /// <summary>
        /// Paints a single page
        /// </summary>
        /// <param name="g">Graphics with 0,0 being the origin of the Page</param>
        /// <param name="pageNum">Page number to print</param>
        public abstract void PaintPage(Graphics g, int pageNum);

    }
}
