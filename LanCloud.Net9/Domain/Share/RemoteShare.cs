using LanCloud.Domain.Application;
using LanCloud.Domain.Remote;
using LanCloud.Domain.Rpc;
using LanCloud.Interfaces;
using LanCloud.Models.Dtos;
using LanCloud.Models.Dtos.Requests;
using LanCloud.Models.Enums;
using Newtonsoft.Json;

namespace LanCloud.Domain.Share;

public class RemoteShare : RpcProxy, IShare
{
    public RemoteShare(RemoteApplication remoteApplication, ShareDto config) : base(config, remoteApplication.Application)
    {
        Share = config;
        RemoteApplication = remoteApplication;
    }

    public ShareDto Share { get; }
    public RemoteApplication RemoteApplication { get; }

    public int[] Indexes => Share.Indexes;

    public void AddFileStripe(IFileStripe fileStripe)
    {
        throw new NotImplementedException();
    }

    public IFileStripe CreateFileStripeSession(string extention)
    {
        throw new NotImplementedException();
    }

    public IFileStripe? FindFileStripe(string extention, string hash, long length, int[] indexes)
    {
        var request = new FindFileStripesRequest(extention, hash, length, indexes);
        var requestJson = JsonConvert.SerializeObject(request);

        string responseJson = "";
        int responseDataLength = 0;
        SendRequest((int)ShareMessageEnum.FindFileStripes, requestJson, null, 0, out responseJson, null, out responseDataLength);
        var fileStripeDto = JsonConvert.DeserializeObject<FileStripeDto>(responseJson);
        if (fileStripeDto == null) return null;

        var remoteFileStripe = new RemoteFileStripe(this, fileStripeDto);
        return remoteFileStripe;
    }
}
