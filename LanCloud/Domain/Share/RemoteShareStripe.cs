using LanCloud.Enums;
using LanCloud.Models.Dtos;
using LanCloud.Models.Share.Requests;
using LanCloud.Models.Share.Responses;
using LanCloud.Shared.Log;
using Newtonsoft.Json;

namespace LanCloud.Domain.Share
{
    public class RemoteShareStripe : IShareStripe
    {
        public RemoteShareStripe(RemoteShare remoteShare, ShareStripeDto config, ILogger logger) 
        {
            RemoteShare = remoteShare;
            Config = config;
            Logger = logger;
        }

        public RemoteShare RemoteShare { get; }
        public ShareStripeDto Config { get; }
        public ILogger Logger { get; }

        public IShare Share => RemoteShare;
        public int[] Indexes => Config.Indexes;


        public CreateFileStripeSessionResponse CreateFileStripeSession(string extention)
        {
            var request = new CreateFileStripeSessionRequest(extention, Indexes);
            var requestJson = JsonConvert.SerializeObject(request);

            string responseJson = "";
            int responseDataLength = 0;
            RemoteShare.SendRequest((int)ShareMessageEnum.CreateFileStripeSession, null, null, 0, out responseJson, null, out responseDataLength);
            var response = JsonConvert.DeserializeObject<CreateFileStripeSessionResponse>(responseJson);
            return response;
        }
        public StoreFileStripeChunkResponse StoreFileStripeChunk(string extention, long index, byte[] data, int datalength)
        {
            var request = new StoreFileStripeChunkRequest(extention, Indexes, index);
            var requestJson = JsonConvert.SerializeObject(request);

            string responseJson = "";
            int responseDataLength = 0;
            RemoteShare.SendRequest((int)ShareMessageEnum.StoreFileStripePart, requestJson, data, datalength, out responseJson, null, out responseDataLength);
            var response = JsonConvert.DeserializeObject<StoreFileStripeChunkResponse>(responseJson);
            return response;
        }
        public CloseFileStripeSessionResponse CloseFileStripeSession(string extention)
        {
            var request = new CloseFileStripeSessionRequest(extention, Indexes);
            var requestJson = JsonConvert.SerializeObject(request);

            string responseJson = "";
            int responseDataLength = 0;
            RemoteShare.SendRequest((int)ShareMessageEnum.CloseFileStripeSession, null, null, 0, out responseJson, null, out responseDataLength);
            var response = JsonConvert.DeserializeObject<CloseFileStripeSessionResponse>(responseJson);
            return response;
        }

        //public CreateFileStripeSessionResponse CreateFileStripeSession(string path)
        //{
        //    var request = new CreateFileStripeSessionRequest(path);
        //    var requestJson = JsonConvert.SerializeObject(request);

        //    string responseJson = "";
        //    int responseDataLength = 0;
        //    RemoteShare.SendRequest((int)ShareMessageEnum.CreateFileStripeSession, null, null, 0, out responseJson, null, out responseDataLength);
        //    var response = JsonConvert.DeserializeObject<CreateFileStripeSessionResponse>(responseJson);
        //    return response;
        //}
        //public StoreFileStripePartResponse StoreFileStripeChunk(string path, long index, byte[] data, int datalength)
        //{
        //    var request = new StoreFileStripeChunkRequest(path, index);
        //    var requestJson = JsonConvert.SerializeObject(request);

        //    string responseJson = "";
        //    int responseDataLength = 0;
        //    RemoteShare.SendRequest((int)ShareMessageEnum.StoreFileStripePart, null, null, 0, out responseJson, null, out responseDataLength);
        //    var response = JsonConvert.DeserializeObject<StoreFileStripePartResponse>(responseJson);
        //    return response;
        //}
        //public CloseFileStripeSessionResponse CloseFileStripeSession(string path, long index, byte[] data, int datalength)
        //{
        //    var request = new CloseFileStripeSessionRequest(path, index);
        //    var requestJson = JsonConvert.SerializeObject(request);

        //    string responseJson = "";
        //    int responseDataLength = 0;
        //    RemoteShare.SendRequest((int)ShareMessageEnum.CloseFileStripeSession, null, null, 0, out responseJson, null, out responseDataLength);
        //    var response = JsonConvert.DeserializeObject<CloseFileStripeSessionResponse>(responseJson);
        //    return response;
        //}
    }
}
