namespace LanCloud.Domain;

public class DoubleBuffer
{
    public DoubleBuffer(int bufferSizeForOne, int width = 1)
    {
        BufferSizeForOne = bufferSizeForOne;
        Width = width;
        Buffer1 = new byte[width * bufferSizeForOne];
        Array.Clear(Buffer1, 0, Buffer1.Length);
        Position1 = 0;
        Buffer2 = new byte[width * bufferSizeForOne];
        Array.Clear(Buffer2, 0, Buffer2.Length);
        Position2 = 0;
    }

    public int BufferSizeForOne { get; }
    public int Width { get; }
    private byte[] Buffer1 { get; }
    private byte[] Buffer2 { get; }
    private int Position1 { get; set; }
    private int Position2 { get; set; }
    private long? StartPosition1 { get; set; }
    private long? StartPosition2 { get; set; }
    public bool Switch { get; set; }

    public byte[] WriteBuffer => Switch ? Buffer1 : Buffer2;
    public int WriteDataLength
    {
        get => Switch ? Position1 : Position2;
        set
        {
            if (Switch)
                Position1 = value;
            else
                Position2 = value;
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
    public int ReadDataLength
    {
        get => Switch ? Position2 : Position1;
        set
        {
            if (Switch)
                Position2 = value;
            else
                Position1 = value;
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
