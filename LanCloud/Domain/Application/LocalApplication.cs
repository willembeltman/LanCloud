using LanCloud.Domain.FileRef;
using LanCloud.Domain.FileStripe;
using LanCloud.Domain.Share;
using LanCloud.Domain.VirtualFtp;
using LanCloud.Enums;
using LanCloud.Models.Configs;
using LanCloud.Models.Dtos;
using LanCloud.Servers.Ftp;
using LanCloud.Servers.Wjp;
using LanCloud.Shared.Log;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace LanCloud.Domain.Application
{
    public class LocalApplication : IDisposable, IWjpApplication, IFtpApplication, IWjpHandler
    {
        #region Status

        public event EventHandler OnStatusChanged;
        private string _Status { get; set; }
        public string Status
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

            Status = Logger.Info($"Constructing");

            Authentication = new AuthenticationService(this, logger);
            RealRootFullName = Config.RefDirectoryName.TrimEnd('\\');
            RealRoot = new DirectoryInfo(RealRootFullName);

            LocalApplicationServerConfig = config.Servers.FirstOrDefault(a => a.IsThisComputer);
            
            int port = LocalApplicationServerConfig?.Port ?? 8080;
            LocalShares = Config.Shares
                .Select(share => new LocalShare(this, share, ++port, Logger))
                .ToArray();

            RemoteApplications = Config.Servers
                .Where(a => a.IsThisComputer == false)
                .Select(remoteconfig => new RemoteApplication(this, remoteconfig, logger))
                .ToArray();

            if (LocalApplicationServerConfig != null)
            {
                LocalApplicationServer = new WjpServer(IPAddress.Any, LocalApplicationServerConfig.Port, this, this, logger);
            }

            VirtualFtpServer = new VirtualFtp.VirtualFtpServer(this, logger);

            if (LocalApplicationServerConfig != null)
            {
                Status = Logger.Info($"OK");
            }
            else
            {
                Status = Logger.Info($"OK without sharing");
            }
        }

        public ApplicationConfig Config { get; }
        public ILogger Logger { get; }

        public string RealRootFullName { get; }
        public DirectoryInfo RealRoot { get; }
        public AuthenticationService Authentication { get; }
        public LocalShare[] LocalShares { get; }
        public RemoteApplication[] RemoteApplications { get; }
        public RemoteApplicationConfig LocalApplicationServerConfig { get; }
        public WjpServer LocalApplicationServer { get; }
        public VirtualFtpServer VirtualFtpServer { get; }

        public string HostName => Config.HostName;
        public int FileStripeBufferSize => Config.FileStripeBufferSize;
        public int WjpBufferSize => Config.WjpBufferMultiplier * Config.FileStripeBufferSize;
        public int FtpBufferSize => Config.FtpBufferMultiplier * Config.FileStripeBufferSize;
        public int? Port => LocalApplicationServerConfig?.Port;
        public LocalShareStripe[] LocalShareStripes => LocalShares?
            .SelectMany(a => a.LocalShareStripes)
            .ToArray();


        public void StatusChanged()
        {
            OnStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        public LocalFileStripe[] FindFileStripes(string extention, FileRefMetadata fileRef, FileRefStripeMetadata fileRefBit)
        {
            if (fileRef.Length == null) return null;
            var fileStripes = LocalShares
                .Select(a => a.FindFileStripe(extention, fileRef.Hash, fileRef.Length.Value, fileRefBit.Indexes))
                .Where(a => a != null)
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
            LocalApplicationServer?.Dispose();
            foreach (var item in RemoteApplications)
                item.Dispose();
            if (LocalShares != null)
            {
                foreach (var item in LocalShares)
                    item.Dispose();
            }
        }

    }
}