using LanCloud.Domain.Local;
using LanCloud.Domain.Share;
using LanCloud.Interfaces;

namespace LanCloud.Domain.IO.Writer;

public class FileStripeWriter
{
    public FileStripeWriter(FileWriter fileWriter, IShare localShare, DoubleBuffer buffer)
    {
        FileWriter = fileWriter;
        LocalShare = localShare;
        Buffer = buffer;

        FileStripe = LocalShare.CreateFileStripeSession(FileWriter.FileRef.Extention);

        Thread = new Thread(new ThreadStart(Kernel));
        Thread.Start();

        fileWriter.Logger.Info($"Opened {FileStripe.Name} as output for parts: {string.Join(" xor ", Indexes.OrderBy(a => a).Select(a => $"#{a}"))}");
    }

    public FileWriter FileWriter { get; }
    public IShare LocalShare { get; }
    public DoubleBuffer Buffer { get; }
    public IFileStripe FileStripe { get; }

    public Thread Thread { get; }

    public AutoResetEvent WritingIsDone { get; } = new AutoResetEvent(true);
    public AutoResetEvent StartNext { get; } = new AutoResetEvent(false);
    public int Position { get; private set; } = 0;
    private bool KillSwitch { get; set; } = false;

    public int[] Indexes => LocalShare.Indexes;

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

    public IFileStripe Stop(long length, string hash)
    {
        if (Thread.CurrentThread == Thread) throw new Exception("Cannot wait for own thread");

        KillSwitch = true;
        StartNext.Set();
        Thread.Join();

        FileStripe.Update(length, hash);
        LocalShare.AddFileStripe(FileStripe);
        return FileStripe;
    }

}