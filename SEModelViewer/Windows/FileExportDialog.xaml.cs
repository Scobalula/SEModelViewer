// ------------------------------------------------------------------------
// SEModelViewer - Tool to view SEModel Files
// Copyright (C) 2018 Philip/Scobalula
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// ------------------------------------------------------------------------
using System.IO;
using System.Windows;
using SEModelViewer.Util;

namespace SEModelViewer.Windows
{
    /// <summary>
    /// Interaction logic for FileExportxaml
    /// </summary>
    public partial class FileExportDialog : Window
    {
        /// <summary>
        /// If the window was closed by the Export button
        /// </summary>
        public bool ClosedByButton = false;

        /// <summary>
        /// Initializes FileExportDialog
        /// </summary>
        public FileExportDialog()
        {
            InitializeComponent();
            PopulateFormats();
            Overwrite.IsChecked = Settings.Get("overwrite", "yes") == "yes";
            NewFolder.IsChecked = Settings.Get("newfolder", "yes") == "yes";
            OutputFolder.Text = Settings.Get("exportfolder", "");
            Prefix.Text = Settings.Get("exportprefix", "");
        }

        /// <summary>
        /// Populates formats combo box and sets it to last used extension
        /// </summary>
        public void PopulateFormats()
        {
            string defaultExtension = Settings.Get("extension", ".obj");

            foreach (var converter in ModelConverter.Converters)
            {
                Formats.Items.Add(converter.Value);

                if (converter.Value.Extension == defaultExtension)
                    Formats.SelectedItem = converter.Value;
            }
        }

        /// <summary>
        /// Sets ClosedByButton to true and closes window
        /// </summary>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ClosedByButton = true;
            Close();
        }

        /// <summary>
        /// Stores settings on close
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(!Common.IsValidPath(OutputFolder.Text) && ClosedByButton)
            {
                MessageBox.Show("Folder path is invalid.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                ClosedByButton = false;
                e.Cancel = true;
            }

            if (!Common.IsValidPath(Prefix.Text) && ClosedByButton)
            {
                MessageBox.Show("Prefix is invalid.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                ClosedByButton = false;
                e.Cancel = true;
            }

            Settings.Set("exportfolder", OutputFolder.Text);
            Settings.Set("exportprefix", Prefix.Text);
            Settings.Set("overwrite", Overwrite.IsChecked == true ? "yes" : "no");
            Settings.Set("newfolder", NewFolder.IsChecked == true ? "yes" : "no");
        }

        /// <summary>
        /// Opens Folder dialog and sets the selected folder
        /// </summary>
        private void SetFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                ShowNewFolderButton = true
            };

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                OutputFolder.Text = Path.GetDirectoryName(folderDialog.SelectedPath);
        }
    }
}
