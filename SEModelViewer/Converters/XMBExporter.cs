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
using System.Text;
using SEModelViewer.Util;
using SELib;
using SELib.Utilities;
using LZ4cc;

namespace SEModelViewer.Converters
{
    /// <summary>
    /// XMODEL BIN Exporter Class
    /// </summary>
    class XMBExporter : ModelConverter
    {
        /// <summary>
        /// Initializes XMODEL_BIN converter and registers it with the converters
        /// </summary>
        public XMBExporter()
        {
            Extension = ".xmodel_bin";
            Description = "XModel Binary File (Call of Duty)";
            Register();
        }

        /// <summary>
        /// 5 byte *LZ4* Magic
        /// </summary>
        private readonly byte[] Magic = { 0x2A, 0x4C, 0x5A, 0x34, 0x2A };

        #region HelperFunctions

        /// <summary>
        /// Handles calculating byte padding, 
        /// from PyCod's xBin py 
        /// (https://github.com/dtzxporter/PyCod/blob/master/PyCod/xbin.py#L24)
        /// </summary>
        private static long CalulatePadding(long size)
        {
            return (size + 0x3) & 0xFFFFFFFFFFFFFC;
        }

        /// <summary>
        /// Clamps a float to 16Bit Int between 32767 and -32768
        /// </summary>
        /// <param name="value">Value to clamp</param>
        /// <returns>Resulting short</returns>
        private static short ClampFloatToShort(double value)
        {
            return (short)Common.Clamp(32767 * value, 32767, -32768);
        }

        /// <summary>
        /// Writes a 16bit data block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="hash">Block hash</param>
        /// <param name="value">Value</param>
        private static void WriteMetaInt16Block(BinaryWriter writer, ushort hash, short value)
        {
            writer.Write(hash);
            writer.Write(value);
        }

        /// <summary>
        /// Writes a 32bit data block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="hash">Block hash</param>
        /// <param name="value">Value</param>
        private static void WriteMetaInt32Block(BinaryWriter writer, int hash, int value)
        {
            writer.Write(hash);
            writer.Write(value);
        }

        /// <summary>
        /// Writes an Unsigned 32bit data block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="hash">Block hash</param>
        /// <param name="value">Value</param>
        private static void WriteMetaUInt32Block(BinaryWriter writer, int hash, uint value)
        {
            writer.Write(hash);
            writer.Write(value);
        }

        /// <summary>
        /// Writes a Float data block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="hash">Block hash</param>
        /// <param name="value">Float value</param>
        private static void WriteMetaFloatBlock(BinaryWriter writer, int hash, float value)
        {
            writer.Write(hash);
            writer.Write(value);
        }

        /// <summary>
        /// Writes a 2 Dimensional Vector data block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="hash">Block hash</param>
        /// <param name="vector">2D Vector</param>
        private static void WriteMetaVec2Block(BinaryWriter writer, int hash, Vector2 vector)
        {
            writer.Write(hash);
            writer.Write((float)vector.X);
            writer.Write((float)vector.Y);
        }

        /// <summary>
        /// Writes a 3 Dimensional Vector data block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="hash">Block hash</param>
        /// <param name="vector">3D Vector</param>
        private static void WriteMetaVec3Block(BinaryWriter writer, int hash, Vector3 vector)
        {
            writer.Write(hash);
            writer.Write((float)vector.X);
            writer.Write((float)vector.Y);
            writer.Write((float)vector.Z);
        }

        /// <summary>
        /// Writes a 4 Dimensional Vector data block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="hash">Block hash</param>
        /// <param name="vector">4D Vector</param>
        private static void WriteMetaVec4Block(BinaryWriter writer, int hash, Vector4 vector)
        {
            writer.Write(hash);
            writer.Write((float)vector.X);
            writer.Write((float)vector.Y);
            writer.Write((float)vector.Z);
            writer.Write((float)vector.W);

        }

        /// <summary>
        /// Writes an aligned string
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="input">String to write</param>
        private static void WriteStringAligned(BinaryWriter writer, string input)
        {
            writer.Write(Encoding.ASCII.GetBytes(input));
            writer.Write((byte)0);
            writer.Write(new byte[(CalulatePadding(input.Length + 1) - (input.Length +1))]);
        }

        /// <summary>
        /// Writes comment block 
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="comment">String to write</param>
        private static void WriteCommentBlock(BinaryWriter writer, string comment)
        {
            writer.Write(0xC355);
            WriteStringAligned(writer, comment);
        }

        /// <summary>
        /// Writes model token block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        private static void WriteModelBlock(BinaryWriter writer)
        {
            writer.Write(0x46C8);
        }

        /// <summary>
        /// Writes model version block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="version">xModel Version</param>
        private static void WriteVersionBlock(BinaryWriter writer, short version)
        {
            WriteMetaInt16Block(writer, 0x24D1, version);
        }

        /// <summary>
        /// Writes Bone Count block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="count">Bone Count</param>
        private static void WriteBoneCountBlock(BinaryWriter writer, short count)
        {
            WriteMetaInt16Block(writer, 0x76BA, count);
        }

        /// <summary>
        /// Writes bone info block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="bone">Bone to write</param>
        /// <param name="index">Bone index</param>
        private static void WriteBoneInfoBlock(BinaryWriter writer, SEModelBone bone, int index)
        {
            writer.Write(0xF099);
            writer.Write(index);
            writer.Write(bone.BoneParent);
            WriteStringAligned(writer, bone.BoneName);
        }

        /// <summary>
        /// Writes bone index block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="index">Bone Index</param>
        private static void WriteBoneIndexBlock(BinaryWriter writer, short index)
        {
            WriteMetaInt16Block(writer, 0xDD9A, index);
        }

        /// <summary>
        /// Writes an XYZ Offset Block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="offset">XYZ Offset</param>
        private static void WriteOffsetBlock(BinaryWriter writer, Vector3 offset)
        {
            writer.Write(0x9383);
            writer.Write((float)Common.CMToInch(offset.X));
            writer.Write((float)Common.CMToInch(offset.Y));
            writer.Write((float)Common.CMToInch(offset.Z));
        }

        /// <summary>
        /// Writes a Rotation Matrix Block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="matrix">Matrix</param>
        private static void WriteMatrixBlock(BinaryWriter writer, Rotation.Matrix matrix)
        {
            writer.Write((ushort)0xDCFD);
            writer.Write(ClampFloatToShort(matrix.X.X));
            writer.Write(ClampFloatToShort(matrix.Y.X));
            writer.Write(ClampFloatToShort(matrix.Z.X));
            writer.Write((ushort)0xCCDC);
            writer.Write(ClampFloatToShort(matrix.X.Y));
            writer.Write(ClampFloatToShort(matrix.Y.Y));
            writer.Write(ClampFloatToShort(matrix.Z.Y));
            writer.Write((ushort)0xFCBF);
            writer.Write(ClampFloatToShort(matrix.X.Z));
            writer.Write(ClampFloatToShort(matrix.Y.Z));
            writer.Write(ClampFloatToShort(matrix.Z.Z));
        }

        /// <summary>
        /// Writes a 32Bit Vertex Count Block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="vertexCount">Vertex Count</param>
        private static void WriteVertexCount32Block(BinaryWriter writer, uint vertexCount)
        {
            WriteMetaUInt32Block(writer, 0x2AEC, vertexCount);
        }

        /// <summary>
        /// Writes a 32Bit Vertex Index Block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="index">Vertex Index</param>
        private static void WriteVertexIndex32Block(BinaryWriter writer, uint index)
        {
            WriteMetaUInt32Block(writer, 0xB097, index);
        }

        /// <summary>
        /// Writes Vertex Weight Count Block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="count">Vertex Weights Count</param>
        private static void WriteVertexWeightCountBlock(BinaryWriter writer, short count)
        {
            WriteMetaInt16Block(writer, 0xEA46, count);
        }

        /// <summary>
        /// Writes Vertex Weight Block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="boneIndex">Bone Index</param>
        /// <param name="weight">Bone Influence</param>
        private static void WriteVertexWeightBlock(BinaryWriter writer, short boneIndex, float weight)
        {
            WriteMetaInt16Block(writer, 0xF1AB, boneIndex);
            writer.Write(weight);
        }

        /// <summary>
        /// Writes Face Info Block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="meshIndex">Mesh Index</param>
        /// <param name="materialIndex">Material Index</param>
        private static void WriteFaceInfoBlock16(BinaryWriter writer, short meshIndex, short materialIndex)
        {
            writer.Write((ushort)0x6711);
            writer.Write((short)0);
            writer.Write(meshIndex);
            writer.Write(materialIndex);
        }

        /// <summary>
        /// Writes Face Vertex Normal Block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="normal">Vertex Normal</param>
        private static void WriteFaceVertexNormalBlock(BinaryWriter writer, Vector3 normal)
        {
            writer.Write((ushort)0x89EC);
            writer.Write(ClampFloatToShort(normal.X));
            writer.Write(ClampFloatToShort(normal.Y));
            writer.Write(ClampFloatToShort(normal.Z));
        }

        /// <summary>
        /// Writes Color Block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="color">Color</param>
        private static void WriteColorBlock(BinaryWriter writer, Color color)
        {
            writer.Write(0x6DD8);
            writer.Write(color.R);
            writer.Write(color.G);
            writer.Write(color.B);
            writer.Write(color.A);
        }

        /// <summary>
        /// Writes Face Vertex UV Block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="layer">UV Layer</param>
        /// <param name="uv">UV Coordinates</param>
        private static void WriteFaceVertexUVBlock(BinaryWriter writer, ushort layer, Vector2 uv)
        {
            writer.Write((ushort)0x1AD4);
            writer.Write(layer);
            writer.Write((float)uv.X);
            writer.Write((float)uv.Y);
        }

        /// <summary>
        /// Writes Face Vertex
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="mesh">Parent Mesh</param>
        /// <param name="vertexIndex">Vertex Index (local to parent mesh)</param>
        private static void WriteFaceVertex(BinaryWriter writer, SEModelMesh mesh, uint vertexIndex, uint offset)
        {
            WriteVertexIndex32Block(writer, offset + vertexIndex);
            WriteFaceVertexNormalBlock(writer, mesh.Verticies[(int)vertexIndex].VertexNormal);
            WriteColorBlock(writer, mesh.Verticies[(int)vertexIndex].VertexColor);
            WriteFaceVertexUVBlock(writer, 1, mesh.Verticies[(int)vertexIndex].UVSets[0]);
        }

        /// <summary>
        /// Writes Object Info Block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="index">Object Index</param>
        /// <param name="name">Object Name</param>
        private static void WriteMetaObjectInfo(BinaryWriter writer, ushort index, string name)
        {
            writer.Write((ushort)0x87D4);
            writer.Write(index);
            WriteStringAligned(writer, name);
        }

        /// <summary>
        /// Writes material info block
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="index">Material Index</param>
        /// <param name="name">Material Name</param>
        /// <param name="type">Material Type</param>
        /// <param name="image">Images</param>
        private static void WriteMaterialInfoBlock(
            BinaryWriter writer, 
            ushort index, 
            string name, 
            string type = "lambert", 
            string image = "no_image.png")
        {
            writer.Write((ushort)0xA700);
            writer.Write(index);
            WriteStringAligned(writer, name);
            WriteStringAligned(writer, type);
            WriteStringAligned(writer, image);
        }

        /// <summary>
        /// Writes Bones and their positions/rotations
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="model">SEModel Object</param>
        private static void WriteBones(BinaryWriter writer, SEModel model)
        {
            WriteBoneCountBlock(writer, (short)model.BoneCount);

            for (int i = 0; i < model.BoneCount; i++)
            {
                WriteBoneInfoBlock(writer, model.Bones[i], i);
            }

            for (short i = 0; i < model.BoneCount; i++)
            {
                WriteBoneIndexBlock(writer, i);
                WriteOffsetBlock(writer, model.Bones[i].GlobalPosition);
                WriteMatrixBlock(writer, Rotation.QuatToMat(model.Bones[i].GlobalRotation));
            }
        }

        /// <summary>
        /// Writes Vertex Positon and Weights from each SEModel Mesh
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="model">SEModel Object</param>
        private static void WriteVertices(BinaryWriter writer, SEModel model)
        {
            uint globalVertexIndex = 0;

            WriteVertexCount32Block(writer, (uint)model.Meshes.Sum(x => x.VertexCount));

            foreach (var mesh in model.Meshes)
            {
                for (uint i = 0; i < mesh.VertexCount; i++)
                {
                    WriteVertexIndex32Block(writer, globalVertexIndex + i);
                    WriteOffsetBlock(writer, mesh.Verticies[(int)i].Position);
                    WriteVertexWeightCountBlock(
                        writer, (short)mesh.Verticies[(int)i].Weights.Count(x => x.BoneWeight != 0.00000));

                    foreach (var weight in mesh.Verticies[(int)i].Weights)
                        if (weight.BoneWeight != 0.00000)
                            WriteVertexWeightBlock(
                                writer, 
                                (short)weight.BoneIndex, 
                                weight.BoneWeight);
                }

                globalVertexIndex += mesh.VertexCount;
            }
        }

        /// <summary>
        /// Writes Faces from each SEModel Mesh
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="model">SEModel Object</param>
        private static void WriteFaces(BinaryWriter writer, SEModel model)
        {
            uint globalVertexIndex = 0;

            WriteMetaUInt32Block(writer, 0xBE92, (uint)model.Meshes.Sum(x => x.FaceCount));

            for (int i = 0; i < model.MeshCount; i++)
            {
                for (uint j = 0; j < model.Meshes[i].FaceCount; j++)
                {
                    WriteFaceInfoBlock16(writer, (short)i, (short)model.Meshes[i].MaterialReferenceIndicies[0]);

                    WriteFaceVertex(
                        writer, 
                        model.Meshes[i], 
                        model.Meshes[i].Faces[(int)j].FaceIndex1, 
                        globalVertexIndex);
                    WriteFaceVertex(
                        writer,
                        model.Meshes[i],
                        model.Meshes[i].Faces[(int)j].FaceIndex2,
                        globalVertexIndex);
                    WriteFaceVertex(
                        writer,
                        model.Meshes[i],
                        model.Meshes[i].Faces[(int)j].FaceIndex3,
                        globalVertexIndex);
                }

                globalVertexIndex += model.Meshes[i].VertexCount;
            }
        }

        /// <summary>
        /// Writes Objects/Meshes
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="objects">Objects/Meshes</param>
        private static void WriteObjects(BinaryWriter writer, List<SEModelMesh> objects)
        {
            WriteMetaInt16Block(writer, 0x62AF, (short)objects.Count);

            for (ushort i = 0; i < objects.Count; i++)
            {
                WriteMetaObjectInfo(writer, i, String.Format("SEModelViewerMesh_{0}", i));
            }
        }

        /// <summary>
        /// Writes Materials
        /// </summary>
        /// <param name="writer">BinaryWriter stream</param>
        /// <param name="materials">SEModel Materials</param>
        private static void WriteMaterials(BinaryWriter writer, List<SEModelMaterial> materials)
        {
            WriteMetaInt16Block(writer, 0xA1B2, (short)materials.Count);

            for (ushort i = 0; i < materials.Count; i++)
            {
                var data = materials[i].MaterialData as SEModelSimpleMaterial;
                WriteMaterialInfoBlock(writer, i, materials[i].Name, "lambert", data.DiffuseMap);
                WriteColorBlock(writer, new Color(255, 255, 255, 255));
                WriteMetaVec4Block(writer, 0x6DAB, new Vector4(0, 0, 0, 1));
                WriteMetaVec4Block(writer, 0x37FF, new Vector4(0, 0, 0, 1));
                WriteMetaVec4Block(writer, 0x4265, new Vector4(0, 0, 0, 1));
                WriteMetaVec2Block(writer, 0xC835, new Vector2(0.8f, 0));
                WriteMetaVec2Block(writer, 0xFE0C, new Vector2(0, 0));
                WriteMetaVec2Block(writer, 0x7E24, new Vector2(6, 1));
                WriteMetaVec4Block(writer, 0x317C, new Vector4(-1, -1, -1, 1));
                WriteMetaVec4Block(writer, 0xE593, new Vector4(-1, -1, -1, 1));
                WriteMetaVec2Block(writer, 0x7D76, new Vector2(-1, -1));
                WriteMetaVec2Block(writer, 0x83C7, new Vector2(-1, -1));
                WriteMetaFloatBlock(writer, 0x5CD2, -1.0f);
            }
        }

        #endregion

        /// <summary>
        /// Converts a SEModel to XMODEL_BIN
        /// </summary>
        /// <param name="inputPath">Input SEModel Path</param>
        /// <param name="outputPath">Output file path</param>
        public override void FromSEModel(string inputPath, string outputPath)
        {
            SEModel model = SEModel.Read(inputPath);

            using (BinaryWriter writer = new BinaryWriter(new MemoryStream(), Encoding.Default, true))
            {
                WriteCommentBlock(writer, "Exported via SEModelViewer by Scobalula");
                WriteCommentBlock(writer, "Export filename: " + outputPath);
                WriteCommentBlock(writer, "Source filename: " + inputPath);
                WriteCommentBlock(writer, "Export time: " + DateTime.Now);
                WriteModelBlock(writer);
                WriteVersionBlock(writer, 7);
                WriteBones(writer, model);
                WriteVertices(writer, model);
                WriteFaces(writer, model);                    
                WriteObjects(writer, model.Meshes);
                WriteMaterials(writer, model.Materials);

                var stream = writer.BaseStream as MemoryStream;

                stream.Flush();
                stream.Position = 0;

                byte[] buffer = stream.ToArray();

                using (BinaryWriter fileWriter = new BinaryWriter(new FileStream(outputPath, FileMode.Create)))
                {
                    fileWriter.Write(Magic);
                    fileWriter.Write(buffer.Length);
                    fileWriter.Write(LZ4Codec.Encode64HC(buffer, 0, buffer.Length));
                }
            }
        }
    }
}
