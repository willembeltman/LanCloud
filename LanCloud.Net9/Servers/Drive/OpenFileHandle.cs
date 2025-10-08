using LanCloud.Interfaces;
using LanCloud.Services;

namespace LanCloud.Servers.VirtualDrive;

public sealed class OpenFileHandle : IDisposable
{
    private Stream _stream;

    private OpenFileHandle(string path, Stream stream, bool writable, bool isDirectory)
    {
        Path = path;
        _stream = stream;
        CanWrite = writable;
        IsDirectoryHandle = isDirectory;
    }

    public OpenFileHandle(string path, Stream stream, bool writable)
        : this(path, stream, writable, false)
    {
    }

    private OpenFileHandle(string path)
        : this(path, stream: null, writable: false, isDirectory: true)
    {
    }

    public static OpenFileHandle Directory(string path) => new OpenFileHandle(path);

    public long Id { get; set; }
    public string Path { get; }
    public bool CanWrite { get; }
    public bool CanRead => !CanWrite && !IsDirectoryHandle;
    public bool IsDirectoryHandle { get; }
    public Stream Stream => _stream ?? throw new InvalidOperationException("Directory handles do not expose streams.");

    public bool EnsureOffset(long offset, FileSystemService fileSystem)
    {
        if (_stream == null)
        {
            return offset == 0;
        }

        if (offset == _stream.Position)
        {
            return true;
        }

        if (offset < _stream.Position)
        {
            _stream.Dispose();
            _stream = fileSystem.OpenRead(Path);
        }

        var buffer = new byte[8192];
        while (_stream.Position < offset)
        {
            var remaining = offset - _stream.Position;
            var read = _stream.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read == 0)
            {
                break;
            }
        }

        return _stream.Position == offset;
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;
    }
}
