using LanCloud.Shared.Log;
using LanCloud.Models.Configs;
using LanCloud.Domain.Application;
using System;
using System.IO;
using LanCloud.Enums;
using LanCloud.Models.Share.Requests;
using Newtonsoft.Json;
using LanCloud.Servers.Wjp;
using System.Net;
using System.Linq;
using LanCloud.Domain.FileStripe;
using System.Collections.Generic;
using LanCloud.Services;
using LanCloud.Models.Share.Responses;

namespace LanCloud.Domain.Share
{
    public class LocalShare : IWjpHandler, IDisposable, IShare
    {
        private string _Status { get; set; }
        public string Status
        {
            get => _Status;
            set
            {
                _Status = value;
                Application.StatusChanged();
            }
        }

        public LocalShare(LocalApplication application, LocalShareConfig config, int port, ILogger logger)
        {
            Application = application;
            Config = config;
            Port = port;
            Logger = logger;

            if (!Root.Exists) Root.Create();

            LocalFileStripeInfos = Root
                .GetFiles($"*.filestripe")
                .Select(fileRefInfo => new LocalFileStripe(fileRefInfo))
                .ToDictionary(a => a.Name);

            LocalShareStripes = config.Parts
                .Select(part => new LocalShareStripe(this, part, logger))
                .ToArray();

            if (Application.LocalApplicationServerConfig != null)
            {
                Server = new WjpServer(IPAddress.Any, Port, this, Application, Logger);
            }

            Status = Logger.Info($"OK");
        }

        public LocalApplication Application { get; }
        public LocalShareConfig Config { get; }
        public int Port { get; }
        public ILogger Logger { get; }
        private Dictionary<string, LocalFileStripe> LocalFileStripeInfos { get; }
        public LocalShareStripe[] LocalShareStripes { get; }

        public string HostName => Application.LocalApplicationServerConfig?.HostName;
        public string RootFullName => Config.DirectoryName;
        public DirectoryInfo Root => new DirectoryInfo(RootFullName);

        public WjpServer Server { get; private set; }

        public LocalFileStripe FindFileStripe(string extention, string hash, long length, int[] indexes)
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

        public void ProcessRequest(int requestMessageType, string requestJson, byte[] requestData, int requestDataLength, out string responseJson, byte[] responseData, out int responseDataLength)
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

        private void Handle_FindFileStripe(string requestJson, byte[] requestData, int requestDataLength, out string responseJson, byte[] responseData, out int responseDataLength)
        {
            var request = JsonConvert.DeserializeObject<FindFileStripesRequest>(requestJson);
            var localFileStripe = FindFileStripe(request.Extention, request.Hash, request.Length, request.Indexes);
            var remoteFileStripe = new FileStripeDto(localFileStripe);
            responseJson = JsonConvert.SerializeObject(remoteFileStripe);
            responseDataLength = 0;
        }

        private void Handle_CreateFileStripeSession(string requestJson, byte[] requestData, int requestDataLength, out string responseJson, byte[] responseData, out int responseDataLength)
        {
            var request = JsonConvert.DeserializeObject<CreateFileStripeSessionRequest>(requestJson);

            var shareStripe = LocalShareStripes.FirstOrDefault(a => a.Indexes.Matches(request.Indexes));
            if (shareStripe == null)
            {
                responseJson = null;
                responseDataLength = 0;
                return;
            }

            var localFileStripe = shareStripe.CreateFileStripeSession(request.Extention);
            var fileStripeDto = new FileStripeDto(localFileStripe);

            var response = new CreateFileStripeSessionResponse(fileStripeDto);
            responseJson = JsonConvert.SerializeObject(response);
            responseDataLength = 0;
        }
        private void Handle_StoreFileStripeChunk(string requestJson, byte[] requestData, int requestDataLength, out string responseJson, byte[] responseData, out int responseDataLength)
        {
            var request = JsonConvert.DeserializeObject<StoreFileStripeChunkRequest>(requestJson);

            var shareStripe = LocalShareStripes.FirstOrDefault(a => a.Indexes.Matches(request.Indexes));
            if (shareStripe == null)
            {
                responseJson = null;
                responseDataLength = 0;
                return;
            }

            var succes = shareStripe.StoreFileStripeChunk(request.Extention, request.Index, requestData, requestDataLength);

            var response = new StoreFileStripeChunkResponse(succes);
            responseJson = JsonConvert.SerializeObject(response);
            responseDataLength = 0;
        }
        private void Handle_CloseFileStripeSession(string requestJson, byte[] requestData, int requestDataLength, out string responseJson, byte[] responseData, out int responseDataLength)
        {
            var request = JsonConvert.DeserializeObject<CloseFileStripeSessionRequest>(requestJson);

            var shareStripe = LocalShareStripes.FirstOrDefault(a => a.Indexes.Matches(request.Indexes));
            if (shareStripe == null)
            {
                responseJson = null;
                responseDataLength = 0;
                return;
            }

            LocalFileStripe localFileStripe = shareStripe.CloseFileStripeSession(request.Extention);
            var fileStripeDto = new FileStripeDto(localFileStripe);

            var response = new CloseFileStripeSessionResponse(fileStripeDto);
            responseJson = JsonConvert.SerializeObject(response);
            responseDataLength = 0;
        }

        public void Dispose()
        {
            Server.Dispose();
        }


        #region IShare interface
        IShareStripe[] IShare.ShareStripes => LocalShareStripes;
        IFileStripe IShare.FindFileStripe(string extention, string hash, long length, int[] indexes)
        {
            return FindFileStripe(extention, hash, length, indexes);
        }
        #endregion
    }
}