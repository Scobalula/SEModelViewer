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
#define TRACE
using System;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Linq;
using System.Windows.Input;
using SEModelViewer.Util;
using SEModelViewer.Windows;
using SELib;


namespace SEModelViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Properties

        /// <summary>
        /// Active Load Thread
        /// </summary>
        private Thread LoadThread { get; set; }

        /// <summary>
        /// Watcher Variable for current active thread whether to end or not
        /// </summary>
        private bool EndThread = false;

        /// <summary>
        /// Loaded Models
        /// </summary>
        private Dictionary<string, ModelFile> LoadedModels = new Dictionary<string, ModelFile>();

        #endregion

        #region MainOperations

        /// <summary>
        /// Main Sausage
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Settings.Load("settings.smvcfg");
            LoadTextures.IsChecked = Settings.Get("loadtexture", "yes") == "yes";
            ShowGrid.IsChecked = Settings.Get("showgrid", "yes") == "yes";
            ViewGrid.Visible = Settings.Get("showgrid", "yes") == "yes";
            ModelConverter.RegisterModelConverters();
            Loaded += new RoutedEventHandler(MainWindow_OnLoad);
        }

        /// <summary>
        /// Loads model if one was loaded via Drag and Drop or Open With
        /// </summary>
        private void MainWindow_OnLoad(object sender, RoutedEventArgs e)
        {
            if (Application.Current.Properties["SEModelInput"] != null)
                ProcessModel(Application.Current.Properties["SEModelInput"].ToString(), true);
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Loads SEModels from a Folder and all subfolders.
        /// </summary>
        private void LoadDirectory(string folder)
        {
            IEnumerable<string> files = Directory.EnumerateFiles(folder);

            foreach (string file in files)
            {
                if (EndThread)
                    break;

                if (Path.GetExtension(file) == ".semodel")
                    ProcessModel(file);
            }


            IEnumerable<string> directories = Directory.EnumerateDirectories(folder);

            foreach (string directory in directories)
            {
                if (EndThread)
                    break;

                LoadDirectory(directory);
            }
        }

        /// <summary>
        /// Exports a list of models 
        /// </summary>
        private void ExportMultiple(List<ModelFile> modelFiles)
        {
            FileExportDialog dialog = new FileExportDialog()
            {
                Owner = this,
                Title = String.Format("Export {0} models", modelFiles.Count),
            };

            dialog.ShowDialog();

            ModelConverter converter = dialog.Formats.SelectedItem as ModelConverter;

            Settings.Set("extension", converter.Extension);

            string outputFolder = dialog.OutputFolder.Text;

            if (dialog.ClosedByButton)
            {
                TaskLabel.Content = "Converting Models...";
                TaskBarProgress.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                LoadThread = new Thread(delegate ()
                {
                    for (int i = 0; i < modelFiles.Count && !EndThread; i++)
                    {
                        UpdateProgress(((double)i / modelFiles.Count) * 100);
                        try
                        {
                            Common.ConvertModel(
                                modelFiles[i],
                                outputFolder,
                                Settings.Get("overwrite", "yes") == "yes",
                                Settings.Get("copyimage", "yes") == "yes",
                                Settings.Get("newfolder", "yes") == "yes",
                                converter,
                                Settings.Get("exportprefix", ""));
                        }
                        catch (Exception e)
                        {
                            Trace.WriteLine(e);
                            if (MessageBox.Show(
                                String.Format("Error occured while converting {0}:\n\n{1}\n\nContinue?", modelFiles[i].Name, e), 
                                "ERROR", 
                                MessageBoxButton.YesNo, 
                                MessageBoxImage.Error) == MessageBoxResult.No)
                                break;
                        }
                    }
                    EndThread = false;
                    ResetProgress();
                });
                LoadThread.Start();
            }
        }

        /// <summary>
        /// Adds a model to the UI List
        /// </summary>
        private void AddModel(ModelFile model, bool drawModel)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!LoadedModels.ContainsKey(model.Name))
                {
                    LoadedModels.Add(model.Name, model);
                    ModelList.Items.Add(model);
                }
                else
                {
                    model = LoadedModels[model.Name];
                }

                if (drawModel)
                {
                    ModelList.SelectedItem = model;
                    ModelList.ScrollIntoView(model);
                    DrawModel(model);
                }
            }));
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

            AddModel(model, drawModel);

            LoadBoneCount(model);
        }

        /// <summary>
        /// Loads an OBJ File, if it fails the model is not displayed.
        /// </summary>
        /// <param name="filePath"></param>
        private void DrawModel(ModelFile modelFile)
        {
            if (modelFile == null)
                return;

            try
            {
                var watch = Stopwatch.StartNew();
                SEModelImporter reader = new SEModelImporter
                {
                    LoadTextures = LoadTextures.IsChecked == true,
                    Folder = Path.GetDirectoryName(modelFile.Path)
                };

                using (FileStream stream = new FileStream(modelFile.Path, FileMode.Open))
                {
                    Model3DGroup loadedModel = reader.Read(stream);
                    modelFile.Materials = reader.SEModelMaterials;
                    modelFile.ModelBones = reader.ModelBones;
                    model.Content = loadedModel;
                    MainViewport.ZoomExtents(0);
                }
                watch.Stop();
                Status.Content        = string.Format("Status     : Loaded {0} in {1} Seconds", 
                    modelFile.Name, watch.ElapsedMilliseconds / 1000.0);
                VertexCount.Content   = string.Format("Vertices   : {0}", reader.VertexCount);
                FaceCount.Content     = string.Format("Faces      : {0}", reader.FaceCount);
                MaterialCount.Content = string.Format("Materials  : {0}", reader.MaterialCount);
                BoneCount.Content     = string.Format("Bones      : {0}", reader.BoneCount);
                Status.Foreground = new SolidColorBrush(Colors.Lime);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                Status.Content        = string.Format("Status     : Error - {0}", e.Message);
                VertexCount.Content   = string.Format("Vertices   : 0");
                FaceCount.Content     = string.Format("Faces      : 0");
                MaterialCount.Content = string.Format("Materials  : 0");
                BoneCount.Content     = string.Format("Bones      : 0");
                Status.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }
        }

        /// <summary>
        /// Loads Bone Count from the header of a SEModel file
        /// </summary>
        private void LoadBoneCount(ModelFile modelFile)
        {
            byte[] buffer = new byte[18];

            try
            {
                // Only load if the previous bone count has not been loaded
                if (!modelFile.BoneCountLoaded)
                {
                    using (var stream = new FileStream(modelFile.Path, FileMode.Open)) stream.Read(buffer, 0, buffer.Length);
                    modelFile.BoneCount = BitConverter.ToInt32(buffer, 14);
                    modelFile.BoneCountLoaded = true;
                }
            }
            catch(Exception e)
            {
                Trace.WriteLine(e);
                modelFile.BoneCount = -1;
                modelFile.BoneCountLoaded = false;
            }
        }

        /// <summary>
        /// Recursively builds a tree from bones
        /// </summary>
        /// <param name="bones">Bone list</param>
        /// <param name="bone">Bone</param>
        /// <param name="parent">Parent Tree Item</param>
        private static void BuildBoneTree(
            IEnumerable<ModelFile.ModelBone> bones,
            ModelFile.ModelBone bone, 
            TreeViewItem parent)
        {
            parent.Items.Add(new TreeViewItem() { Header = CustomizeTreeViewItem(bone.Name, "bone") });

            var childBones = bones.Where(i => i.Parent == bone.Index);

            foreach (var childBone in childBones)
                BuildBoneTree(bones, childBone, (TreeViewItem)parent.Items[parent.Items.Count - 1]);
        }

        /// <summary>
        /// Expands a tree view item and all child items
        /// </summary>
        /// <param name="treeItem">Root Item</param>
        private void ExpandAllNodes(TreeViewItem treeItem)
        {
            // https://stackoverflow.com/a/22846911
            treeItem.IsExpanded = true;

            foreach (var childItem in treeItem.Items.OfType<TreeViewItem>())
                ExpandAllNodes(childItem);
        }

        /// <summary>
        /// Customizes a TreeView with an icon and label
        /// </summary>
        /// <param name="itemObj"></param>
        /// <returns></returns>
        private static StackPanel CustomizeTreeViewItem(object itemObj, string imageName)
        {
            // https://stackoverflow.com/a/28718751
            // Add Icon
            // Create Stack Panel
            StackPanel stkPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            // Create Image
            stkPanel.Children.Add(new Image
            {
                Source = new BitmapImage(new Uri(String.Format("pack://application:,,,/Icons/{0}.png", imageName))),
                Width = 16,
                Height = 16
            });

            // Create TextBlock
            stkPanel.Children.Add(new TextBlock
            {
                Text = itemObj.ToString()
            });

            return stkPanel;
        }

        /// <summary>
        /// Clears loaded models
        /// </summary>
        private void ClearLoadedModels()
        {
            EndThread = true;
            LoadedModels.Clear();
            ModelList.Items.Clear();
            ClearDrawnModel();
        }

        /// <summary>
        /// Clears the currently drawn model
        /// </summary>
        private void ClearDrawnModel()
        {
            model.Content         = null;
            Status.Content        = "Status     : Idle";
            VertexCount.Content   = "Vertices   : 0";
            FaceCount.Content     = "Faces      : 0";
            MaterialCount.Content = "Materials  : 0";
            BoneCount.Content     = "Bones      : 0";
            Status.Foreground = new SolidColorBrush(Colors.Lime);
        }

        /// <summary>
        /// Resets Progress Bars
        /// </summary>
        private void ResetProgress()
        {
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    TaskProgress.Value = 0;
                    TaskBarProgress.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                    TaskBarProgress.ProgressValue = 0;
                    TaskProgress.IsIndeterminate = false;
                    TaskLabel.Content = "Idle";
                }));
        }

        /// <summary>
        /// Updates Progress Bars
        /// </summary>
        /// <param name="value"></param>
        private void UpdateProgress(double value)
        {
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    TaskProgress.Value = value;
                    TaskBarProgress.ProgressValue = value / 100.0;
                }));
        }

        /// <summary>
        /// Processes Dropped Data
        /// </summary>
        private void ProcessDroppedData(string[] paths)
        {
            foreach (string path in paths)
            {
                try
                {
                    if (Path.GetExtension(path) == ".semodel" && File.Exists(path))
                    {
                        ProcessModel(path, paths.Length == 1);
                    }
                    else if (Directory.Exists(path))
                    {
                        LoadDirectory(path);
                    }
                }
                catch(Exception e)
                {
                    Trace.WriteLine(e);
                }
            }

            ResetProgress();
        }

        /// <summary>
        /// Builds File Dialog Filter
        /// </summary>
        private string BuildExportFilter()
        {
            string result = "";

            string defaultExtension = Settings.Get("extension", ".obj");

            foreach (var converter in ModelConverter.Converters)
            {
                string filter = String.Format("{0}|*{1}|", converter.Value.ToString(), converter.Key);

                if (converter.Key == defaultExtension)
                    result = filter + result;
                else
                    result += filter;
            }

            // Purge trailing bar
            return result.Remove(result.Length - 1);

        }

        /// <summary>
        /// Handles Clearing Search and Listed Items
        /// </summary>
        private void ClearSearch()
        {
            SearchBox.Text = "";

            if (LoadThread?.IsAlive == true)
            {
                MessageBox.Show("Please wait for SEModelViewer to finish the current task.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if a search was made (these would not equal)
            if (ModelList.Items.Count != LoadedModels.Count)
            {
                ModelList.Items.Clear();
                SearchBox.Clear();
                foreach (var model in LoadedModels)
                    ModelList.Items.Add(model.Value);
            }

            EndThread = false;
        }

        /// <summary>
        /// Handles executing search
        /// </summary>
        private void ExecuteSearch()
        {
            if (LoadThread?.IsAlive == true)
            {
                MessageBox.Show("Please wait for SEModelViewer to finish the current task.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if there is a query
            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                // Clear current list
                ModelList.Items.Clear();
                string[] query = SearchBox.Text.Split();
                foreach (var model in LoadedModels)
                {
                    if (query.Any(search => model.Value.Name.Contains(search)))
                    {
                        ModelList.Items.Add(model.Value);
                    }
                }
            }
            else
            {
                ClearSearch();
            }
        }

        #endregion

        #region CheckBoxOperations

        /// <summary>
        /// Enables/Disables Load Textures setting
        /// </summary>
        private void LoadTextures_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Set("loadtexture", LoadTextures.IsChecked == true ? "yes" : "no");
        }

        /// <summary>
        /// Enables/Disables Grid on Check
        /// </summary>
        private void ShowGrid_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Set("showgrid", ShowGrid.IsChecked == true ? "yes" : "no");
            ViewGrid.Visible = ShowGrid.IsChecked == true;
        }

        #endregion

        #region ButtonClickOperations

        /// <summary>
        /// Opens the Wiki
        /// </summary>
        private void Wiki_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Scobalula/SEModelViewer/wiki");
        }

        /// <summary>
        /// Closes window via File menu exit button
        /// </summary>
        private void AppExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
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

            var folderDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                ShowNewFolderButton = false,
                Description = "Select Folder to Import"
            };

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
                !string.IsNullOrWhiteSpace(folderDialog.SelectedPath) &&
                Directory.Exists(folderDialog.SelectedPath))
            {
                ModelList.Items.Clear();
                TaskProgress.IsIndeterminate = true;
                TaskLabel.Content = "Loading models...";
                LoadThread = new Thread(delegate () { LoadDirectory(Path.GetDirectoryName(folderDialog.SelectedPath)); EndThread = false; });
                LoadThread.Start();
            }
        }

        /// <summary>
        /// Exports selected models on click
        /// </summary>
        private void ExportModel_Click(object sender, RoutedEventArgs e)
        {
            if (LoadThread?.IsAlive == true)
            {
                MessageBox.Show("Please wait for SEModelViewer to finish the current task.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ModelList.SelectedItems.Count == 0)
            {
                MessageBox.Show("No model/s selected.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // If we're only exporting 1 Model, just show a standard dialog 
            if(ModelList.SelectedItems.Count < 2)
            {
                if (ModelList.SelectedItem is ModelFile modelFile)
                {
                    var fileSaveDialog = new SaveFileDialog()
                    {
                        Filter = BuildExportFilter(),
                        Title = "Export selected model",
                        FileName = modelFile.Name
                    };
                    
                    try
                    {
                        if (fileSaveDialog.ShowDialog() == true)
                        {
                            string extension = Path.GetExtension(fileSaveDialog.FileName);
                            Settings.Set("extension", extension);
                            ModelConverter.Get(extension).FromSEModel(modelFile.Path, fileSaveDialog.FileName);
                        }
                    }
                    catch(Exception exception)
                    {
                        Trace.WriteLine(exception);
                        MessageBox.Show(String.Format("Unhandled exception occured. Error:\n\n{0}", exception), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    
                }
            }
            else
            {
                ExportMultiple(ModelList.SelectedItems.Cast<ModelFile>().ToList());
            }
        }

        /// <summary>
        /// Exports all listed models on click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            if (LoadThread?.IsAlive == true)
            {
                MessageBox.Show("Please wait for SEModelViewer to finish the current task.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ModelList.Items.Count == 0)
            {
                MessageBox.Show("No models listed.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ExportMultiple(ModelList.Items.Cast<ModelFile>().ToList());
        }

        /// <summary>
        /// Opens and populates Model Info Window
        /// </summary>
        private void ModeInfoClickButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModelList.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a select or load a model first.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ModelList.SelectedItem is ModelFile modelFile)
            {
                ModelInfoWindow modelInfoWindow = new ModelInfoWindow
                {
                    Owner = this
                };

                // Add a root bone as our starting point
                var rootBone = new ModelFile.ModelBone() { Name = "Root", Index = -1, Parent = 0 };

                modelInfoWindow.InfoList.Items.Add(new TreeViewItem() { Header = String.Format("Bones: {0}", modelFile.ModelBones?.Count) });

                if (modelFile.ModelBones != null)
                    // Recursively build bones, to ensure child bones are parented
                    BuildBoneTree(modelFile.ModelBones, rootBone, (TreeViewItem)modelInfoWindow.InfoList.Items[0]);

                modelInfoWindow.InfoList.Items.Add(new TreeViewItem() { Header = String.Format("Materials: {0}", modelFile.Materials?.Count) });

                var materialItem = (TreeViewItem)modelInfoWindow.InfoList.Items[1];

                // Add Materials and Images
                if (modelFile.Materials != null)
                {
                    foreach (var material in modelFile.Materials)
                    {
                        var materialInfo = material.MaterialData as SEModelSimpleMaterial;

                        var item = new TreeViewItem() { Header = CustomizeTreeViewItem(material.Name, "material") };
                        item.Items.Add(new TreeViewItem() { Header = CustomizeTreeViewItem("Diffuse Map  - " + materialInfo.DiffuseMap, "image") });
                        item.Items.Add(new TreeViewItem() { Header = CustomizeTreeViewItem("Normal Map   - " + materialInfo.NormalMap, "image") });
                        item.Items.Add(new TreeViewItem() { Header = CustomizeTreeViewItem("Specular Map - " + materialInfo.SpecularMap, "image") });

                        materialItem.Items.Add(item);
                    }
                }

                // Gots to expand them views
                ExpandAllNodes((TreeViewItem)modelInfoWindow.InfoList.Items[0]);
                ExpandAllNodes((TreeViewItem)modelInfoWindow.InfoList.Items[1]);

                modelInfoWindow.ShowDialog();
            }
        }

        /// <summary>
        /// Copies model name to clipboard
        /// </summary>
        private void CopyAssetName_Click(object sender, RoutedEventArgs e)
        {
            if (ModelList.SelectedItem is ModelFile modelFile)
                Clipboard.SetText(modelFile?.Name);
        }

        /// <summary>
        /// Copies model file to clipboard
        /// </summary>
        private void CopyAssetFile_Click(object sender, RoutedEventArgs e)
        {
            if (ModelList.SelectedItem is ModelFile modelFile)
                // Have to set this as an array apparently, otherwise Windows won't paste
                Clipboard.SetDataObject(new DataObject(DataFormats.FileDrop, new string[] { modelFile.Path }));
        }

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
                Title = "Select SEModel Files"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                if(openFileDialog.FileNames.Length < 2)
                {
                    ProcessModel(openFileDialog.FileName, true);
                }
                else
                {
                    LoadThread = new Thread(delegate ()
                    {
                        foreach (string file in openFileDialog.FileNames)
                        {
                            ProcessModel(file);
                        }

                        EndThread = false;
                    });
                    LoadThread.Start();
                }
            }
        }

        /// <summary>
        /// Aborts current task
        /// </summary>
        private void AbortTask_Click(object sender, RoutedEventArgs e)
        {
            TaskProgress.IsIndeterminate = false;
            TaskBarProgress.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
            TaskProgress.Value = 0;
            TaskLabel.Content = "Idle";
            EndThread = true;
        }

        /// <summary>
        /// Clears loaded models on request
        /// </summary>
        private void ClearModels_Click(object sender, RoutedEventArgs e)
        {
            ClearLoadedModels();
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
        /// Clears search on request
        /// </summary>
        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            ClearSearch();
        }

        /// <summary>
        /// Executes search on request
        /// </summary>
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSearch();
        }

        #endregion

        #region FileDropOperations

        /// <summary>
        /// Processes dropped model onto viewport
        /// </summary>
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

                foreach (string file in files)
                {
                    if (Path.GetExtension(file) == ".semodel" && File.Exists(file))
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
                TaskProgress.IsIndeterminate = true;
                TaskBarProgress.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                TaskLabel.Content = "Loading models...";
                LoadThread = new Thread(delegate () { ProcessDroppedData(data); EndThread = false; });
                LoadThread.Start();
            }
        }

        #endregion

        #region MiscOperations

        /// <summary>
        /// Switches model on index change/click.
        /// </summary>
        private void ModelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModelList.SelectedItem is ModelFile modelFile)
                DrawModel(modelFile);
        }

        /// <summary>
        /// Shuts down thread on close and stores settings
        /// </summary>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            TaskProgress.IsIndeterminate = false;
            TaskBarProgress.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
            TaskProgress.Value = 0;
            TaskLabel.Content = "Idle";
            EndThread = true;
            Settings.Save("settings.cfg");
            // If thread doesn't quit after 1.5 seconds, abort it
            LoadThread?.Join(1500);
        }

        /// <summary>
        /// Executes search on enter
        /// </summary>
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ExecuteSearch();
        }

        #endregion
    }
}
