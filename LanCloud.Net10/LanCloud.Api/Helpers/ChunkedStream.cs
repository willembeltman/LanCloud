using LanCloud.Api.Models;
using LanCloud.Shared.Dtos;

namespace LanCloud.Api.Helpers;

public class ChunkedStream(
    IAsyncEnumerable<FileChunkDto> fileChunks,
    Entry entity,
    CancellationToken ct) 
    : Stream
{
    private int _Position { get; set; }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => entity.FileSystemEntry.Size;
    public override long Position { get => _Position; set => throw new NotImplementedException(); }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotImplementedException();
    public override long Seek(long offset, SeekOrigin origin) 
        => throw new NotImplementedException();
    public override void SetLength(long value) 
        => throw new NotImplementedException();
    public override void Flush() 
        => throw new NotImplementedException();
}