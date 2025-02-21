using LanCloud.Services;
using System;
using System.Data;
using System.IO;
using System.Linq;

namespace LanCloud.Domain.FileStripe
{
    public class LocalFileStripe : IFileStripe
    {
        public LocalFileStripe(FileInfo info)
        {
            Info = info;
            FullName = info.FullName;
            Name = info.Name;
            var split = info.Name.Split('.');
            Extention = split[0];
            Hash = split[1];
            Length = Convert.ToInt64(split[2]);
            Indexes = split[3].Split('_').Select(a => Convert.ToInt32(a)).ToArray();
        }
        public LocalFileStripe(DirectoryInfo dirinfo, string extention, int[] indexes)
        {
            Extention = extention;
            Indexes = indexes;
            Name = CreateTempFileName(Extention, Indexes);
            FullName = Path.Combine(dirinfo.FullName, Name);
            Info = new FileInfo(FullName);
            IsTemp = true;
        }
        public LocalFileStripe(DirectoryInfo dirinfo, string extention, int[] indexes, long length, string hash)
        {
            Extention = extention;
            Indexes = indexes;
            Length = length;
            Hash = hash;
            Name = CreateFileName(Extention, Hash, Length, Indexes);
            FullName = Path.Combine(dirinfo.FullName, Name);
            Info = new FileInfo(FullName);
            IsTemp = false;
        }

        public string Extention { get; }
        public int[] Indexes { get; }
        public string Name { get; }
        public FileInfo Info { get; private set; }
        public string FullName { get; private set; }
        public long Length { get; private set; }
        public string Hash { get; private set; }
        public bool IsTemp { get; private set; }

        public void Update(long length, string hash)
        {
            Length = length;
            Hash = hash;
            var name = CreateFileName(Extention, Hash, Length, Indexes);
            FullName = Path.Combine(Info.Directory.FullName, name);
            if (File.Exists(FullName))
                File.Delete(Info.FullName);
            else
                File.Move(Info.FullName, FullName);
            Info = new FileInfo(FullName);
            IsTemp = false;
        }

        public FileStream OpenRead()
        {
            return Info.OpenRead();
        }
        public FileStream OpenWrite()
        {
            return Info.OpenWrite();
        }

        public static string CreateFileName(string extention, string hash, long length, int[] indexes)
        {
            return $"{extention}.{hash}.{length}.{indexes.ToUniqueKey()}.filestripe";
        }
        public static string CreateTempFileName(string extention, int[] indexes)
        {
            return $"{extention}.{Guid.NewGuid().ToString()}.{indexes.ToUniqueKey()}.tempbit";
        }
    }
}