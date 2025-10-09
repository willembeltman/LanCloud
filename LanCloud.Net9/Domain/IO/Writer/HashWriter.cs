using LanCloud.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace LanCloud.Domain.IO.Writer;

public class HashWriter
{
    public HashWriter(FileWriter fileRefWriter)
    {
        FileRefWriter = fileRefWriter;

        Thread = new Thread(new ThreadStart(Start));
        Thread.Start();
    }

    public FileWriter FileRefWriter { get; }
    public Thread Thread { get; }

    private IncrementalHash IncrementalHash { get; } = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
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

                    IncrementalHash.AppendData(data, 0, datalength);
                }

                WritingIsDone.Set();
            }
        }
    }

    public string Stop()
    {
        if (Thread.CurrentThread == Thread) throw new Exception("Cannot wait for own thread");

        KillSwitch = true;
        StartNext.Set();
        Thread.Join();

        var hashBytes = IncrementalHash.GetHashAndReset();
        var hashString = new StringBuilder(hashBytes.Length * 2);
        foreach (byte b in hashBytes)
            hashString.Append(b.ToString("x2"));
        return hashString.ToString();
    }
}