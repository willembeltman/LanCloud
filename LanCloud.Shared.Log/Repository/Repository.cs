using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LanCloud.Domain
{
    public class Repository<T> where T : class, IEntity
    {
        public Repository(FileInfo dbFile)
        {
            DbFile = dbFile;
        }
        public FileInfo DbFile { get; }

        public IEnumerable<T> List()
        {
            lock (this)
            {
                using (var stream = DbFile.Open(FileMode.OpenOrCreate))
                using (var reader = new BinaryReader(stream))
                {
                    while (stream.Position < stream.Length)
                    {
                        var json = reader.ReadString();
                        var fileRef = JsonConvert.DeserializeObject<T>(json);
                        yield return fileRef;
                    }
                }
            }
        }
        public T Find(string id)
        {
            return List().FirstOrDefault(a => a.Id == id);
        }
        public void Add(T item)
        {
            if (item == null) throw new Exception("Cannot add null");
            if (List().Any(a => a.Id == item.Id)) throw new Exception("Item already exists");

            lock (this)
            {
                using (var stream = DbFile.Open(FileMode.Append))
                using (var writer = new BinaryWriter(stream))
                {
                    var json = JsonConvert.SerializeObject(item);
                    writer.Write(json);
                }
            }
        }
        public void Update(T item)
        {
            if (item == null) throw new Exception("Cannot update null");

            lock (this)
            {
                var tempFile = new FileInfo(DbFile.FullName + ".temp");
                var newJson = JsonConvert.SerializeObject(item);
                var replaced = false;
                using (var tempstream = tempFile.Open(FileMode.OpenOrCreate))
                using (var writer = new BinaryWriter(tempstream))
                using (var stream = DbFile.Open(FileMode.OpenOrCreate))
                using (var reader = new BinaryReader(stream))
                {
                    while (stream.Position < stream.Length)
                    {
                        var json = reader.ReadString();
                        var fileRef = JsonConvert.DeserializeObject<T>(json);
                        if (fileRef.Id == item.Id)
                        {
                            writer.Write(newJson);
                            replaced = true;
                        }
                        else
                        {
                            writer.Write(json);
                        }
                    }
                }
                if (replaced)
                {
                    DbFile.Delete();
                    tempFile.MoveTo(DbFile.FullName);
                }
                else
                {
                    tempFile.Delete();
                    throw new Exception("Item not found");
                }
            }
        }
        public void Remove(T item)
        {
            if (item == null) throw new Exception("Cannot remove null");

            lock (this)
            {
                var tempFile = new FileInfo(DbFile.FullName + ".temp");
                var deleted = false;
                using (var tempstream = tempFile.Open(FileMode.OpenOrCreate))
                using (var writer = new BinaryWriter(tempstream))
                using (var stream = DbFile.Open(FileMode.OpenOrCreate))
                using (var reader = new BinaryReader(stream))
                {
                    while (stream.Position < stream.Length)
                    {
                        var json = reader.ReadString();
                        var fileRef = JsonConvert.DeserializeObject<T>(json);
                        if (fileRef.Id == item.Id)
                        {
                            deleted = true;
                        }
                        else
                        {
                            writer.Write(json);
                        }
                    }
                }
                if (deleted)
                {
                    DbFile.Delete();
                    tempFile.MoveTo(DbFile.FullName);
                }
                else
                {
                    tempFile.Delete();
                    throw new Exception("Item not found");
                }
            }
        }
    }
}
