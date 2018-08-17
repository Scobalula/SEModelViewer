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
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using SELib;
using HelixToolkit.Wpf;
using System.Linq;

namespace SEModelViewer.Util
{
    /// <summary>
    /// SEModel Helix Importer
    /// Handles loading SEModel Data for the Helix Viewport
    /// </summary>
    class SEModelImporter : ModelReader
    {
        /// <summary>
        /// Accepted Image Formats for Textures
        /// </summary>
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
        public readonly List<SEModelMaterial> SEModelMaterials = new List<SEModelMaterial>();

        /// <summary>
        /// Helix Materials
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
            for(int i = 0; i < semodel.Bones.Count; i++)
            {
                ModelBones.Add(new ModelFile.ModelBone()
                {
                    Name     = semodel.Bones[i].BoneName,
                    Index    = i,
                    Parent   = semodel.Bones[i].BoneParent,
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
                SEModelMaterials.Add(material);

                var materialGroup = new MaterialGroup();

                var data = material.MaterialData as SEModelSimpleMaterial;

                string image = Path.Combine(Folder, data.DiffuseMap);

                if (File.Exists(image) && AcceptedImageExtensions.Contains(Path.GetExtension(image).ToUpper()) && LoadTextures == true)
                {
                    materialGroup.Children.Add(new DiffuseMaterial(CreateTextureBrush(image)));
                }
                else
                {
                    // Assign a random color
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
