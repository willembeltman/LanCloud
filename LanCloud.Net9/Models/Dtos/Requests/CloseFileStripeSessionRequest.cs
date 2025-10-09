namespace LanCloud.Models.Dtos.Requests;

public class CloseFileStripeSessionRequest
{
    public CloseFileStripeSessionRequest() { }
    public CloseFileStripeSessionRequest(string extention, int[] indexes)
    {
        Extention = extention;
        Indexes = indexes;
    }

    public string Extention { get; set; } = string.Empty;
    public int[] Indexes { get; set; } = [];
}