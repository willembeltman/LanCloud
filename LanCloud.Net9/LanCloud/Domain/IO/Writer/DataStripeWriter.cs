using LanCloud.Domain.Local;
using LanCloud.Domain.Share;
using LanCloud.Interfaces;

namespace LanCloud.Domain.IO.Writer;

public class DataStripeWriter
{
    public DataStripeWriter(FileWriter fileRefWriter, int bufferSize, int index, LocalShare[] localShares)
    {
        FileRefWriter = fileRefWriter;
        BufferSize = bufferSize;
        Buffer = new DoubleBuffer(bufferSize, 1);
        Index = index;
        FileStripeWriters = localShares
            .Select(localShare => new FileStripeWriter(fileRefWriter, localShare, Buffer))
            .ToArray();

        Thread = new Thread(new ThreadStart(Kernel));
        Thread.Start();
    }

    private readonly FileWriter FileRefWriter;

    private int BufferSize;
    private readonly DoubleBuffer Buffer;
    private readonly int Index;
    private readonly FileStripeWriter[] FileStripeWriters;
    private readonly Thread Thread;
    private bool KillSwitch = false;

    public AutoResetEvent WritingIsDone { get; } = new AutoResetEvent(true);
    public AutoResetEvent StartNext { get; } = new AutoResetEvent(false);

    private void Kernel()
    {
        while (!KillSwitch)
        {
            if (StartNext.WaitOne(1000))
            {
                if (!KillSwitch && FileRefWriter.Buffer.ReadBytesWritten > 0)
                {
                    var startposition = FileRefWriter.Buffer.ReadStartPosition;
                    var buffer = FileRefWriter.Buffer.ReadBuffer;
                    var datalength = FileRefWriter.Buffer.ReadBytesWritten;
                    WriteBufferToStream(startposition, buffer,  datalength);
                }

                WritingIsDone.Set();
            }
        }
    }
    private void WriteBufferToStream(long? startPosition, byte[] data, int datalength)
    {
        var start = Convert.ToInt32(BufferSize * Index);
        var end = Convert.ToInt32(BufferSize * (Index + 1));
        var length = end - start;

        Console.WriteLine($"{Buffer.WriteBuffer.Length} - {length}");

        Buffer.WriteStartPosition = startPosition;
        Buffer.WriteBytesWritten = length;
        Array.Copy(data, start, Buffer.WriteBuffer, 0, length);

        FlipBuffer();
    }

    public void FlipBuffer()
    {
        foreach (var fileStripeWriter in FileStripeWriters)
            if (!fileStripeWriter.WritingIsDone.WaitOne(100000))
                throw new Exception("Timeout writing to FileStripeWriter");

        Buffer.Flip();

        foreach (var fileStripeWriter in FileStripeWriters)
            fileStripeWriter.StartNext.Set();
    }

    public IFileStripe[] Stop(long length, string hash)
    {
        if (Thread.CurrentThread == Thread) throw new Exception("Cannot wait for own thread");

        KillSwitch = true;
        StartNext.Set();
        Thread.Join();

        return FileStripeWriters.Select(a => a.Stop(length, hash)).ToArray();
    }
}