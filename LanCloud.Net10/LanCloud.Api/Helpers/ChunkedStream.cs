using LanCloud.Api.Models;
using LanCloud.Shared.Dtos;

namespace LanCloud.Api.Helpers;

public sealed class ChunkedStream(
    IAsyncEnumerable<FileChunkDto> fileChunks,
    Entry entity,
    CancellationToken ct)
    : Stream
{
    private readonly IAsyncEnumerator<FileChunkDto> _enumerator =
        fileChunks.GetAsyncEnumerator(ct);

    private byte[]? _currentBuffer;
    private int _currentOffset;

    private long _position;
    private bool _completed;
    private bool _disposed;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;

    public override long Length =>
        entity.FileSystemEntry.Size;

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override int Read(
        byte[] buffer,
        int offset,
        int count)
    {
        return ReadAsync(
                buffer.AsMemory(offset, count),
                CancellationToken.None)
            .AsTask()
            .GetAwaiter()
            .GetResult();
    }

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        return ReadInternalAsync(buffer, cancellationToken);
    }

    private async ValueTask<int> ReadInternalAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (buffer.Length == 0)
            return 0;

        while (true)
        {
            if (_currentBuffer is not null &&
                _currentOffset < _currentBuffer.Length)
            {
                var remaining =
                    _currentBuffer.Length - _currentOffset;

                var count = Math.Min(
                    remaining,
                    buffer.Length);

                _currentBuffer
                    .AsMemory(_currentOffset, count)
                    .CopyTo(buffer);

                _currentOffset += count;
                _position += count;

                return count;
            }

            if (_completed)
                return 0;

            if (!await _enumerator.MoveNextAsync())
            {
                _completed = true;
                return 0;
            }

            var chunk = _enumerator.Current;

            _currentBuffer = chunk.Data;
            _currentOffset = 0;
        }
    }

    public override void Flush()
    {
    }

    public override Task FlushAsync(
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    public override void Write(
        byte[] buffer,
        int offset,
        int count)
        => throw new NotSupportedException();

    public override long Seek(
        long offset,
        SeekOrigin origin)
        => throw new NotSupportedException();

    public override void SetLength(long value)
        => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;

            if (disposing)
            {
                _enumerator.DisposeAsync()
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
            }
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;

            await _enumerator.DisposeAsync();
        }

        GC.SuppressFinalize(this);
        await base.DisposeAsync();
    }
}

//public class ChunkedStream(
//    IAsyncEnumerable<FileChunkDto> fileChunks,
//    Entry entity,
//    CancellationToken ct) 
//    : Stream
//{
//    private int _Position { get; set; }

//    public override bool CanRead => true;
//    public override bool CanSeek => false;
//    public override bool CanWrite => false;
//    public override long Length => entity.FileSystemEntry.Size;
//    public override long Position { get => _Position; set => throw new NotImplementedException(); }

//    public override int Read(byte[] buffer, int offset, int count)
//    {
//        throw new NotImplementedException();
//    }

//    public override void Write(byte[] buffer, int offset, int count)
//        => throw new NotImplementedException();
//    public override long Seek(long offset, SeekOrigin origin) 
//        => throw new NotImplementedException();
//    public override void SetLength(long value) 
//        => throw new NotImplementedException();
//    public override void Flush() 
//        => throw new NotImplementedException();
//}