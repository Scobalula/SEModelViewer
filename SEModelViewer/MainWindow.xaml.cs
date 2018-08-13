/*
    Copyright (c) 2018 Philip/Scobalula - SEModelViewer

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Win32;
using System.Linq;

namespace SEModelViewer
{
    /// <summary>
    /// Model Class
    /// </summary>
    public class ModelFile : INotifyPropertyChanged
    {
        /// <summary>
        /// Model Bone Data
        /// </summary>
        public class ModelBone
        {
            public string Name { get; set; }

            public string Position { get; set; }
        }

        /// <summary>
        /// File Path
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// File Name without extension
        /// </summary>
        public string Name { get { return System.IO.Path.GetFileNameWithoutExtension(Path); } }

        /// <summary>
        /// Models Bones
        /// </summary>
        public List<ModelBone> ModeBones { get; set; }

        /// <summary>
        /// Has the bone count been loaded
        /// </summary>
        public bool BoneCountLoaded = false;

        /// <summary>
        /// Bone Count Value
        /// </summary>
        private int BoneCountValue { get; set; }

        /// <summary>
        /// Number of Bones
        /// </summary>
        public int BoneCount
        {
            get
            {
                return BoneCountValue;
            }
            set
            {
                if (BoneCountValue != value)
                {
                    BoneCountValue = value;
                    NotifyPropertyChanged("BoneCount");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Main Sausage
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Active Load Thread
        /// </summary>
        public Thread LoadThread { get; set; }

        /// <summary>
        /// Loads SEModels via Dialog
        /// </summary>
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (LoadThread?.IsAlive == true)
            {
                MessageBox.Show("Please wait for SEModelViewer to finish the current task.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var openFileDialog = new OpenFileDialog()
            {
                Filter = "SEModel File (*.semodel)|*.semodel",
                Multiselect = true,
                Title = "Select a SEModel File"
            };

            if(openFileDialog.ShowDialog() == true)
            {
                LoadThread = new Thread(delegate ()
                {
                    foreach(string file in openFileDialog.FileNames)
                    {
                        ProcessModel(file);
                    }
                });

                LoadThread.Start();
            }
        }

        /// <summary>
        /// Loads folder of OBJ Files
        /// </summary>
        private void LoadFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (LoadThread?.IsAlive == true)
            {
                MessageBox.Show("Please wait for SEModelViewer to finish the current task.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var folderDialog = new CommonOpenFileDialog()
            {
                EnsurePathExists = true,
                EnsureFileExists = false,
                AllowNonFileSystemItems = false,
                DefaultFileName = "Select Folder",
                Title = "Select a folder of SEModel Files/Folders with SEModels"
            };

            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok && !string.IsNullOrWhiteSpace(folderDialog.FileName))
            {
                ModelList.Items.Clear();

                LoadThread = new Thread(delegate () { LoadFoldersRecursive(Path.GetDirectoryName(folderDialog.FileName)); });
                LoadThread.Start();
            }
        }

        /// <summary>
        /// Loads SEModels from a Folder and all subfolders.
        /// </summary>
        /// <param name="folder"></param>
        public void LoadFoldersRecursive(string folder)
        {
            IEnumerable<string> files = Directory.EnumerateFiles(folder);

            foreach (string file in files)
            {
                if(Path.GetExtension(file) == ".semodel")
                {
                    ProcessModel(file);
                }
            }

            IEnumerable<string> directories = Directory.EnumerateDirectories(folder);

            foreach(string directory in directories)
            {
                LoadFoldersRecursive(directory);
            }
        }

        /// <summary>
        /// Adds a model to the UI List
        /// </summary>
        private void AddModel(ModelFile model)
        {
            Dispatcher.BeginInvoke(new Action(() => ModelList.Items.Add(model)));
        }

        /// <summary>
        /// Takes a SEModel Path and adds it to file list
        /// </summary>
        private void ProcessModel(string filePath, bool drawModel = false)
        {
            ModelFile model = new ModelFile()
            {
                Path = filePath,
                BoneCount = 0,
            };

            AddModel(model);

            if(drawModel)
                LoadModel(model);
        }

        /// <summary>
        /// Switches model on index change/click.
        /// </summary>
        private void ModelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var model = (ModelFile)ModelList.SelectedItem;

            LoadModel(model);
        }

        /// <summary>
        /// Loads an OBJ File, if it fails the model is not displayed.
        /// </summary>
        /// <param name="filePath"></param>
        public void LoadModel(ModelFile modelFile)
        {
            try
            {
                var watch            = Stopwatch.StartNew();
                SEModelImporter reader = new SEModelImporter
                {
                    LoadTextures = LoadTextures.IsChecked == true,
                    Folder = Path.GetDirectoryName(modelFile.Path)
                };

                using (FileStream stream = new FileStream(modelFile.Path, FileMode.Open))
                {
                    Model3DGroup loadedModel = reader.Read(stream);
                    modelFile.ModeBones = reader.ModelBones;
                    model.Content = loadedModel;
                    MainViewport.ZoomExtents(0);
                }
                watch.Stop();
                Status.Content        = string.Format("Status     : Loaded {0} in {1} Seconds", modelFile.Name, watch.ElapsedMilliseconds / 1000.0);
                VertexCount.Content   = string.Format("Vertices   : {0}", reader.VertexCount);
                FaceCount.Content     = string.Format("Faces      : {0}", reader.FaceCount);
                MaterialCount.Content = string.Format("Materials  : {0}", reader.MaterialCount);
                BoneCount.Content     = string.Format("Bones      : {0}", reader.BoneCount);
                Status.Foreground     = new SolidColorBrush(Colors.Lime);
            }
            catch (Exception e)
            {
                Status.Content        = string.Format("Status     : Error - {0}", e.Message);
                VertexCount.Content   = string.Format("Vertices   : 0");
                FaceCount.Content     = string.Format("Faces      : 0");
                MaterialCount.Content = string.Format("Materials  : 0");
                BoneCount.Content     = string.Format("Bones      : 0");
                Status.Foreground     = new SolidColorBrush(Colors.Red);
                model.Content         = null;
                return;
            }
        }

        /// <summary>
        /// Loads Model Information
        /// </summary>
        private void LoadModelInfo(List<ModelFile> modelFiles)
        {
            foreach (var modelFile in modelFiles)
            {
                try
                {
                    if (!modelFile.BoneCountLoaded)
                    {
                        using (BinaryReader reader = new BinaryReader(new FileStream(modelFile.Path, FileMode.Open)))
                        {
                            reader.BaseStream.Seek(14, SeekOrigin.Begin);
                            modelFile.BoneCount = reader.ReadInt32();
                        }
                        modelFile.BoneCountLoaded = false;
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// Copies model name to clipboard
        /// </summary>
        private void CopyAssetName_Click(object sender, RoutedEventArgs e)
        {
            if (ModelList.SelectedItems.Count == 0)
            {
                MessageBox.Show("No model selected", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ModelList.SelectedItem is ModelFile modelFile)
                Clipboard.SetText(modelFile.Name);
        }
        
        /// <summary>
        /// Opens About Window
        /// </summary>
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow()
            {
                Owner = this
            }.ShowDialog();
        }
        
        /// <summary>
        /// Loads Model Bone Counts
        /// </summary>
        private void LoadModelInfo_Click(object sender, RoutedEventArgs e)
        {
            if (LoadThread?.IsAlive == true)
            {
                MessageBox.Show("Please wait for SEModelViewer to finish the current task.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var models = ModelList.Items.Cast<ModelFile>().ToList();

            LoadThread = new Thread(delegate () { LoadModelInfo(models); });
            LoadThread.Start();
        }

        /// <summary>
        /// Shows Bone Info Window with Bone Names and Positions
        /// </summary>
        private void BonesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModelList.SelectedItems.Count == 0)
            {
                MessageBox.Show("No model selected", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BonesWindow bonesWindow = new BonesWindow();

            ModelFile modelFile = ModelList.SelectedItem as ModelFile;

            foreach(var bone in modelFile.ModeBones)
            {
                bonesWindow.Bones.Items.Add(bone);
            }

            bonesWindow.Owner = this;
            bonesWindow.ShowDialog();
        }

        /// <summary>
        /// Shuts down thread on close
        /// </summary>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            LoadThread?.Abort();
        }

        /// <summary>
        /// Aborts current task
        /// </summary>
        private void AbortTask_Click(object sender, RoutedEventArgs e)
        {
            LoadThread?.Abort();
        }

        /// <summary>
        /// Clears loaded models on request
        /// </summary>
        private void ClearModels_Click(object sender, RoutedEventArgs e)
        {
            ClearLoadedModels();
        }

        /// <summary>
        /// Clears loaded models
        /// </summary>
        private void ClearLoadedModels()
        {
            LoadThread?.Abort();
            ModelList.Items.Clear();
            Status.Content        = "Status     : Idle";
            VertexCount.Content   = "Vertices   : 0";
            FaceCount.Content     = "Faces      : 0";
            MaterialCount.Content = "Materials  : 0";
            BoneCount.Content     = "Bones      : 0";
            Status.Foreground = new SolidColorBrush(Colors.Lime);
        }

        /// <summary>
        /// Processes Dropped Data
        /// </summary>
        public void ProcessDroppedData(string[] paths)
        {
            foreach(string path in paths)
            {
                if (Path.GetExtension(path) == ".semodel" && File.Exists(path))
                {
                    ProcessModel(path);
                }
                else if(Directory.Exists(path))
                {
                    LoadFoldersRecursive(path);
                }
            }
        }

        /// <summary>
        /// Processes dropped model onto viewport
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainViewport_Drop(object sender, DragEventArgs e)
        {
            if (LoadThread?.IsAlive == true)
            {
                MessageBox.Show("Please wait for SEModelViewer to finish the current task.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach(string file in files)
                {
                    if(Path.GetExtension(file) == ".semodel" && File.Exists(file))
                    {
                        ProcessModel(file, true);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Processes dropped models/folders onto list
        /// </summary>
        private void ModelList_Drop(object sender, DragEventArgs e)
        {
            if (LoadThread?.IsAlive == true)
            {
                MessageBox.Show("Please wait for SEModelViewer to finish the current task.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] data = (string[])e.Data.GetData(DataFormats.FileDrop);

                LoadThread = new Thread(delegate () { ProcessDroppedData(data); });
                LoadThread.Start();
            }
        }
    }
}
