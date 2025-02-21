using LanCloud.Domain.FileStripe;
using LanCloud.Domain.Share;
using LanCloud.Models;
using LanCloud.Shared.Log;
using System;
using System.Linq;
using System.Threading;

namespace LanCloud.Domain.IO.Writer
{
    public class ParityBuffer
    {
        public ParityBuffer(FileRefWriter fileRefWriter, LocalShareStripe[] localShareParts, ILogger logger)
        {
            FileRefWriter = fileRefWriter;
            LocalShareParts = localShareParts;
            Logger = logger;

            Buffer = new DoubleBuffer(1);
            Indexes = localShareParts.First().Indexes;
            FileStripeWriters = localShareParts
                .Select(localSharePart => new FileStripeWriter(Buffer, fileRefWriter, localSharePart, logger))
                .ToArray();

            Thread = new Thread(new ThreadStart(Start));
            Thread.Start();
        }

        public FileRefWriter FileRefWriter { get; }
        public LocalShareStripe[] LocalShareParts { get; }
        public ILogger Logger { get; }
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
            Buffer.WriteBufferPosition = maxlength;

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
}