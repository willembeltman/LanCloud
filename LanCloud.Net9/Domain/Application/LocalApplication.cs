using LanCloud.Domain.IO;
using LanCloud.Domain.Local;
using LanCloud.Domain.Rpc;
using LanCloud.Domain.Share;
using LanCloud.Interfaces;
using LanCloud.Models.Config;
using LanCloud.Models.Dtos;
using LanCloud.Models.Enums;
using Newtonsoft.Json;
using System.Net;

namespace LanCloud.Domain.Application;

public class LocalApplication : IDisposable, IRpcHandler
{
    #region Status

    public event EventHandler? OnStatusChanged;
    public void StatusChanged()
    {
        OnStatusChanged?.Invoke(this, EventArgs.Empty);
    }
    private string? _Status { get; set; }
    public string? Status
    {
        get => _Status;
        set
        {
            _Status = value;
            StatusChanged();
        }
    }

    #endregion

    public LocalApplication(ApplicationConfig config, ILogger logger)
    {
        Config = config;
        Logger = logger;

        Authentication = new Authentication(this);
        RealRootFullName = Config.RefDirectoryName.TrimEnd('\\');
        RealRoot = new DirectoryInfo(RealRootFullName);

        ApplicationConfig = config.Servers.FirstOrDefault(a => a.IsThisComputer);

        int port = ApplicationConfig?.Port ?? 8080;
        LocalShares = Config.Shares
            .Select(share => new LocalShare(this, share, ++port))
            .ToArray();

        RemoteApplications = Config.Servers
            .Where(a => a.IsThisComputer == false)
            .Select(remoteconfig => new RemoteApplication(this, remoteconfig))
            .ToArray();

        FileSystem = new FileSystem(this, logger);

        if (ApplicationConfig != null)
        {
            RpcServer = new RpcServer(this, this, IPAddress.Any, ApplicationConfig.Port);
        }

        if (Config.EnableVirtualDrive)
        {
            VirtualDriveServer = new VirtualDriveServer(this);
        }

        if (Config.EnableFtpServer)
        {
            VirtualFtpServer = new VirtualFtpServer(this);
        }

        Status = Logger.Info($"OK");
    }

    public ApplicationConfig Config { get; }
    public ILogger Logger { get; }

    public string RealRootFullName { get; }
    public DirectoryInfo RealRoot { get; }
    public Authentication Authentication { get; }
    public LocalShare[] LocalShares { get; }
    public RemoteApplication[] RemoteApplications { get; }
    public FileSystem FileSystem { get; }
    public RemoteApplicationConfig? ApplicationConfig { get; }
    public RpcServer? RpcServer { get; }
    public VirtualFtpServer? VirtualFtpServer { get; }
    public VirtualDriveServer? VirtualDriveServer { get; }

    public string HostName => Config.HostName;
    public int FileStripeBufferSize => Config.FileStripeBufferSize;
    public int RpcBufferSize => Config.RpcBufferMultiplier * Config.FileStripeBufferSize;
    public int FtpBufferSize => Config.FtpBufferMultiplier * Config.FileStripeBufferSize;
    public int VirtualDriveBufferSize => Config.VirtualDriveBufferMultiplier * Config.FileStripeBufferSize;
    public int? Port => ApplicationConfig?.Port;
    public LocalShareStripe[] LocalShareStripes => LocalShares
        .SelectMany(a => a.LocalShareStripes)
        .ToArray();


    public LocalFileStripe[] FindFileStripes(string extention, FileMetadata fileRef, FileStripeMetadata fileRefBit)
    {
        var fileStripes = LocalShares
            .Select(a =>
            {
                if (fileRef.Hash == null) return null;
                return a.FindFileStripe(extention, fileRef.Hash, fileRef.Length, fileRefBit.Indexes);
            })
            .Where(a => a != null)
            .Select(a => a!)
            .ToArray();
        return fileStripes;
    }

    public void ProcessRequest(
        int requestMessageType, string requestJson, byte[] requestData, int requestDataLength,
        out string responseJson, byte[] responseData, out int responseDataLength)
    {
        switch (requestMessageType)
        {
            case (int)ApplicationMessageEnum.GetShares:
                Handle_GetShareDtos(out responseJson, out responseDataLength);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void Handle_GetShareDtos(out string responseJson, out int responseDataLength)
    {
        var shareDtos = LocalShares
            .Select(localShare => new ShareDto(localShare))
            .ToArray();
        responseJson = JsonConvert.SerializeObject(shareDtos);
        responseDataLength = 0;
    }

    public void Dispose()
    {
        try
        {
            VirtualDriveServer?.Dispose();
            VirtualFtpServer?.Dispose();
        }
        catch
        {
            // swallow shutdown exceptions
        }

        RpcServer?.Dispose();
        foreach (var item in RemoteApplications)
            item.Dispose();
        if (LocalShares != null)
        {
            foreach (var item in LocalShares)
                item.Dispose();
        }
    }

}







