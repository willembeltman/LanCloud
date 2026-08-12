namespace LanCloud.Shared.Dtos;

public class FileChunkDto
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public long Offset { get; internal set; }
}
