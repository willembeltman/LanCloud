using LanCloud.Interfaces;
using LanCloud.Domain.Share;
using LanCloud.Models.Dtos;

namespace LanCloud.Domain.Remote;

public class RemoteFileStripe : IFileStripe
{
    public RemoteFileStripe(RemoteShare remoteShare, FileStripeDto response)
    {
        RemoteShare = remoteShare;
        FileStripeDto = response;
    }

    public RemoteShare RemoteShare { get; }
    public FileStripeDto FileStripeDto { get; }

    public string Name => FileStripeDto.Name;
    public string Extention => FileStripeDto.Extention;
    public int[] Indexes => FileStripeDto.Indexes;
    public bool IsTemp => FileStripeDto.IsTemp;
    public string? Hash => FileStripeDto.Hash;
    public long? Length => FileStripeDto.Length;


    public Stream OpenRead()
    {
        throw new NotImplementedException();
    }

    public Stream OpenWrite()
    {
        throw new NotImplementedException();
    }

    public void Update(long length, string hash)
    {
        throw new NotImplementedException();
    }
}