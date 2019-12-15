using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;

namespace WinPrint.UWPCore.Models {
    class Document {
        private ImageSource _image;

        public ImageSource Image {
            get { return _image ?? (_image = CreateImage()); }
        }

        private ImageSource CreateImage() {
            // load your image dynamically here
            // If you're creating it from scratch, WriteableBitmap might help you
            return null;
        }
    }
}
