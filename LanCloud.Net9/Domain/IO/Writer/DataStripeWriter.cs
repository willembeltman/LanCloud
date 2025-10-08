using LanCloud.Domain.FileStripe;
using LanCloud.Domain.Share;
using LanCloud.Interfaces;

namespace LanCloud.Domain.IO.Writer;

public class DataStripeWriter
{
    public DataStripeWriter(FileRefWriter fileRefWriter, int bufferSize, int index, LocalShareStripe[] localShareStripes)
    {
        FileRefWriter = fileRefWriter;
        Buffer = new DoubleBuffer(bufferSize, 1);
        Index = index;
        LocalShareStripes = localShareStripes;
                FileStripeWriters = localShareStripes
            .Select(localSharePart => new FileStripeWriter(fileRefWriter, localSharePart, Buffer))
            .ToArray();

        Thread = new Thread(new ThreadStart(Start));
        Thread.Start();
    }

    private readonly FileRefWriter FileRefWriter;
    private readonly DoubleBuffer Buffer;
    private readonly int Index;
    private readonly LocalShareStripe[] LocalShareStripes;
    private readonly FileStripeWriter[] FileStripeWriters;
    private readonly Thread Thread;
    private bool KillSwitch = false;


    public AutoResetEvent WritingIsDone { get; } = new AutoResetEvent(true);
    public AutoResetEvent StartNext { get; } = new AutoResetEvent(false);

    private void Start()
    {
        while (!KillSwitch)
        {
            if (StartNext.WaitOne(1000))
            {
                if (!KillSwitch && FileRefWriter.Buffer.ReadBufferPosition > 0)
                {
                    var data = FileRefWriter.Buffer.ReadBuffer;
                    var datalength = FileRefWriter.Buffer.ReadBufferPosition;
                    var width = FileRefWriter.Buffer.Width;

                    WriteBufferToStream(data, datalength, width);
                }

                WritingIsDone.Set();
            }
        }
    }
    private void WriteBufferToStream(byte[] data, int datalength, int width)
    {
        var sublength = Convert.ToDouble(datalength) / width;
        var start = Convert.ToInt32(sublength * Index);
        var end = Convert.ToInt32(sublength * (Index + 1));
        var length = end - start;

        Console.WriteLine($"{Buffer.WriteBuffer.Length} - {length}");

        Array.Copy(data, start, Buffer.WriteBuffer, 0, length);
        Buffer.WriteBufferPosition = length;

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

    public LocalFileStripe[] Stop(long length, string hash)
    {
        if (Thread.CurrentThread == Thread) throw new Exception("Cannot wait for own thread");

        KillSwitch = true;
        StartNext.Set();
        Thread.Join();

        return FileStripeWriters.Select(a => a.Stop(length, hash)).ToArray();
    }
}