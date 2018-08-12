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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

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
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Loads a single model
        /// </summary>
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "SEModel File (*.semodel)|*.semodel"
            };

            if(openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
            {
                ModelFile modelFile = new ModelFile() { Path = openFileDialog.FileName };
                LoadModel(modelFile);
            }
        }

        /// <summary>
        /// Loads folder of OBJ Files
        /// </summary>
        private void LoadFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new FolderBrowserDialog();

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
            {
                ModelList.Items.Clear();

                string[] files = Directory.GetFiles(folderDialog.SelectedPath, "*.semodel*", SearchOption.AllDirectories);

                foreach(string file in files)
                {
                    ModelFile model = new ModelFile()
                    {
                        Path = file,
                        BoneCount = 0,
                    };

                    ModelList.Items.Add(model);
                }
            }
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
                SEModelImporter reader  = new SEModelImporter();
                reader.LoadTextures = LoadTextures.IsChecked == true;
                reader.Folder = Path.GetDirectoryName(modelFile.Path);

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
                Console.WriteLine(e);
                Status.Content        = string.Format("Status     : Error - {0}", e.Message);
                VertexCount.Content   = string.Format("Vertices   : None");
                FaceCount.Content     = string.Format("Faces      : None");
                MaterialCount.Content = string.Format("Materials  : None");
                BoneCount.Content     = string.Format("Bones      : None");
                Status.Foreground     = new SolidColorBrush(Colors.Red);
                model.Content         = null;
                return;
            }
        }

        /// <summary>
        /// Copies model name to clipboard
        /// </summary>
        private void CopyAssetName_Click(object sender, RoutedEventArgs e)
        {
            ModelFile model = (ModelFile)ModelList.SelectedItem;

            System.Windows.Clipboard.SetText(model.Name);
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
            try
            {
                foreach (var item in ModelList.Items)
                {
                    ModelFile modelFile = item as ModelFile;

                    using (BinaryReader reader = new BinaryReader(new FileStream(modelFile.Path, FileMode.Open)))
                    {
                        reader.BaseStream.Seek(14, SeekOrigin.Begin);
                        modelFile.BoneCount = reader.ReadInt32();
                    }
                }
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// Shows Bone Info Window with Bone Names and Positions
        /// </summary>
        private void BonesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModelList.SelectedItems.Count == 0)
            {
                System.Windows.MessageBox.Show("No model selected", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
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
    }
}
