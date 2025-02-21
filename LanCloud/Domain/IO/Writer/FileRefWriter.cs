using LanCloud.Domain.Application;
using LanCloud.Domain.FileRef;
using LanCloud.Models;
using LanCloud.Services;
using LanCloud.Shared.Log;
using System;
using System.IO;
using System.Linq;

namespace LanCloud.Domain.IO.Writer
{
    public class FileRefWriter : Stream
    {
        public FileRefWriter(LocalFileRef pathInfo, ILogger logger)
        {
            PathInfo = pathInfo;
            Logger = logger;

            HashWriter = new HashBuffer(this, Logger);
            DataStripeWriters = Application.LocalShareStripes
                .Where(a => a.Indexes.Length == 1)
                .GroupBy(a => a.Indexes.First())
                .Select(sharepart => new DataBuffer(this, sharepart.Key, sharepart.ToArray(), Logger))
                .ToArray();
            ParityStripeWriters = Application.LocalShareStripes
                .Where(a => a.Indexes.Length > 1)
                .GroupBy(a => a.Indexes.ToUniqueKey())
                .Select(sharepart => new ParityBuffer(this, sharepart.ToArray(), Logger))
                .ToArray();

            AllIndexes = Application.LocalShareStripes
                .SelectMany(a => a.Indexes)
                .GroupBy(a => a)
                .Select(a => a.Key)
                .OrderBy(a => a)
                .ToArray();
            Buffer = new DoubleBuffer(AllIndexes.Length);

            Logger.Info($"Opened virtual ftp file: {pathInfo.Name}");
        }

        public LocalFileRef PathInfo { get; }
        public ILogger Logger { get; }
        public DataBuffer[] DataStripeWriters { get; }
        public ParityBuffer[] ParityStripeWriters { get; }
        public HashBuffer HashWriter { get; }
        public int[] AllIndexes { get; }
        public DoubleBuffer Buffer { get; }
        public bool Disposed { get; private set; }
        public override long Position { get; set; }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public LocalApplication Application => PathInfo.Application;

        public override void Write(byte[] buffer, int offset, int count)
        {
            var bytesWritten = 0;

            while (bytesWritten < count)
            {
                var availableSpace = Buffer.WriteBuffer.Length - Buffer.WriteBufferPosition;
                int bytesToWrite = Math.Min(count - bytesWritten, availableSpace);

                Array.Copy(buffer, offset + bytesWritten, Buffer.WriteBuffer, Buffer.WriteBufferPosition, bytesToWrite);

                bytesWritten += bytesToWrite;
                Buffer.WriteBufferPosition += bytesToWrite;
                Position += bytesWritten;

                if (Buffer.WriteBufferPosition >= Buffer.WriteBuffer.Length)
                {
                    FlipBuffer();
                    Buffer.WriteBufferPosition = 0;
                }
            }
        }
        public override void Flush()
        {
            if (Buffer.WriteBufferPosition > 0)
            {
                FlipBuffer();
            }
        }
        private void FlipBuffer()
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
                if (Buffer.WriteBufferPosition > 0)
                {
                    FlipBuffer();
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
                    .Select(a => new FileRefStripeMetadata(a.Indexes))
                    .GroupBy(a => a.GetUniqueIdentifier())
                    .Select(a => a.First())
                    .ToArray();

                // En dan de waardes updaten
                PathInfo.Metadata = new FileRefMetadata(length, hash, stripes);
            }

            base.Dispose(disposing);
        }

        #region Not implemented

        public override long Length => throw new NotImplementedException();

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
}
