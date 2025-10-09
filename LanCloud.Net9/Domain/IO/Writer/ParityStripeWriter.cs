using LanCloud.Domain.Local;
using LanCloud.Domain.Share;
using LanCloud.Interfaces;

namespace LanCloud.Domain.IO.Writer;

public class ParityStripeWriter
{
    public ParityStripeWriter(FileWriter fileRefWriter, int bufferSize, LocalShare[] localShares)
    {
        FileRefWriter = fileRefWriter;
        LocalShares = localShares;

        Buffer = new DoubleBuffer(bufferSize, 1);
        Indexes = localShares.First().Indexes;
        FileStripeWriters = localShares
            .Select(localSharePart => new FileStripeWriter(fileRefWriter, localSharePart, Buffer))
            .ToArray();

        Thread = new Thread(new ThreadStart(Start));
        Thread.Start();
    }

    public FileWriter FileRefWriter { get; }
    public LocalShare[] LocalShares { get; }
    public DoubleBuffer Buffer { get; }
    public int[] Indexes { get; }
    public FileStripeWriter[] FileStripeWriters { get; }

    public Thread Thread { get; }

    public AutoResetEvent WritingIsDone { get; } = new AutoResetEvent(true);
    public AutoResetEvent StartNext { get; } = new AutoResetEvent(false);
    private bool KillSwitch { get; set; } = false;

    private void Start()
    {
        while (!KillSwitch)
        {
            if (StartNext.WaitOne(1000))
            {
                if (!KillSwitch && FileRefWriter.Buffer.ReadBytesWritten > 0)
                {
                    var data = FileRefWriter.Buffer.ReadBuffer;
                    var datalength = FileRefWriter.Buffer.ReadBytesWritten;
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
        var maxlength = 0;
        var buffer = Buffer.WriteBuffer;

        // Prepare buffer
        Array.Clear(buffer, 0, buffer.Length);

        // XOR data from indexes on to own buffer
        foreach (var index in Indexes)
        {
            var start = Convert.ToInt32(sublength * index);
            var end = Convert.ToInt32(sublength * (index + 1));
            var length = end - start;
            if (length > maxlength) maxlength = length;

            for (var i = 0; i < length; i++)
            {
                buffer[i] ^= data[start + i];
            }
        }

        // Set position
        Buffer.WriteBytesWritten = maxlength;

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