using LanCloud.Domain.IO;

namespace LanCloud.Repositories
{
    public static class FileRefRepository
    {
        public static FileMetadata Save(FileInfo fileInfo, FileMetadata fileRef)
        {
            if (!fileInfo.Exists) fileInfo.Delete();
            using (var stream = fileInfo.OpenWrite())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(fileRef.BufferSize);
                writer.Write(fileRef.Length);
                writer.Write(fileRef.Hash);
                writer.Write(Convert.ToByte(fileRef.Stripes?.Length ?? 0));
                if (fileRef.Stripes != null)
                {
                    foreach (var bit in fileRef.Stripes)
                    {
                        writer.Write(Convert.ToByte(bit.Indexes.Length));
                        foreach (var index in bit.Indexes)
                        {
                            writer.Write(Convert.ToByte(index));
                        }
                    }
                }
            }
            return fileRef;
        }

        public static FileMetadata? Load(FileInfo fileInfo)
        {
            if (!fileInfo.Exists) return null;
            using (var stream = fileInfo.OpenRead())
            using (var reader = new BinaryReader(stream))
            {
                var bufferSize = reader.ReadInt32();
                var length = reader.ReadInt64();
                var hash = reader.ReadString();
                var stripes = new FileStripeMetadata[reader.ReadByte()];
                for (int i = 0; i < stripes.Length; i++)
                {
                    var Indexes = new int[reader.ReadByte()];
                    for (int j = 0; j < Indexes.Length; j++)
                    {
                        Indexes[j] = reader.ReadByte();
                    }
                    stripes[i] = new FileStripeMetadata(Indexes);
                }
                return new FileMetadata(bufferSize, length, hash, stripes);
            }
        }
    }
}
