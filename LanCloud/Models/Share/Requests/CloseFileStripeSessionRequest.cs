namespace LanCloud.Models.Share.Requests
{
    public class CloseFileStripeSessionRequest
    {
        public CloseFileStripeSessionRequest() { }
        public CloseFileStripeSessionRequest(string extention, int[] indexes)
        {
            Extention = extention;
            Indexes = indexes;
        }

        public string Extention { get; set; }
        public int[] Indexes { get; set; }
    }
}