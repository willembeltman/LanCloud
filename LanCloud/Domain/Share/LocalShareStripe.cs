using LanCloud.Shared.Log;
using System;
using LanCloud.Models.Configs;
using System.Linq;
using LanCloud.Domain.FileStripe;

namespace LanCloud.Domain.Share
{
    public class LocalShareStripe : IShareStripe
    {
        public LocalShareStripe(LocalShare localShare, LocalShareBitConfig partConfig, ILogger logger)
        {
            LocalShare = localShare;
            PartConfig = partConfig;
            Logger = logger;

            Indexes = PartConfig.Indexes;
        }

        public LocalShare LocalShare { get; }
        public LocalShareBitConfig PartConfig { get; }
        public ILogger Logger { get; }

        public int[] Indexes { get; }

        public IShare Share => LocalShare;


        public LocalFileStripe CreateFileStripeSession(string extention)
        {
            return new LocalFileStripe(LocalShare.Root, extention, Indexes);
        }

        public bool StoreFileStripeChunk(string extention, long index, byte[] requestData, int requestDataLength)
        {
            throw new NotImplementedException();
        }

        internal LocalFileStripe CloseFileStripeSession(string extention)
        {
            throw new NotImplementedException();
        }
    }
}