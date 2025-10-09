using LanCloud.Domain.IO.Reader;
using LanCloud.Domain.Local;
using LanCloud.Interfaces;

namespace LanCloud.Domain.IO;

public class FileReader : Stream
{
    public FileReader(LocalFileInfo file, int bufferSize)
    {
        File = file;

        ReconstructBuffer = new ReconstructBuffer(this, bufferSize);

        file.Logger.Info($"Opened virtual file: {file.Name} for reading");
    }

    public LocalFileInfo File { get; }
    public ILogger Logger => File.Logger;
    public ReconstructBuffer ReconstructBuffer { get; }

    public override long Position { get; set; }
    private bool BufferInitialized { get; set; }
    private int BufferPosition { get; set; }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => File.Length;
    public bool Disposed => ReconstructBuffer.Disposed;

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!BufferInitialized)
        {
            ReconstructBuffer.FlipBuffer();
            BufferInitialized = true;
        }

        var read = 0;
        while (read < count && BufferPosition < ReconstructBuffer.Buffer.ReadBytesWritten)
        {
            var availableSpace = ReconstructBuffer.Buffer.ReadBytesWritten - BufferPosition;
            int bytesToWrite = Math.Min(count - read, availableSpace);

            Array.Copy(ReconstructBuffer.Buffer.ReadBuffer, BufferPosition, buffer, offset + read, bytesToWrite);

            read += bytesToWrite;
            BufferPosition += bytesToWrite;
            Position += bytesToWrite;

            if (BufferPosition == ReconstructBuffer.Buffer.ReadBuffer.Length)
            {
                ReconstructBuffer.FlipBuffer();
                BufferPosition = 0;
            }
        }
        return read;
    }

    protected override void Dispose(bool disposing)
    {
        if (!Disposed && disposing)
        {
            ReconstructBuffer.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Not implemented

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }
    public override void Flush()
    {
        throw new NotImplementedException();
    }
    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    #endregion
}
