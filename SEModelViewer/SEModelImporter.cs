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
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using SELib;
using HelixToolkit.Wpf;
using System.Linq;

namespace SEModelViewer
{
    /// <summary>
    /// SEModel Helix Importer
    /// </summary>
    class SEModelImporter : ModelReader
    {
        public static string[] AcceptedImageExtensions =
        {
            ".PNG",
            ".TIF",
            ".TIFF",
            ".JPG",
            ".JPEG",
            ".BMP",
        };

        /// <summary>
        /// Number of Materials
        /// </summary>
        public int MaterialCount { get; set; }

        /// <summary>
        /// Number of Vertices
        /// </summary>
        public uint VertexCount { get; set; }

        /// <summary>
        /// Number of Faces
        /// </summary>
        public uint FaceCount { get; set; }

        /// <summary>
        /// Number of Bones
        /// </summary>
        public uint BoneCount { get; set; }

        /// <summary>
        /// Load Textures from Material Data
        /// </summary>
        public bool LoadTextures { get; set; }

        /// <summary>
        /// Folder Path (For material loader)
        /// </summary>
        public string Folder { get; set; }

        /// <summary>
        /// Random Int (For material loader)
        /// </summary>
        private readonly Random RandomInt = new Random();

        /// <summary>
        /// Bones in this Model
        /// </summary>
        public List<ModelFile.ModelBone> ModelBones { get; set; }

        /// <summary>
        /// SEModel Materials
        /// </summary>
        private readonly List<Material> Materials = new List<Material>();

        /// <summary>
        /// SEModel Meshes
        /// </summary>
        private readonly List<Mesh> Meshes = new List<Mesh>();

        /// <summary>
        /// Loads SEModel
        /// </summary>
        public override Model3DGroup Read(Stream s)
        {
            SEModel semodel = SEModel.Read(s);

            Model3DGroup modelGroup = new Model3DGroup();

            MaterialCount = semodel.Materials.Count;
            BoneCount = semodel.BoneCount;


            ModelBones = new List<ModelFile.ModelBone>();

            LoadBones(semodel);

            LoadMaterials(semodel);

            foreach(var semesh in semodel.Meshes)
            {
                Mesh mesh = new Mesh
                {
                    Positions = new List<Point3D>(),
                    TriangleIndices = new List<int>(),
                    TextureCoordinates = new List<Point>(),
                    Normals = new List<Vector3D>(),
                    Material = Materials[semesh.MaterialReferenceIndicies[0]]
                };

                VertexCount += semesh.VertexCount;
                FaceCount += semesh.FaceCount;

                foreach(var vertex in semesh.Verticies)
                {
                    mesh.Positions.Add(new Point3D(vertex.Position.X, vertex.Position.Y, vertex.Position.Z));
                    mesh.TextureCoordinates.Add(new Point(vertex.UVSets[0].X, vertex.UVSets[0].Y));
                    mesh.Normals.Add(new Vector3D(vertex.VertexNormal.X, vertex.VertexNormal.Y, vertex.VertexNormal.Z));
                }

                foreach(var face in semesh.Faces )
                {
                    mesh.TriangleIndices.Add((int)face.FaceIndex1);
                    mesh.TriangleIndices.Add((int)face.FaceIndex2);
                    mesh.TriangleIndices.Add((int)face.FaceIndex3);
                }

                modelGroup.Children.Add(mesh.CreateModel());
            }

            return modelGroup;
        }

        /// <summary>
        /// Loads Bone Names and Offsets (As a string formatted)
        /// </summary>
        private void LoadBones(SEModel semodel)
        {
            foreach(var bone in semodel.Bones)
            {
                ModelBones.Add(new ModelFile.ModelBone()
                {
                    Name = bone.BoneName,
                    Position = String.Format("({0}, {1}, {2})", bone.GlobalPosition.X, bone.GlobalPosition.Y, bone.GlobalPosition.Z)
                });
            }
        }

        /// <summary>
        /// Loads materials and textures (if they exist)
        /// </summary>
        private void LoadMaterials(SEModel semodel)
        {
            foreach(var material in semodel.Materials)
            {
                var materialGroup = new MaterialGroup();

                var data = material.MaterialData as SEModelSimpleMaterial;

                string image = Path.Combine(Folder, data.DiffuseMap);

                if (File.Exists(image) && AcceptedImageExtensions.Contains(Path.GetExtension(image).ToUpper()) && LoadTextures == true)
                {
                    materialGroup.Children.Add(new DiffuseMaterial(CreateTextureBrush(image)));
                }
                else
                {

                    materialGroup.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromRgb
                        (
                            (byte)RandomInt.Next(128, 255),
                            (byte)RandomInt.Next(128, 255),
                            (byte)RandomInt.Next(128, 255)
                        ))));
                }

                Materials.Add(materialGroup);
            }
        }

        /// <summary>
        /// Loads texture
        /// </summary>
        private ImageBrush CreateTextureBrush(string path)
        {
            var img = new BitmapImage(new Uri(path, UriKind.Relative));
            var textureBrush = new ImageBrush(img) { Opacity = 1.0, ViewportUnits = BrushMappingMode.Absolute, TileMode = TileMode.Tile };
            return textureBrush;
        }

        /// <summary>
        /// Mesh Data
        /// </summary>
        private class Mesh
        {
            /// <summary>
            /// Vertex Positions
            /// </summary>
            public List<Point3D> Positions { get; set; }

            /// <summary>
            /// Face Indices
            /// </summary>
            public List<int> TriangleIndices { get; set; }

            /// <summary>
            /// UV Positions
            /// </summary>
            public List<Point> TextureCoordinates { get; set; }

            /// <summary>
            /// Vertex Normals
            /// </summary>
            public List<Vector3D> Normals { get; set; }

            /// <summary>
            /// Mesh Material
            /// </summary>
            public Material Material { get; set; }

            /// <summary>
            /// Creates a Model from Mesh Data
            /// </summary>
            /// <returns></returns>
            public Model3D CreateModel()
            {
                var geometry = new MeshGeometry3D
                {
                    Positions = new Point3DCollection(Positions),
                    TriangleIndices = new Int32Collection(TriangleIndices),
                    Normals = new Vector3DCollection(Normals)
                };
                if (TextureCoordinates != null)
                {
                    geometry.TextureCoordinates = new PointCollection(TextureCoordinates);
                }

                return new GeometryModel3D(geometry, Material) { BackMaterial = Material };
            }
        }
    }
}
