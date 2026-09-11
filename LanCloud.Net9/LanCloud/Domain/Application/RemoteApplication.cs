using LanCloud.Domain.Rpc;
using LanCloud.Domain.Share;
using LanCloud.Interfaces;
using LanCloud.Models.Config;
using LanCloud.Models.Dtos;
using LanCloud.Models.Enums;
using Newtonsoft.Json;

namespace LanCloud.Domain.Application;

public class RemoteApplication : RpcProxy
{
    public RemoteApplication(
        LocalApplication application,
        RemoteApplicationConfig config) : base(config, application)
    {
        StateChanged += RemoteApplication_StateChanged;
    }

    public RemoteShare[] RemoteShares { get; private set; } = [];

    private void RemoteApplication_StateChanged(object? sender, System.EventArgs? e)
    {
        if (Connected)
        {
            RemoteShares = GetShares()
                .Select(a => new RemoteShare(this, a))
                .ToArray();
        }
        else
        {
            foreach (var item in RemoteShares)
            {
                item.Dispose();
            }
            RemoteShares = new RemoteShare[0];
        }
    }

    public ShareDto[] GetShares()
    {
        string? responseJson = null;
        int responseDataLength = 0;
        SendRequest((int)ApplicationMessageEnum.GetShares, string.Empty, null, 0, out responseJson, null, out responseDataLength);
        if (responseJson == null) throw new Exception();
        var response = JsonConvert.DeserializeObject<ShareDto[]>(responseJson);
        if (response == null) throw new Exception();
        return response;
    }
}
