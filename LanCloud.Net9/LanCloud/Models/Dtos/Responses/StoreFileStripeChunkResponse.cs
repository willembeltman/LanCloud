namespace LanCloud.Models.Dtos.Responses;

public class StoreFileStripeChunkResponse
{
    public StoreFileStripeChunkResponse() { }
    public StoreFileStripeChunkResponse(bool succes)
    {
        Succes = succes;
    }

    public bool Succes { get; set; }
}