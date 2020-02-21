using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace Splawft
{
    internal class MeshDumper
    {
        private static Dictionary<Mesh, MeshData> dumpedMeshes = new Dictionary<Mesh, MeshData>();
        private readonly string outputDirectory;
        private const int DefaultType = 3;

        public MeshDumper(string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            this.outputDirectory = outputDirectory;
        }

        public MeshData DumpMesh(Mesh mesh)
        {
            if (dumpedMeshes.TryGetValue(mesh, out var result))
                return result;

            var hash = HashMesh(mesh);
            result = new MeshData(4300002, hash, DefaultType);
            dumpedMeshes.Add(mesh, result);

            var metaFile = Path.Combine(outputDirectory, hash + ".obj.meta");

            // Create the file, if necessary.
            if (!File.Exists(metaFile))
            {
                var objMesh = new ObjMesh();
                objMesh.AddMesh(mesh);
                File.WriteAllText(Path.Combine(outputDirectory, hash + ".obj"), objMesh.ToString());
                WriteMetaFile(metaFile, mesh.name, result);
            }

            return result;
        }

        private void WriteMetaFile(string fileName, string meshName, MeshData data)
        {
            File.WriteAllText(fileName, $@"fileFormatVersion: 2
guid: {data.Guid}
ModelImporter:
  serializedVersion: 23
  fileIDToRecycleName:
    100000: //RootNode
    100002: {meshName}
    400000: //RootNode
    400002: {meshName}
    2100000: default
    3300000: default
    4300000: default
");
        }

        /// <summary>
        /// A very poorly thought-out implementation of "get me a GUID from a hash"
        /// that somehow, _maybe_, be proper?
        /// </summary>
        private string HashMesh(Mesh mesh)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            using (var md5 = MD5.Create())
            {
                List<Vector3> buffer = new List<Vector3>();

                int vertexHash = 0;
                mesh.GetVertices(buffer);
                bw.Write(buffer.Count);
                for (int i = 0; i < buffer.Count; i++)
                    vertexHash ^= buffer[i].GetHashCode();
                bw.Write(vertexHash);

                int normalsHash = 0;
                mesh.GetNormals(buffer);
                bw.Write(buffer.Count);
                for (int i = 0; i < buffer.Count; i++)
                    normalsHash ^= buffer[i].GetHashCode();
                bw.Write(normalsHash);

                for (int i = 0; i < mesh.subMeshCount; ++i)
                {
                    int trianglesHash = 0;
                    var triangles = mesh.GetTriangles(i);
                    bw.Write(triangles.Length);
                    for (int j = 0; j < triangles.Length; ++j)
                        trianglesHash ^= triangles[i].GetHashCode();
                    bw.Write(trianglesHash);
                }

                int uvHash = 0;
                var uv0 = mesh.uv;
                bw.Write(uv0.Length);
                for (int i = 0; i < uv0.Length; ++i)
                    uvHash ^= uv0[i].GetHashCode();
                bw.Write(uvHash);

                ms.Seek(0, SeekOrigin.Begin);
                var guid = md5.ComputeHash(ms);
                return string.Concat(guid.Select(i => Convert.ToString(i, 16).PadLeft(2, '0')));
            }
        }
    }
}
