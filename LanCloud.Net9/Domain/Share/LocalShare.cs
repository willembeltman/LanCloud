using LanCloud.Domain.Application;
using LanCloud.Domain.Local;
using LanCloud.Domain.Rpc;
using LanCloud.Interfaces;
using LanCloud.Models.Config;
using LanCloud.Models.Dtos;
using LanCloud.Models.Dtos.Requests;
using LanCloud.Models.Dtos.Responses;
using LanCloud.Models.Enums;
using Newtonsoft.Json;
using System.Net;

namespace LanCloud.Domain.Share;

public class LocalShare : StatusBase, IRpcHandler, IDisposable, IShare
{
    public LocalShare(LocalApplication application, LocalShareConfig config, int port) : base(application)
    {
        Config = config;
        Port = port;

        if (!Root.Exists) Root.Create();

        LocalFileStripeInfos = Root
            .GetFiles($"*.filestripe")
            .Select(fileRefInfo => new LocalFileStripe(fileRefInfo))
            .ToDictionary(a => a.Name);

        if (Application.ApplicationConfig != null)
        {
            Server = new RpcServer(this, Application, IPAddress.Any, Port);
        }

        Status = application.Logger.Info($"OK");
    }
    public LocalShareConfig Config { get; }
    public int Port { get; }
    private Dictionary<string, LocalFileStripe> LocalFileStripeInfos { get; }
    public int[] Indexes => Config.Indexes;

    public string HostName => Application.ApplicationConfig?.HostName ?? string.Empty;
    public string RootFullName => Config.DirectoryName ?? string.Empty;
    public DirectoryInfo Root => new DirectoryInfo(RootFullName);

    public RpcServer? Server { get; private set; }

    public LocalFileStripe? FindFileStripe(string extention, string hash, long length, int[] indexes)
    {
        lock (LocalFileStripeInfos)
        {
            var name = LocalFileStripe.CreateFileName(extention, hash, length, indexes);
            if (LocalFileStripeInfos.TryGetValue(name, out var file)) return file;
            return null;
        }
    }
    public void AddFileStripe(LocalFileStripe fileStripe)
    {
        lock (LocalFileStripeInfos)
        {
            LocalFileStripeInfos.Add(fileStripe.Name, fileStripe);
        }
    }
    public void RemoveFileStripe(LocalFileStripe fileStripe)
    {
        lock (LocalFileStripeInfos)
        {
            LocalFileStripeInfos.Remove(fileStripe.Name);
        }
    }

    public void ProcessRequest(int requestMessageType, string? requestJson, byte[]? requestData, int requestDataLength, out string? responseJson, byte[]? responseData, out int responseDataLength)
    {
        switch (requestMessageType)
        {
            case (int)ShareMessageEnum.FindFileStripes:
                Handle_FindFileStripe(requestJson, requestData, requestDataLength, out responseJson, responseData, out responseDataLength);
                break;
            case (int)ShareMessageEnum.CreateFileStripeSession:
                Handle_CreateFileStripeSession(requestJson, requestData, requestDataLength, out responseJson, responseData, out responseDataLength);
                break;
            case (int)ShareMessageEnum.StoreFileStripePart:
                Handle_StoreFileStripeChunk(requestJson, requestData, requestDataLength, out responseJson, responseData, out responseDataLength);
                break;
            case (int)ShareMessageEnum.CloseFileStripeSession:
                Handle_CloseFileStripeSession(requestJson, requestData, requestDataLength, out responseJson, responseData, out responseDataLength);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void Handle_FindFileStripe(string? requestJson, byte[]? requestData, int requestDataLength, out string? responseJson, byte[]? responseData, out int responseDataLength)
    {
        if (requestJson == null)
        {
            responseJson = null;
            responseDataLength = 0;
            return;
        }

        var request = JsonConvert.DeserializeObject<FindFileStripesRequest>(requestJson);
        if (request == null)
        {
            responseJson = null;
            responseDataLength = 0;
            return;
        }

        var localFileStripe = FindFileStripe(request.Extention, request.Hash, request.Length, request.Indexes); 
        if (localFileStripe == null)
        {
            responseJson = null;
            responseDataLength = 0;
            return;
        }

        var remoteFileStripe = new FileStripeDto(localFileStripe);
        responseJson = JsonConvert.SerializeObject(remoteFileStripe);
        responseDataLength = 0;
    }

    private void Handle_CreateFileStripeSession(string? requestJson, byte[]? requestData, int requestDataLength, out string? responseJson, byte[]? responseData, out int responseDataLength)
    {
        if (requestJson == null)
        {
            responseJson = null;
            responseDataLength = 0;
            return;
        }

        var request = JsonConvert.DeserializeObject<CreateFileStripeSessionRequest>(requestJson);

        if (request == null)
        {
            responseJson = null;
            responseDataLength = 0;
            return;
        }

        var localFileStripe = CreateFileStripeSession(request.Extention);
        var fileStripeDto = new FileStripeDto(localFileStripe);

        var response = new CreateFileStripeSessionResponse(fileStripeDto);
        responseJson = JsonConvert.SerializeObject(response);
        responseDataLength = 0;
    }
    private void Handle_StoreFileStripeChunk(string? requestJson, byte[]? requestData, int requestDataLength, out string? responseJson, byte[]? responseData, out int responseDataLength)
    {
        if (requestJson == null)
        {
            responseJson = null;
            responseDataLength = 0;
            return;
        }

        var request = JsonConvert.DeserializeObject<StoreFileStripeChunkRequest>(requestJson);

        if (request == null)
        {
            responseJson = null;
            responseDataLength = 0;
            return;
        }

        var succes = StoreFileStripeChunk(request.Extention, request.Index, requestData, requestDataLength);

        var response = new StoreFileStripeChunkResponse(succes);
        responseJson = JsonConvert.SerializeObject(response);
        responseDataLength = 0;
    }
    private void Handle_CloseFileStripeSession(string? requestJson, byte[]? requestData, int requestDataLength, out string? responseJson, byte[]? responseData, out int responseDataLength)
    {
        if (requestJson == null)
        {
            responseJson = null;
            responseDataLength = 0;
            return;
        }

        var request = JsonConvert.DeserializeObject<CloseFileStripeSessionRequest>(requestJson);

        if (request == null)
        {
            responseJson = null;
            responseDataLength = 0;
            return;
        }

        LocalFileStripe localFileStripe = CloseFileStripeSession(request.Extention);
        var fileStripeDto = new FileStripeDto(localFileStripe);

        var response = new CloseFileStripeSessionResponse(fileStripeDto);
        responseJson = JsonConvert.SerializeObject(response);
        responseDataLength = 0;
    }

    public LocalFileStripe CreateFileStripeSession(string extention)
    {
        return new LocalFileStripe(Root, extention, Indexes);
    }

    public bool StoreFileStripeChunk(string extention, long index, byte[] requestData, int requestDataLength)
    {
        throw new NotImplementedException();
    }

    internal LocalFileStripe CloseFileStripeSession(string extention)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        Server?.Dispose();
    }

    IFileStripe? IShare.FindFileStripe(string extention, string hash, long length, int[] indexes)
    {
        return FindFileStripe(extention, hash, length, indexes);
    }
}