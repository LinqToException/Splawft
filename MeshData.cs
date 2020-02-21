using System;

namespace Splawft
{
    internal class MeshData
    {
        public int FileId { get; }
        public string Guid { get; }
        public int Type { get; }

        public MeshData(int fileId, string guid, int type)
        {
            this.FileId = fileId;
            this.Guid = guid ?? throw new ArgumentNullException(nameof(guid));
            this.Type = type;
        }
    }
}
