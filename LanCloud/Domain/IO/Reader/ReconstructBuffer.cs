using LanCloud.Domain.Application;
using LanCloud.Domain.FileRef;
using LanCloud.Models;
using LanCloud.Services;
using LanCloud.Shared.Log;
using System;
using System.Linq;
using System.Threading;

namespace LanCloud.Domain.IO.Reader
{
    public class ReconstructBuffer :  IDisposable
    {
        public ReconstructBuffer(FileRefReader fileRefReader, ILogger logger)
        {
            FileRefReader = fileRefReader;
            Logger = logger;

            FileStripeReaders = FileRef.Stripes
                .Select(fileRefBit =>
                {
                    var fileStripe = Application
                        .FindFileStripes(PathInfo.Extention, FileRef, fileRefBit)
                        .FirstOrDefault();
                    return new FileStripeReader(this, fileStripe, Logger);
                })
                .Where(fileStripe => fileStripe.Exception == null)
                .OrderBy(a => a.Indexes.Length)
                .ThenBy(a => a.Indexes.OrderBy(b => b).First())
                .ToArray();
            AllIndexes = FileStripeReaders
                .SelectMany(a => a.Indexes)
                .GroupBy(a => a)
                .Select(a => a.Key)
                .OrderBy(a => a)
                .ToArray();
            Buffer = new DoubleBuffer(AllIndexes.Length);

            Thread = new Thread(new ThreadStart(Start));
            Thread.Start();
        }

        public FileRefReader FileRefReader { get; }
        public ILogger Logger { get; }
        internal FileStripeReader[] FileStripeReaders { get; }
        public int[] AllIndexes { get; }
        public DoubleBuffer Buffer { get; }
        public Thread Thread { get; }
        public bool Disposed { get; private set; }
        private int BufferReadCounter { get; set; }

        private AutoResetEvent StartNext { get; } = new AutoResetEvent(true);
        private AutoResetEvent BufferIsWritten { get; } = new AutoResetEvent(false);

        public LocalFileRef PathInfo => FileRefReader.PathInfo;
        public LocalApplication Application => PathInfo.Application;
        public FileRefMetadata FileRef => PathInfo.Metadata;
        bool KillSwitch { get; set; }

        public void FlipBuffer()
        {
            BufferIsWritten.WaitOne();
            Buffer.Flip();
            StartNext.Set();
        }

        private void Start()
        {
            while (!KillSwitch)
            {
                if (StartNext.WaitOne(100))
                {
                    Buffer.WriteBufferPosition = 0;

                    var goodReaders = FileStripeReaders.Where(a => a.Exception == null).ToArray();
                    if (goodReaders.Length < AllIndexes.Length)
                    {
                        throw GeneralException();
                    }
                    if (goodReaders.Length >= AllIndexes.Length)
                    {
                        ReadAll(goodReaders);
                    }

                    BufferReadCounter++;
                    BufferIsWritten.Set();
                }
            }
        }
        private void ReadAll(FileStripeReader[] goodReaders)
        {
            foreach (var item in goodReaders)
            {
                item.FlipBuffer(BufferReadCounter);
            }
            goodReaders = goodReaders.Where(a => a.Exception == null).ToArray();

            var normals = AllIndexes
                .Select(index => new
                {
                    Index = index,
                    Readers = goodReaders
                        .Where(a => 
                            a.Indexes.Length == 1 && 
                            a.Indexes.First() == index)
                        .ToArray()
                })
                .Where(a => a.Readers.Any())
                .OrderBy(a => a.Index)
                .ToArray();

            if (normals.Length == AllIndexes.Length)
            {
                foreach (var item in normals)
                {
                    ReadItem(item.Readers
                        .OrderByDescending(a => a.Speed)
                        .FirstOrDefault());
                }
            }
            else
            {
                var parityReaders = goodReaders
                    .Where(reader => !normals.Any(normal => normal.Readers.Any(normalReader => normalReader == reader)))
                    .ToArray();

                var missingIndexes = AllIndexes
                    .Where(index => !normals.Any(b => b.Index == index))
                    .ToArray();

                var missingIndexReaders = missingIndexes
                    .Select(missingIndex => new
                    {
                        MissingIndex = missingIndex,
                        Readers = parityReaders
                            .Where(other =>
                                other.Indexes.Length > 1 &&
                                other.Indexes.Contains(missingIndex) &&
                                other.Indexes.Any(otherIndex => normals.Any(foundNormal => foundNormal.Index == otherIndex)))
                            .ToArray()
                    })
                    .Where(a => a.Readers.Any())
                    .ToArray();

                if (missingIndexReaders.Length == missingIndexes.Length)
                {
                    foreach (var index in AllIndexes)
                    {
                        var normal = normals.FirstOrDefault(a => a.Index == index);
                        if (normal != null)
                        {
                            ReadItem(normal.Readers
                                .OrderByDescending(a => a.Speed)
                                .FirstOrDefault());
                        }
                        else
                        {
                            var parity = missingIndexReaders
                                .FirstOrDefault(a => a.MissingIndex == index);
                            if (parity != null)
                            {
                                var parityreader = parity.Readers
                                    .OrderByDescending(a => a.Speed)
                                    .FirstOrDefault(a => a.Indexes.Any(b => normals.Any(c => c.Index == b)));
                                if (parityreader != null)
                                {
                                    var buffer = parityreader.Buffer.ReadBuffer;
                                    var length = parityreader.Buffer.ReadBufferPosition;

                                    var otherNormals = normals
                                        .Where(foundNormal => parityreader.Indexes.Contains(foundNormal.Index))
                                        .ToArray();
                                    if (otherNormals.Length + 1 == parityreader.Indexes.Length)
                                    {
                                        // XOR data from indexes on to own buffer
                                        foreach (var other in otherNormals)
                                        {
                                            var otherReader = other.Readers
                                                .OrderByDescending(a => a.Speed)
                                                .FirstOrDefault();
                                            var otherBuffer = otherReader.Buffer.ReadBuffer;
                                            for (var i = 0; i < length; i++)
                                            {
                                                buffer[i] ^= otherBuffer[i];
                                            }
                                        }

                                        Array.Copy(buffer, 0, Buffer.WriteBuffer, Buffer.WriteBufferPosition, length);

                                        Buffer.WriteBufferPosition += length;
                                    }
                                    else
                                    {
                                        throw GeneralException();
                                    }
                                }
                                else
                                {
                                    throw GeneralException();
                                }
                            }
                            else
                            {
                                throw GeneralException();
                            }
                        }
                    }
                }
                else
                {
                    throw GeneralException();
                }
            }
        }

        private void ReadItem(FileStripeReader item)
        {
            if (item.Buffer.ReadBufferPosition > 0)
            {
                var length = item.Buffer.ReadBufferPosition;

                Array.Copy(item.Buffer.ReadBuffer, 0, Buffer.WriteBuffer, Buffer.WriteBufferPosition, length);

                Buffer.WriteBufferPosition += length;
            }
        }

        private Exception GeneralException()
        {
            var badReaders = FileStripeReaders.Where(a => a.Exception != null).ToArray();
            var message = $"Data is lost...{Environment.NewLine}";
            message += $"Path: {PathInfo.Path}{Environment.NewLine}";
            foreach (var badReader in badReaders)
            {
                message += $"{Environment.NewLine}";
                message += $"Indexes: {badReader.FileStripe.Indexes.ToUniqueKey()}{Environment.NewLine}";
                //message += $"FilePart: {badReader.FileStripe.Info.FullName}{Environment.NewLine}";
                message += $"Error: {badReader.Exception.Message}{Environment.NewLine}";
                message += $"Stacktrace: {badReader.Exception.StackTrace}{Environment.NewLine}";
            }
            return new Exception(message);
        }

        public void Dispose()
        {
            KillSwitch = true;
            Disposed = true;

            foreach (var part in FileStripeReaders)
            {
                part.Dispose();
            }
        }
    }
}
