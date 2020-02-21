using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Splawft
{
    /// <summary>
    /// Allows serialization of <see cref="Mesh"/> into a very rough,
    /// non-optimized .obj that mirrors Unity's internal representation
    /// exactly.
    /// Scaling and rotation of the mesh will be taken into account when dumping.
    /// </summary>
    internal class ObjMesh
    {
        public int Vertices { get; private set; } = 1;
        public int Meshes { get; private set; } = 0;

        private StringBuilder meshBuilder = new StringBuilder();
        private StringBuilder groupBuilder = new StringBuilder();
        private List<Vector3> vertices = new List<Vector3>();
        private int uvIndex = 1;

        public void AddMesh(Mesh mesh, Transform transform = null)
        {
            var offset = Vertices;
            List<int> triangles = new List<int>();

            mesh.GetVertices(vertices);
            Vertices += vertices.Count;
            if (transform != null)
            {
                for (int i = 0; i < vertices.Count; ++i)
                    vertices[i] = transform.TransformPoint(vertices[i]);
            }

            foreach (var v in vertices)
                meshBuilder.AppendLine($"v {-v.x:0.00000} {v.y:0.00000} {v.z:0.00000}");
            meshBuilder.AppendLine();

            mesh.GetNormals(vertices);
            if (transform != null)
            {
                for (int i = 0; i < vertices.Count; ++i)
                    vertices[i] = transform.TransformDirection(vertices[i]);
            }

            foreach (var v in vertices)
                meshBuilder.AppendLine($"vn {-v.x:0.00000} {v.y:0.00000} {v.z:0.00000}");
            meshBuilder.AppendLine();

            mesh.GetUVs(0, vertices);
            foreach (var v in vertices)
                meshBuilder.AppendLine($"vt {v.x} {v.y}");
            meshBuilder.AppendLine();

            bool hasUV = vertices.Count > 0;
            var uvOffset = this.uvIndex;
            this.uvIndex += vertices.Count;

            string V(int i) => hasUV ? $"{i + offset}/{i + uvOffset}/{i + offset}" : $"{i + offset}//{i + offset}";
            for (int m = 0; m < mesh.subMeshCount; ++m)
            {
                groupBuilder.AppendLine($"g {(transform?.name ?? "submesh")}_{Meshes + m}");

                mesh.GetTriangles(triangles, m);
                for (int i = 0; i < triangles.Count; i += 3)
                {
                    var c = triangles[i];
                    var b = triangles[i + 1];
                    var a = triangles[i + 2];
                    groupBuilder.AppendLine($"f {V(a)} {V(b)} {V(c)}");
                }

                groupBuilder.AppendLine();
            }

            Meshes += mesh.subMeshCount;
        }

        public override string ToString() => meshBuilder.ToString() + groupBuilder.ToString();
    }
}
