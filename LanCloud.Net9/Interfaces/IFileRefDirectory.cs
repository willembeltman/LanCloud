namespace LanCloud.Domain.FileRef;

public interface IFileRefDirectory
{
    bool Exists { get; }

    string Path { get; }
    DateTime LastWriteTime { get; }
    string Name { get; }

    void Create();
    void Delete();
    void MoveTo(string pathTo);

    IFileRefDirectory[] GetDirectories();
    IFileRef[] GetFiles();
}