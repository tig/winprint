using System;

using Windows.UI.Xaml.Controls;

using WinPrint.ViewModels;

namespace WinPrint.Views
{
    public sealed partial class MainPage : Page
    {
        private MainViewModel ViewModel
        {
            get { return ViewModelLocator.Current.MainViewModel; }
        }

        public MainPage()
        {
            InitializeComponent();
        }
    }
}
