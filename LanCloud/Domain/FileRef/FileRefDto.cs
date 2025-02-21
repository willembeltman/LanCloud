using System;

namespace LanCloud.Domain.FileRef
{
    public class FileRefDto
    {
        public FileRefDto() { }
        public FileRefDto(IFileRef fileRef)
        {
            Path = fileRef.Path;
            Exists = fileRef.Exists;
            Hash = fileRef.Hash;
            Length = fileRef.Length;
            LastWriteTime = fileRef.LastWriteTime;
        }

        public string Path { get; set; }
        public bool Exists { get; set; }
        public string Hash { get; }
        public long? Length { get; set; }
        public DateTime LastWriteTime { get; set; }
    }
}
