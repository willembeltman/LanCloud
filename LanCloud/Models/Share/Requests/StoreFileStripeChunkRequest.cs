namespace LanCloud.Models.Share.Requests
{
    public class StoreFileStripeChunkRequest
    {
        public StoreFileStripeChunkRequest() { }
        public StoreFileStripeChunkRequest(string extention, int[] indexes, long index)
        {
            Extention = extention;
            Indexes = indexes;
            Index = index;
        }

        public string Extention { get; set; }
        public int[] Indexes { get; set; }
        public long Index { get; set; }
    }
}
