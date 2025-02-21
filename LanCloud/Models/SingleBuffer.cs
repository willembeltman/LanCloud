using System;

namespace LanCloud.Models
{
    public class SingleBuffer
    {
        public SingleBuffer(int bufferSizeForOne, int width = 1)
        {
            BufferSizeForOne = bufferSizeForOne;
            Width = width;
            Buffer = new byte[width * bufferSizeForOne];
            Array.Clear(Buffer, 0, Buffer.Length);
            BufferPosition = 0;
        }

        public int BufferSizeForOne { get; }
        public int Width { get; }
        public byte[] Buffer { get; }
        public int BufferPosition { get; set; }
        public int Length => Buffer.Length;
    }
}
