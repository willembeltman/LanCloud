using LanCloud.Api.Models;
using LanCloud.Shared.Dtos;

namespace LanCloud.Api.Helpers;

public sealed class ChunkedStream : Stream
{
    private readonly Func<long, CancellationToken, IAsyncEnumerable<FileChunkDto>> _openFileChunks;
    private readonly Entry _entity;
    private readonly CancellationToken _ct;

    private IAsyncEnumerator<FileChunkDto> _enumerator;
    private CancellationTokenSource? _enumeratorCts;
    private byte[]? _currentBuffer;
    private int _currentOffset;

    private long _position;
    private bool _completed;
    private bool _disposed;

    public ChunkedStream(
        Func<long, CancellationToken, IAsyncEnumerable<FileChunkDto>> openFileChunks,
        Entry entity,
        CancellationToken ct)
    {
        _openFileChunks = openFileChunks;
        _entity = entity;
        _ct = ct;
        _enumerator = CreateEnumerator(0);
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;

    public override long Length =>
        _entity.FileSystemEntry.Size;

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
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

            cancellationToken.ThrowIfCancellationRequested();

            if (!await _enumerator.MoveNextAsync())
            {
                _completed = true;
                return 0;
            }

            var chunk = _enumerator.Current;

            if (chunk.Offset > _position)
            {
                throw new IOException(
                    $"The chunked stream skipped from position {_position} to {chunk.Offset}.");
            }

            var offsetInChunk = 0;
            if (chunk.Offset < _position)
            {
                var alreadyRead = _position - chunk.Offset;
                if (alreadyRead >= chunk.Data.Length)
                    continue;

                offsetInChunk = checked((int)alreadyRead);
            }

            _currentBuffer = chunk.Data;
            _currentOffset = offsetInChunk;
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
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        if (position < 0)
            throw new IOException("An attempt was made to move the position before the beginning of the stream.");

        if (position == _position)
            return _position;

        ResetEnumerator(position);

        return _position;
    }

    public override void SetLength(long value)
        => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;

            if (disposing)
            {
                _enumeratorCts?.Cancel();
                _enumerator.DisposeAsync()
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
                _enumeratorCts?.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;

            if (_enumeratorCts is not null)
            {
                await _enumeratorCts.CancelAsync();
                _enumeratorCts.Dispose();
            }

            await _enumerator.DisposeAsync();
        }

        GC.SuppressFinalize(this);
        await base.DisposeAsync();
    }

    private void ResetEnumerator(long position)
    {
        _enumeratorCts?.Cancel();
        _enumerator.DisposeAsync()
            .AsTask()
            .GetAwaiter()
            .GetResult();
        _enumeratorCts?.Dispose();

        _enumerator = CreateEnumerator(position);
        _currentBuffer = null;
        _currentOffset = 0;
        _position = position;
        _completed = false;
    }

    private IAsyncEnumerator<FileChunkDto> CreateEnumerator(long position)
    {
        _enumeratorCts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        return _openFileChunks(position, _enumeratorCts.Token)
            .GetAsyncEnumerator(_enumeratorCts.Token);
    }
}
