using System.Threading;

namespace LanCloud.Servers.Rpc
{
    public class RpcProxyQueueItem
    {
        public RpcProxyQueueItem(int requestMessageType, string requestJson, byte[] requestData, int requestDataLength, byte[] responseData)
        {
            RequestMessageType = requestMessageType;
            RequestJson = requestJson;
            RequestDataLength = requestDataLength;
            ResponseData = responseData;
        }

        public int RequestMessageType { get; set; }
        public string RequestJson { get; set; }
        public byte[] RequestData { get; set; }
        public int RequestDataLength { get; set; }

        public string ResponseJson { get; set; }
        public byte[] ResponseData { get; set; }
        public int ResponseDataLength { get; set; }

        public AutoResetEvent Done { get; } = new AutoResetEvent(false);
    }
}
