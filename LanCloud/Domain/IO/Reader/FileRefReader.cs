using LanCloud.Domain.FileRef;
using LanCloud.Shared.Log;
using System;
using System.IO;

namespace LanCloud.Domain.IO.Reader
{
    public class FileRefReader : Stream
    {
        public FileRefReader(LocalFileRef pathInfo, ILogger logger)
        {
            PathInfo = pathInfo;
            Logger = logger;

            ReconstructBuffer = new ReconstructBuffer(this, logger);
        }

        public LocalFileRef PathInfo { get; }
        public ILogger Logger { get; }
        public ReconstructBuffer ReconstructBuffer { get; }

        public override long Position { get; set; }
        private bool BufferInitialized { get; set; }
        private int BufferPosition { get; set; }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => PathInfo.Length.Value;
        public bool Disposed => ReconstructBuffer.Disposed;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!BufferInitialized)
            {
                ReconstructBuffer.FlipBuffer();
                BufferInitialized = true;
            }

            var read = 0;
            while (read < count && BufferPosition < ReconstructBuffer.Buffer.ReadBufferPosition)
            {
                var availableSpace = ReconstructBuffer.Buffer.ReadBufferPosition - BufferPosition;
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
}
