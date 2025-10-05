namespace LanCloud.Shared.Interfaces
{
    public interface IRpcHandler
    {
        void ProcessRequest(
            int requestMessageType,
            string requestJson,
            byte[] requestData,
            int requestDataLength,
            out string responseJson,
            byte[] responseData,
            out int responseDataLength);
    }
}