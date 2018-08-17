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
using SEModelViewer.Util;
using SELib;

namespace SEModelViewer.Converters
{
    /// <summary>
    /// OBJ Exporter Class
    /// </summary>
    class OBJExporter : ModelConverter
    {
        /// <summary>
        /// Initializes OBJ converter and registers it with the converters
        /// </summary>
        public OBJExporter()
        {
            Extension = ".obj";
            Description = "Wavefront OBJ File";
            Register();
        }

        /// <summary>
        /// Converts a SEModel to OBJ
        /// </summary>
        /// <param name="inputPath">Input SEModel Path</param>
        /// <param name="outputPath">Output file path</param>
        public override void FromSEModel(string inputPath, string outputPath)
        {
            SEModel model = SEModel.Read(inputPath);

            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                writer.WriteLine("# Exported via SEModelViewer");

                uint globalVertexIndex = 1;

                foreach(var mesh in model.Meshes)
                {
                    foreach(var vertex in mesh.Verticies)
                    {
                        writer.WriteLine("v {0} {1} {2}", 
                            vertex.Position.X, 
                            vertex.Position.Y, 
                            vertex.Position.Z);
                        writer.WriteLine("vn {0} {1} {2}", 
                            vertex.VertexNormal.X, 
                            vertex.VertexNormal.Y, 
                            vertex.VertexNormal.Z);
                        writer.WriteLine("vt {0} {1}", 
                            vertex.UVSets[0].X, 
                            vertex.UVSets[0].Y);
                    }

                    writer.WriteLine("g {0}", 
                        model.Materials[mesh.MaterialReferenceIndicies[0]].Name);
                    writer.WriteLine("usemtl {0}", 
                        model.Materials[mesh.MaterialReferenceIndicies[0]].Name);

                    foreach (var face in mesh.Faces)
                    {
                        writer.Write("f");
                        writer.Write(" {0}/{0}/{0}", 
                            globalVertexIndex + face.FaceIndex3);
                        writer.Write(" {0}/{0}/{0}", 
                            globalVertexIndex + face.FaceIndex2);
                        writer.Write(" {0}/{0}/{0}", 
                            globalVertexIndex + face.FaceIndex1);
                        writer.WriteLine();
                    }

                    globalVertexIndex += mesh.VertexCount;
                }
            }
        }
    }
}
