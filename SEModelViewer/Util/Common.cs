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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using SELib;
using SELib.Utilities;

namespace SEModelViewer.Util
{
    /// <summary>
    /// Model Class
    /// Holds Model Information such as data counts, etc.
    /// </summary>
    public class ModelFile : INotifyPropertyChanged
    {
        /// <summary>
        /// Model Bone Data
        /// </summary>
        public class ModelBone
        {
            /// <summary>
            /// Bone Name
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Bone Parent Index
            /// </summary>
            public int Parent { get; set; }

            public int Index { get; set; }
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
        public List<ModelBone> ModelBones { get; set; }

        /// <summary>
        /// Models Bones
        /// </summary>
        public List<SEModelMaterial> Materials { get; set; }

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

        public override bool Equals(object obj)
        {
            if (obj is ModelFile modelFile)
                return modelFile.Path == Path;

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
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
    /// ModelConverter Class
    /// Contains Base Model Converter Methods, to create a model converter,
    /// inherit from this class and override the FromSEModel method.
    /// </summary>
    public class ModelConverter
    {
        /// <summary>
        /// Registered Model Converters
        /// </summary>
        public static readonly Dictionary<string, ModelConverter> Converters = new Dictionary<string, ModelConverter>();

        /// <summary>
        /// Format Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Format Extension
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// Formats the description and extension
        /// </summary>
        /// <returns>Formatted String</returns>
        public override string ToString()
        {
            return String.Format("{0} (*{1})", Description, Extension);
        }

        /// <summary>
        /// Converts a SEModel to the given model format
        /// </summary>
        /// <param name="inputPath">Input SEModel Path</param>
        /// <param name="outputPath">Output file path</param>
        public virtual SEModel FromSEModel(string inputPath, string outputPath) { return new SEModel(); }

        /// <summary>
        /// Registers the Model Converter 
        /// </summary>
        public void Register()
        {
            Converters[Extension] = this;
        }

        /// <summary>
        /// Gets a model converter by extension
        /// </summary>
        /// <param name="extension">Format extension</param>
        /// <returns>Model Converter if found, otherwise null</returns>
        public static ModelConverter Get(string extension)
        {
            return Converters.TryGetValue(extension, out ModelConverter value) ? value : null;
        }

        /// <summary>
        /// Registers Model Converters
        /// </summary>
        public static void RegisterModelConverters()
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(x => x.BaseType == typeof(ModelConverter)))
                Activator.CreateInstance(type);
        }
    }

    /// <summary>
    /// Common Utils
    /// </summary>
    public class Common
    {
        /// <summary>
        /// Checks if the given path is valid
        /// </summary>
        public static bool IsValidPath(string path)
        {
            if (path == "")
                return true;

            try
            {
                // if a file exists with same name it can result in bad stuffs for folders
                if (File.Exists(path))
                    return false;

                Path.GetDirectoryName(path);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static void CopyImages(SEModel model, string inputDirectory, string outputDirectory)
        {
            outputDirectory = Path.Combine(outputDirectory, "_images");

            Directory.CreateDirectory(outputDirectory);

            foreach (var material in model.Materials)
            {
                var data = material.MaterialData as SEModelSimpleMaterial;

                string[] images = {
                    Path.Combine(inputDirectory, data.DiffuseMap),
                    Path.Combine(inputDirectory, data.NormalMap),
                    Path.Combine(inputDirectory, data.SpecularMap),
                };

                foreach(string image in images)
                {
                    if(File.Exists(image))
                    {
                        File.Copy(image, Path.Combine(outputDirectory, Path.GetFileName(image)), true);
                    }
                }
            }
        }

        /// <summary>
        /// Converts a Model to the given format
        /// </summary>
        public static void ConvertModel(
            ModelFile modelFile,
            string outputFolder,
            bool overwrite,
            bool copyImage,
            bool newFolder,
            ModelConverter converter,
            string prefix)
        {
            string outputDirectory = newFolder ? Path.Combine(outputFolder, modelFile.Name) : outputFolder;
            string inputDirectory = Path.GetDirectoryName(modelFile.Path);
            string outputPath = Path.Combine(outputDirectory, prefix + modelFile.Name);

            Directory.CreateDirectory(outputDirectory);

            if (File.Exists(modelFile.Path))
            {
                if (File.Exists(outputPath) || !overwrite)
                    return;

                var model = converter.FromSEModel(modelFile.Path, outputPath + converter.Extension);

                if (copyImage) CopyImages(model, inputDirectory, outputDirectory);
            }
        }

        /// <summary>
        /// Clamps Value to a range.
        /// </summary>
        /// <param name="value">Value to Clamp</param>
        /// <param name="max">Max value</param>
        /// <param name="min">Min value</param>
        /// <returns>Clamped Value</returns>
        public static T Clamp<T>(T value, T max, T min) where T : IComparable<T>
        {
            return value.CompareTo(min) < 0 ? min : value.CompareTo(max) > 0 ? max : value;
        }

        /// <summary>
        /// Converts CM to Inch
        /// </summary>
        /// <param name="value">CM Value</param>
        /// <returns>Value in inches</returns>
        public static double CMToInch(double value)
        {
            return value * 0.3937007874015748031496062992126;
        }
    }

    /// <summary>
    /// A container for a 4 Dimensional Vector
    /// </summary>
    public class Vector4 : Vector3
    {
        public double W { get; set; }

        public Vector4() { X = 0; Y = 0; Z = 0; W = 1; }
        public Vector4(float XCoord, float YCoord, float ZCoord, float WCoord) { X = XCoord; Y = YCoord; Z = ZCoord; W = WCoord; }
    }
}
