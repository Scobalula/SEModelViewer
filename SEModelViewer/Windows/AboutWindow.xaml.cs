// ------------------------------------------------------------------------
// SEModelViewer - Tool to view SEModel Files
// Copyright (C) 2018 Philip/Scobalula
// ------------------------------------------------------------------------
using System.Windows;
using System.Diagnostics;

namespace SEModelViewer
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Opens donate link
        /// </summary>
        private void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://ko-fi.com/scobalula");
        }
    }
}
