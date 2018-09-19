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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SEModelViewer.Util;
using SELib;

namespace SEModelViewer.Converters
{
    /// <summary>
    /// XMODEL EXPORTER Exporter Class
    /// Contains methods that handle converting a SEModel to an XModel_Export File (Call of Duty Mod Tools)
    /// </summary>
    class XMEExporter : ModelConverter
    {
        /// <summary>
        /// Initializes XMODEL_EXPORT converter and registers it with the converters
        /// </summary>
        public XMEExporter()
        {
            Extension = ".xmodel_export";
            Description = "XModel ASCII File (Call of Duty)";
            Register();
        }

        #region HelperFunctions

        /// <summary>
        /// Writes comment block 
        /// </summary>
        /// <param name="writer">StreamWriter stream</param>
        /// <param name="comment">String to write</param>
        private static void WriteCommentBlock(StreamWriter writer, string comment)
        {
            writer.WriteLine("// {0}", comment);
        }

        /// <summary>
        /// Writes model token block
        /// </summary>
        /// <param name="writer">StreamWriter stream</param>
        private static void WriteModelBlock(StreamWriter writer)
        {
            writer.WriteLine("MODEL");
        }

        /// <summary>
        /// Writes model version block
        /// </summary>
        /// <param name="writer">StreamWriter stream</param>
        /// <param name="version">xModel Version</param>
        private static void WriteVersionBlock(StreamWriter writer, int version)
        {
            writer.WriteLine("VERSION {0}", version);
        }

        /// <summary>
        /// Writes Bones and their positions/rotations
        /// </summary>
        /// <param name="writer">StreamWriter stream</param>
        /// <param name="model">SEModel Object</param>
        private static void WriteBones(StreamWriter writer, SEModel model)
        {
            writer.WriteLine("NUMBONES {0}", model.BoneCount);

            for (int i = 0; i < model.BoneCount; i++)
            {
                writer.WriteLine("BONE {0} {1} \"{2}\"", i, model.Bones[i].BoneParent, model.Bones[i].BoneName);
            }

            for (int i = 0; i < model.BoneCount; i++)
            {
                var rotation = Rotation.QuatToMat(model.Bones[i].GlobalRotation);
                writer.WriteLine("BONE {0}", i);
                writer.WriteLine("OFFSET {0:0.000000} {1:0.000000} {2:0.000000}",
                    Common.CMToInch(model.Bones[i].GlobalPosition.X),
                    Common.CMToInch(model.Bones[i].GlobalPosition.Y),
                    Common.CMToInch(model.Bones[i].GlobalPosition.Z));
                writer.WriteLine("X {0:0.000000} {1:0.000000} {2:0.000000}",
                    rotation.X.X,
                    rotation.Y.X,
                    rotation.Z.X);
                writer.WriteLine("Y {0:0.000000} {1:0.000000} {2:0.000000}",
                    rotation.X.Y,
                    rotation.Y.Y,
                    rotation.Z.Y);
                writer.WriteLine("Z {0:0.000000} {1:0.000000} {2:0.000000}",
                    rotation.X.Z,
                    rotation.Y.Z,
                    rotation.Z.Z);
            }
        }

        /// <summary>
        /// Writes Vertex Positon and Weights from each SEModel Mesh
        /// </summary>
        /// <param name="writer">StreamWriter stream</param>
        /// <param name="model">SEModel Object</param>
        private static void WriteVertices(StreamWriter writer, SEModel model)
        {
            uint globalVertexIndex = 0;

            writer.WriteLine("NUMVERTS {0}", model.Meshes.Sum(x => x.VertexCount));

            foreach (var mesh in model.Meshes)
            {
                for (int i = 0; i < mesh.VertexCount; i++)
                {
                    writer.WriteLine("VERT {0}", globalVertexIndex + i);
                    writer.WriteLine("OFFSET {0:0.000000} {1:0.000000} {2:0.000000}",
                        Common.CMToInch(mesh.Verticies[i].Position.X),
                        Common.CMToInch(mesh.Verticies[i].Position.Y),
                        Common.CMToInch(mesh.Verticies[i].Position.Z));
                    writer.WriteLine("BONES {0}", mesh.Verticies[i].Weights.Count(x => x.BoneWeight != 0.00000));

                    foreach (var weight in mesh.Verticies[i].Weights)
                        if (weight.BoneWeight != 0.00000)
                            writer.WriteLine("BONE {0} {1:0.000000}",
                                weight.BoneIndex,
                                weight.BoneWeight);
                }

                globalVertexIndex += mesh.VertexCount;
            }
        }

        /// <summary>
        /// Writes Face Vertex
        /// </summary>
        /// <param name="writer">StreamWriter stream</param>
        /// <param name="mesh">Parent Mesh</param>
        /// <param name="index">Vertex Index</param>
        /// <param name="offset">Global Vertex Offset</param>
        private static void WriteFaceVertex(StreamWriter writer, SEModelMesh mesh, uint index, uint offset)
        {
            writer.WriteLine("VERT {0}", offset + index);
            writer.WriteLine("NORMAL {0:0.000000} {1:0.000000} {2:0.000000}",
                mesh.Verticies[(int)index].VertexNormal.X,
                mesh.Verticies[(int)index].VertexNormal.Y,
                mesh.Verticies[(int)index].VertexNormal.Z);
            writer.WriteLine("COLOR {0:0.000000} {1:0.000000} {2:0.000000} {3:0.000000}",
                mesh.Verticies[(int)index].VertexColor.R / 255.0,
                mesh.Verticies[(int)index].VertexColor.G / 255.0,
                mesh.Verticies[(int)index].VertexColor.B / 255.0,
                mesh.Verticies[(int)index].VertexColor.A / 255.0);
            writer.WriteLine("UV 1 {0:0.000000} {1:0.000000}",
                mesh.Verticies[(int)index].UVSets[0].X,
                mesh.Verticies[(int)index].UVSets[0].Y);
        }

        /// <summary>
        /// Writes Faces from each SEModel Mesh
        /// </summary>
        /// <param name="writer">StreamWriter stream</param>
        /// <param name="model">SEModel Object</param>
        private static void WriteFaces(StreamWriter writer, SEModel model)
        {
            uint globalVertexIndex = 0;

            writer.WriteLine("NUMFACES {0}", model.Meshes.Sum(x => x.FaceCount));

            for (int i = 0; i < model.MeshCount; i++)
            {
                for (int j = 0; j < model.Meshes[i].FaceCount; j++)
                {
                    writer.WriteLine("TRI {0} {1} 0 0", i, model.Meshes[i].MaterialReferenceIndicies[0]);

                    WriteFaceVertex(
                        writer,
                        model.Meshes[i],
                        model.Meshes[i].Faces[j].FaceIndex1,
                        globalVertexIndex);
                    WriteFaceVertex(
                        writer, model.Meshes[i],
                        model.Meshes[i].Faces[j].FaceIndex2,
                        globalVertexIndex);
                    WriteFaceVertex(
                        writer,
                        model.Meshes[i],
                        model.Meshes[i].Faces[j].FaceIndex3,
                        globalVertexIndex);
                }

                globalVertexIndex += model.Meshes[i].VertexCount;
            }
        }

        /// <summary>
        /// Writes Objects/Meshes
        /// </summary>
        /// <param name="writer">StreamWriter stream</param>
        /// <param name="objects">Objects/Meshes</param>
        private static void WriteObjects(StreamWriter writer, List<SEModelMesh> objects)
        {
            writer.WriteLine("NUMOBJECTS {0}", objects.Count);

            for (int i = 0; i < objects.Count; i++)
            {
                writer.WriteLine("OBJECT {0} \"SEModelViewerMesh_{0}\"", i);
            }
        }

        /// <summary>
        /// Writes Materials
        /// </summary>
        /// <param name="writer">StreamWriter stream</param>
        /// <param name="materials">SEModel Materials</param>
        private static void WriteMaterials(StreamWriter writer, List<SEModelMaterial> materials)
        {
            writer.WriteLine("NUMMATERIALS {0}", materials.Count);

            for (int i = 0; i < materials.Count; i++)
            {
                var data = materials[i].MaterialData as SEModelSimpleMaterial;
                writer.WriteLine("MATERIAL {0} \"{1}\" \"Lambert\" \"{2}\"", 
                    i, 
                    materials[i].Name,
                    data.DiffuseMap);
                writer.WriteLine("COLOR 0.000000 0.000000 0.000000 1.000000");
                writer.WriteLine("TRANSPARENCY 0.000000 0.000000 0.000000 1.000000");
                writer.WriteLine("AMBIENTCOLOR 0.000000 0.000000 0.000000 1.000000");
                writer.WriteLine("INCANDESCENCE 0.000000 0.000000 0.000000 1.000000");
                writer.WriteLine("COEFFS 0.800000 0.000000");
                writer.WriteLine("GLOW 0.000000 0");
                writer.WriteLine("REFRACTIVE 6 1.000000");
                writer.WriteLine("SPECULARCOLOR -1.000000 -1.000000 -1.000000 1.000000");
                writer.WriteLine("REFLECTIVECOLOR -1.000000 -1.000000 -1.000000 1.000000");
                writer.WriteLine("REFLECTIVE -1 -1.000000");
                writer.WriteLine("BLINN -1.000000 -1.000000");
                writer.WriteLine("PHONG -1.000000");
            }
        }

        #endregion

        /// <summary>
        /// Converts a SEModel to XMODEL_EXPORT
        /// </summary>
        /// <param name="inputPath">Input SEModel Path</param>
        /// <param name="outputPath">Output file path</param>
        public override SEModel FromSEModel(string inputPath, string outputPath)
        {
            SEModel model = SEModel.Read(inputPath);

            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                WriteCommentBlock(writer, "Exported via SEModelViewer by Scobalula");
                WriteCommentBlock(writer, "Export filename: " + outputPath);
                WriteCommentBlock(writer, "Source filename: " + inputPath);
                WriteCommentBlock(writer, "Export time: " + DateTime.Now);
                WriteModelBlock(writer);
                WriteVersionBlock(writer, 6);
                WriteBones(writer, model);
                WriteVertices(writer, model);
                WriteFaces(writer, model);
                WriteObjects(writer, model.Meshes);
                WriteMaterials(writer, model.Materials);
            }

            return model;
        }
    }
}
