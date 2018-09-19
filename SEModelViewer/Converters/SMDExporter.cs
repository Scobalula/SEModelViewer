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
using System.Linq;
using SEModelViewer.Util;
using SELib.Utilities;
using SELib;

namespace SEModelViewer.Converters
{
    /// <summary>
    /// SMD Exporter Class
    /// </summary>
    class SMDExporter : ModelConverter
    {
        /// <summary>
        /// Initializes SMD converter and registers it with the converters
        /// </summary>
        public SMDExporter()
        {
            Extension = ".smd";
            Description = "Studiomdl Data (Valve)";
            Register();
        }

        #region HelperFunctions

        /// <summary>
        /// Normalizes Vertex Normal
        /// </summary>
        private static void NormalizeVertexNormal(Vector3 input)
        {
            var length = Math.Sqrt(
                input.X * input.X +
                input.Y * input.Y +
                input.Z * input.Z
                );

            input.X /= length;
            input.Y /= length;
            input.Z /= length;
        }

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
        /// Writes Version Block
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="version"></param>
        private static void WriteVersionBlock(StreamWriter writer, int version)
        {
            writer.WriteLine("version {0}", version);
        }

        /// <summary>
        /// Writes SMD Nodes
        /// </summary>
        /// <param name="writer">StreamWriter stream</param>
        /// <param name="model">SEModel Object</param>
        private static void WriteNodes(StreamWriter writer, SEModel model)
        {
            writer.WriteLine("nodes");
            for (int i = 0; i < model.BoneCount; i++)
                writer.WriteLine("{0} \"{1}\" {2}", i, model.Bones[i].BoneName, model.Bones[i].BoneParent);
            writer.WriteLine("end");
            writer.WriteLine("skeleton");
            writer.WriteLine("time 0");
            for (int i = 0; i < model.BoneCount; i++)
            {
                var rotation = Rotation.QuatToEuler(model.Bones[i].LocalRotation);
                writer.WriteLine("{0} {1:0.000000} {2:0.000000} {3:0.000000} {4:0.000000} {5:0.000000} {6:0.000000}",
                    i,
                    Common.CMToInch(model.Bones[i].LocalPosition.X),
                    Common.CMToInch(model.Bones[i].LocalPosition.Y),
                    Common.CMToInch(model.Bones[i].LocalPosition.Z),
                    rotation.X,
                    rotation.Y,
                    rotation.Z);
            }
            writer.WriteLine("end");
        }

        /// <summary>
        /// Writes SMD Triangles
        /// </summary>
        /// <param name="writer">StreamWriter stream</param>
        /// <param name="model">SEModel Object</param>
        private static void WriteTriangleBlocks(StreamWriter writer, SEModel model)
        {
            foreach (var mesh in model.Meshes)
            {
                writer.WriteLine("triangles");
                foreach (var face in mesh.Faces)
                {
                    writer.WriteLine(model.Materials[mesh.MaterialReferenceIndicies[0]].Name);
                    WriteFacePoint(writer, mesh.Verticies[(int)face.FaceIndex3]);
                    WriteFacePoint(writer, mesh.Verticies[(int)face.FaceIndex2]);
                    WriteFacePoint(writer, mesh.Verticies[(int)face.FaceIndex1]);
                }
                writer.WriteLine("end");
            }
        }

        /// <summary>
        /// Writes a Face Point
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="vertex"></param>
        private static void WriteFacePoint(StreamWriter writer, SEModelVertex vertex)
        {
            NormalizeVertexNormal(vertex.VertexNormal);

            writer.Write("0");
            writer.Write(" {0:0.000000} {1:0.000000} {2:0.000000}",
                Common.CMToInch(vertex.Position.X),
                Common.CMToInch(vertex.Position.Y),
                Common.CMToInch(vertex.Position.Z));
            writer.Write(" {0:0.000000} {1:0.000000} {2:0.000000}",
                vertex.VertexNormal.X,
                vertex.VertexNormal.Y,
                vertex.VertexNormal.Z);
            writer.Write(" {0:0.000000} {1:0.000000}",
                vertex.UVSets[0].X,
                1 - vertex.UVSets[0].Y);

            writer.Write(" {0}", vertex.Weights.Count(x => x.BoneWeight != 0.00000));
            foreach (var weight in vertex.Weights)
                if (weight.BoneWeight != 0.00000)
                    writer.Write(" {0} {1:0.000000}",
                        weight.BoneIndex,
                        weight.BoneWeight);
            writer.WriteLine();
        }

        #endregion

        /// <summary>
        /// Converts a SEModel to SMD
        /// </summary>
        /// <param name="inputPath">Input SEModel Path</param>
        /// <param name="outputPath">Output file path</param>
        public override SEModel FromSEModel(string inputPath, string outputPath)
        {
            SEModel model = SEModel.Read(inputPath);

            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                WriteVersionBlock(writer, 1);
                WriteCommentBlock(writer, "Exported via SEModelViewer by Scobalula");
                WriteCommentBlock(writer, "Export filename: " + outputPath);
                WriteCommentBlock(writer, "Source filename: " + inputPath);
                WriteCommentBlock(writer, "Export time: " + DateTime.Now);
                WriteNodes(writer, model);
                WriteTriangleBlocks(writer, model);
            }

            return model;
        }
    }
}
