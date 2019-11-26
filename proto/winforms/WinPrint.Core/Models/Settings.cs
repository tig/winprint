using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text;

namespace WinPrint.Core.Models {
    public class Settings : ModelBase {
        /// <summary>
        /// Window location
        /// </summary>
        public Point Location { get => location; set => SetField(ref location, value); }
        private Point location;

        /// <summary>
        /// Window size
        /// </summary>
        public Size Size { get => size; set => SetField(ref size, value); }
        private Size size;

        /// <summary>
        /// Default sheet (guid)
        /// </summary>
        public Guid DefaultSheet { get => defaultSheet; set => SetField(ref defaultSheet, value); }
        private Guid defaultSheet;

        // TODO: Make this a Dictionary<Guid, Sheet>
        public IList<Sheet> Sheets { get; set; }

        public Settings() {
        }


    }
}
