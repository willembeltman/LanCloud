namespace LanCloud.Interfaces;

public interface IFileInfo
{
    bool Exists { get; }
    string Extention { get; }
    long Length { get; }
    string Name { get; }
    string Path { get; }
    string Hash { get; }
    DateTime LastWriteTime { get; }

    void Delete();
    void MoveTo(string toPath);
    Stream? Create();
    //Stream? OpenAppend();
    Stream? OpenRead();
}