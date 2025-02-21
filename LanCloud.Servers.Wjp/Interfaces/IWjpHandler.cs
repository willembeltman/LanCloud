namespace LanCloud.Servers.Wjp
{
    public interface IWjpHandler
    {
        void ProcessRequest(int requestMessageType, string requestJson, byte[] requestData, int requestDataLength, out string responseJson, byte[] responseData, out int responseDataLength);
    }
}