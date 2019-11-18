using System;

using Windows.UI.Xaml.Controls;

using WinPrint.ViewModels;

namespace WinPrint.Views
{
    public sealed partial class BlankPage : Page
    {
        private BlankViewModel ViewModel
        {
            get { return ViewModelLocator.Current.BlankViewModel; }
        }

        public BlankPage()
        {
            InitializeComponent();
        }
    }
}
