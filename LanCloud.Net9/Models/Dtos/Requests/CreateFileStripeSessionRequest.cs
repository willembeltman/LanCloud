namespace LanCloud.Models.Dtos.Requests;

public class CreateFileStripeSessionRequest
{
    public CreateFileStripeSessionRequest() { }
    public CreateFileStripeSessionRequest(string extention, int[] indexes)
    {
        Extention = extention;
        Indexes = Indexes;
    }

    public string? Extention { get; }
    public int[]? Indexes { get; set; }
}