using LanCloud.Api.Models;
using LanCloud.Shared.Dtos;

namespace LanCloud.Api.Helpers;

public sealed class FileChunkStream : Stream
{
    private readonly Func<long, CancellationToken, IAsyncEnumerable<FileChunkDto>> OpenFileChunksFunc;
    private readonly Entry Entity;
    private readonly CancellationToken Ct;

    private IAsyncEnumerator<FileChunkDto> Enumerator;
    private CancellationTokenSource? EnumeratorCts;
    private byte[]? CurrentBuffer;
    private int CurrentOffset;

    private long CurrentPosition;
    private bool Completed;
    private bool Disposed;

    public FileChunkStream(
        Func<long, CancellationToken, IAsyncEnumerable<FileChunkDto>> openFileChunks,
        Entry entity,
        CancellationToken ct)
    {
        OpenFileChunksFunc = openFileChunks;
        Entity = entity;
        Ct = ct;
        Enumerator = CreateEnumerator(0);
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;

    public override long Length =>
        Entity.FileSystemEntry.Size;

    public override long Position
    {
        get => CurrentPosition;
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
        ObjectDisposedException.ThrowIf(Disposed, this);

        if (buffer.Length == 0)
            return 0;

        while (true)
        {
            if (CurrentBuffer is not null &&
                CurrentOffset < CurrentBuffer.Length)
            {
                var remaining =
                    CurrentBuffer.Length - CurrentOffset;

                var count = Math.Min(
                    remaining,
                    buffer.Length);

                CurrentBuffer
                    .AsMemory(CurrentOffset, count)
                    .CopyTo(buffer);

                CurrentOffset += count;
                CurrentPosition += count;

                return count;
            }

            if (Completed)
                return 0;

            cancellationToken.ThrowIfCancellationRequested();

            if (!await Enumerator.MoveNextAsync())
            {
                Completed = true;
                return 0;
            }

            var chunk = Enumerator.Current;

            if (chunk.Offset > CurrentPosition)
            {
                throw new IOException(
                    $"The chunked stream skipped from position {CurrentPosition} to {chunk.Offset}.");
            }

            var offsetInChunk = 0;
            if (chunk.Offset < CurrentPosition)
            {
                var alreadyRead = CurrentPosition - chunk.Offset;
                if (alreadyRead >= chunk.Data.Length)
                    continue;

                offsetInChunk = checked((int)alreadyRead);
            }

            CurrentBuffer = chunk.Data;
            CurrentOffset = offsetInChunk;
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
        ObjectDisposedException.ThrowIf(Disposed, this);

        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => CurrentPosition + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        if (position < 0)
            throw new IOException("An attempt was made to move the position before the beginning of the stream.");

        if (position == CurrentPosition)
            return CurrentPosition;

        ResetEnumerator(position);

        return CurrentPosition;
    }

    public override void SetLength(long value)
        => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            Disposed = true;

            if (disposing)
            {
                EnumeratorCts?.Cancel();
                Enumerator.DisposeAsync()
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
                EnumeratorCts?.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!Disposed)
        {
            Disposed = true;

            if (EnumeratorCts is not null)
            {
                await EnumeratorCts.CancelAsync();
                EnumeratorCts.Dispose();
            }

            await Enumerator.DisposeAsync();
        }

        GC.SuppressFinalize(this);
        await base.DisposeAsync();
    }

    private void ResetEnumerator(long position)
    {
        EnumeratorCts?.Cancel();
        Enumerator.DisposeAsync()
            .AsTask()
            .GetAwaiter()
            .GetResult();
        EnumeratorCts?.Dispose();

        Enumerator = CreateEnumerator(position);
        CurrentBuffer = null;
        CurrentOffset = 0;
        CurrentPosition = position;
        Completed = false;
    }

    private IAsyncEnumerator<FileChunkDto> CreateEnumerator(long position)
    {
        EnumeratorCts = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        return OpenFileChunksFunc(position, EnumeratorCts.Token)
            .GetAsyncEnumerator(EnumeratorCts.Token);
    }
}
