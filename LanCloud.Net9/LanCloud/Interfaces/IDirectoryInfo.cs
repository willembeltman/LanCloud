namespace LanCloud.Interfaces;

public interface IDirectoryInfo
{
    bool Exists { get; }

    string Path { get; }
    DateTime LastWriteTime { get; }
    string Name { get; }

    void Create();
    void Delete();
    void MoveTo(string pathTo);

    IDirectoryInfo[] GetDirectories();
    IFileInfo[] GetFiles();
}