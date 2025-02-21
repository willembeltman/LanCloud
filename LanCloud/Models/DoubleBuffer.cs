using System;

namespace LanCloud.Models
{
    public class DoubleBuffer
    {
        public DoubleBuffer(int bufferSizeForOne, int width = 1)
        {
            BufferSizeForOne = bufferSizeForOne;
            Width = width;
            Buffer1 = new byte[width * bufferSizeForOne];
            Array.Clear(Buffer1, 0, Buffer1.Length);
            BufferPosition1 = 0;
            Buffer2 = new byte[width * bufferSizeForOne];
            Array.Clear(Buffer2, 0, Buffer2.Length);
            BufferPosition2 = 0;
        }

        public int BufferSizeForOne { get; }
        public int Width { get; }
        private byte[] Buffer1 { get; }
        private byte[] Buffer2 { get; }
        private int BufferPosition1 { get; set; }
        private int BufferPosition2 { get; set; }
        public bool CurrentBufferSwitch { get; set; }

        public byte[] WriteBuffer => CurrentBufferSwitch ? Buffer1 : Buffer2;
        public int WriteBufferPosition
        {
            get => CurrentBufferSwitch ? BufferPosition1 : BufferPosition2;
            set
            {
                if (CurrentBufferSwitch)
                    BufferPosition1 = value;
                else
                    BufferPosition2 = value;
            }
        }

        public byte[] ReadBuffer => CurrentBufferSwitch ? Buffer2 : Buffer1;
        public int ReadBufferPosition => CurrentBufferSwitch ? BufferPosition2 : BufferPosition1;

        public void Flip()
        {
            CurrentBufferSwitch = !CurrentBufferSwitch;
        }
    }
}
