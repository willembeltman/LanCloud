using LanCloud.Domain.Application;
using LanCloud.Domain.FileStripe;
using LanCloud.Enums;
using LanCloud.Interfaces;
using LanCloud.Models.Dtos;
using LanCloud.Servers.Rpc;
using Newtonsoft.Json;

namespace LanCloud.Domain.Share;

public class RemoteShare : RpcProxy, IShare
{
    public RemoteShare(RemoteApplication remoteApplication, ShareDto config, ILogger logger) : base(config, remoteApplication.LocalApplication, logger)
    {
        Logger = logger;
        ShareStripes = config.ShareStripes
            .Select(a => new RemoteShareStripe(this, a, logger))
            .ToArray();
    }

    public ILogger Logger { get; }
    public RemoteShareStripe[] ShareStripes { get; }

    IShareStripe[] IShare.ShareStripes => ShareStripes;

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
