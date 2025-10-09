using LanCloud.Domain.Local;
using LanCloud.Domain.Share;
using LanCloud.Interfaces;

namespace LanCloud.Domain.IO.Writer;

public class FileStripeWriter
{
    public FileStripeWriter(FileWriter fileRefWriter, LocalShareStripe localSharePart, DoubleBuffer buffer)
    {
        FileRefWriter = fileRefWriter;
        LocalShareStripe = localSharePart;
        Buffer = buffer;

        FileStripe = LocalShareStripe.CreateFileStripeSession(FileRefWriter.FileRef.Extention);

        Thread = new Thread(new ThreadStart(Kernel));
        Thread.Start();

        fileRefWriter.Logger.Info($"Opened {FileStripe.Info.Name} as output for parts: {string.Join(" xor ", Indexes.OrderBy(a => a).Select(a => $"#{a}"))}");
    }

    public FileWriter FileRefWriter { get; }
    public LocalShareStripe LocalShareStripe { get; }
    public DoubleBuffer Buffer { get; }
    public LocalFileStripe FileStripe { get; }

    public Thread Thread { get; }

    public AutoResetEvent WritingIsDone { get; } = new AutoResetEvent(true);
    public AutoResetEvent StartNext { get; } = new AutoResetEvent(false);
    public int Position { get; private set; } = 0;
    private bool KillSwitch { get; set; } = false;

    public int[] Indexes => LocalShareStripe.Indexes;

    private void Kernel()
    {
        using (var stream = FileStripe.OpenWrite())
        {
            while (!KillSwitch)
            {
                if (StartNext.WaitOne(100))
                {
                    if (!KillSwitch && Buffer.ReadBytesWritten > 0)
                    {
                        var data = Buffer.ReadBuffer;
                        var datalength = Buffer.ReadBytesWritten;

                        stream.Write(data, 0, datalength);
                        Position += datalength;
                    }

                    WritingIsDone.Set();
                }
            }
        }
    }

    public LocalFileStripe Stop(long length, string hash)
    {
        if (Thread.CurrentThread == Thread) throw new Exception("Cannot wait for own thread");

        KillSwitch = true;
        StartNext.Set();
        Thread.Join();

        FileStripe.Update(length, hash);
        LocalShareStripe.LocalShare.AddFileStripe(FileStripe);
        return FileStripe;
    }

}