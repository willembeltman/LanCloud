namespace LanCloud.Interfaces;

public interface IFileDirectory
{
    bool Exists { get; }

    string Path { get; }
    DateTime LastWriteTime { get; }
    string Name { get; }

    void Create();
    void Delete();
    void MoveTo(string pathTo);

    IFileDirectory[] GetDirectories();
    IFile[] GetFiles();
}