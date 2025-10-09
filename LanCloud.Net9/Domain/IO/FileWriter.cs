using LanCloud.Domain.Application;
using LanCloud.Domain.IO.Writer;
using LanCloud.Domain.Local;
using LanCloud.Interfaces;

namespace LanCloud.Domain.IO;

public class FileWriter : Stream
{
    public FileWriter(LocalApplication application, LocalFile fileRef, int bufferSize)
    {
        Application = application;
        FileRef = fileRef;
        BufferSize = bufferSize;

        _Length = fileRef.Length;

        HashWriter = new HashWriter(this);
        DataStripeWriters = Application.LocalShareStripes
            .Where(a => a.Indexes.Length == 1)
            .GroupBy(a => a.Indexes.First())
            .Select(sharepart => new DataStripeWriter(this, bufferSize, sharepart.Key, sharepart.ToArray()))
            .ToArray();
        ParityStripeWriters = Application.LocalShareStripes
            .Where(a => a.Indexes.Length > 1)
            .GroupBy(a => a.Indexes.ToUniqueKey())
            .Select(sharepart => new ParityStripeWriter(this, bufferSize, sharepart.ToArray()))
            .ToArray();
        AllIndexes = Application.LocalShareStripes
            .SelectMany(a => a.Indexes)
            .GroupBy(a => a)
            .Select(a => a.Key)
            .OrderBy(a => a)
            .ToArray();
        Buffer = new DoubleBuffer(bufferSize, AllIndexes.Length);

        fileRef.Logger.Info($"Opened virtual ftp file: {fileRef.Name}");
    }

    private long _Length { get; set; }
    private DataStripeWriter[] DataStripeWriters;
    private ParityStripeWriter[] ParityStripeWriters;
    private HashWriter HashWriter;
    private int[] AllIndexes;
    private bool Disposed;

    public LocalApplication Application { get; }
    public LocalFile FileRef { get; }
    public int BufferSize { get; }
    public DoubleBuffer Buffer { get; }

    public override long Position { get; set; }
    public override long Length => throw new NotImplementedException();
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;

    public ILogger Logger => FileRef.Logger;

    public override void Write(byte[] buffer, int offset, int count)
    {
        var bytesWritten = 0;

        Buffer.WriteStartPosition = Position;
        while (bytesWritten < count)
        {
            var availableSpace = Buffer.WriteBuffer.Length - Buffer.WriteBytesWritten;
            var totalBytesToWrite = count - bytesWritten;
            int bytesToWrite = Math.Min(totalBytesToWrite, availableSpace);

            Array.Copy(buffer, offset + bytesWritten, Buffer.WriteBuffer, Buffer.WriteBytesWritten, bytesToWrite);

            bytesWritten += bytesToWrite;
            Buffer.WriteBytesWritten += bytesToWrite;
            Position += bytesWritten;

            if (Buffer.WriteBytesWritten >= Buffer.WriteBuffer.Length)
            {
                StartNext();
                Buffer.WriteBytesWritten = 0;
                Buffer.WriteStartPosition = null;
            }
        }
    }
    public override void Flush()
    {
        if (Buffer.WriteBytesWritten > 0)
        {
            StartNext();
        }
    }
    private void StartNext()
    {
        WaitForDone();

        Buffer.Flip();

        HashWriter.StartNext.Set();

        foreach (var item in DataStripeWriters)
            item.StartNext.Set();

        foreach (var item in ParityStripeWriters)
            item.StartNext.Set();
    }
    private void WaitForDone()
    {
        if (!HashWriter.WritingIsDone.WaitOne(100000))
            throw new Exception("Timeout writing to HashWriter");

        foreach (var item in DataStripeWriters)
            if (!item.WritingIsDone.WaitOne(100000))
                throw new Exception("Timeout writing to DataBitWriters");

        foreach (var item in ParityStripeWriters)
            if (!item.WritingIsDone.WaitOne(100000))
                throw new Exception("Timeout writing to ParityBitWriters");
    }

    protected override void Dispose(bool disposing)
    {
        if (!Disposed && disposing)
        {
            Disposed = true;

            // Eventueel de laatste buffer wegschrijven
            if (Buffer.WriteBytesWritten > 0)
            {
                StartNext();
            }
            WaitForDone();

            // Waardes ophalen
            var length = Position;
            var hash = HashWriter.Stop();
            var dataStripes = DataStripeWriters
                .SelectMany(a => a.Stop(length, hash))
                .ToArray();
            var parityStripes = ParityStripeWriters
                .SelectMany(a => a.Stop(length, hash))
                .ToArray();

            // Stripes samenstellen
            var stripes = dataStripes
                .Concat(parityStripes)
                .Select(a => new FileStripeMetadata(a.Indexes))
                .GroupBy(a => a.GetUniqueIdentifier())
                .Select(a => a.First())
                .ToArray();

            // En dan de waardes updaten
            var metadata = new FileMetadata(BufferSize, length, hash, stripes);
            FileRef.SaveMetadata(metadata);
        }

        base.Dispose(disposing);
    }

    #region Not implemented


    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    #endregion
}
