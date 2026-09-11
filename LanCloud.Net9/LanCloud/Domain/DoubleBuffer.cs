namespace LanCloud.Domain;

public class DoubleBuffer
{
    public DoubleBuffer(int bufferSizeForOne, int width = 1)
    {
        BufferSizeForOne = bufferSizeForOne;
        Width = width;
        Buffer1 = new byte[width * bufferSizeForOne];
        Array.Clear(Buffer1, 0, Buffer1.Length);
        BytesWritten1 = 0;
        Buffer2 = new byte[width * bufferSizeForOne];
        Array.Clear(Buffer2, 0, Buffer2.Length);
        BytesWritten2 = 0;
    }

    public int BufferSizeForOne { get; }
    public int Width { get; }
    private byte[] Buffer1 { get; }
    private byte[] Buffer2 { get; }
    private int BytesWritten1 { get; set; }
    private int BytesWritten2 { get; set; }
    private long? StartPosition1 { get; set; }
    private long? StartPosition2 { get; set; }
    public bool Switch { get; set; }

    public byte[] WriteBuffer => Switch ? Buffer1 : Buffer2;
    public int WriteBytesWritten
    {
        get => Switch ? BytesWritten1 : BytesWritten2;
        set
        {
            if (Switch)
                BytesWritten1 = value;
            else
                BytesWritten2 = value;
        }
    }
    public long? WriteStartPosition
    {
        get => Switch ? StartPosition1 : StartPosition2;
        set
        {
            if (Switch)
                StartPosition1 = value;
            else
                StartPosition2 = value;
        }
    }

    public byte[] ReadBuffer => Switch ? Buffer2 : Buffer1;
    public int ReadBytesWritten
    {
        get => Switch ? BytesWritten2 : BytesWritten1;
        set
        {
            if (Switch)
                BytesWritten2 = value;
            else
                BytesWritten1 = value;
        }
    }
    public long? ReadStartPosition
    {
        get => Switch ? StartPosition2 : StartPosition1;
        set
        {
            if (Switch)
                StartPosition2 = value;
            else
                StartPosition1 = value;
        }
    }


    public void Flip()
    {
        Switch = !Switch;
    }
}
