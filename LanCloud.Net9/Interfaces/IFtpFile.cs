namespace LanCloud.Interfaces;

public interface IFtpFile
{
    string? Path { get; }
    string? Name { get; }
    long? Length { get; }
    DateTime LastWriteTime { get; }
    string? Extention { get; }
    bool Exists { get; }

    void Delete();
    void MoveTo(string toPath);
    Stream? Create();
    Stream? OpenAppend();
    Stream? OpenRead();
}