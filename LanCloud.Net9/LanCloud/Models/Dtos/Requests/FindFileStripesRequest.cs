namespace LanCloud.Models.Dtos.Requests;

internal class FindFileStripesRequest
{
    public FindFileStripesRequest() { }
    public FindFileStripesRequest(string extention, string hash, long length, int[] indexes)
    {
        Extention = extention;
        Hash = hash;
        Length = length;
        Indexes = indexes;
    }

    public string Extention { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public long Length { get; set; }
    public int[] Indexes { get; set; } = [];
}