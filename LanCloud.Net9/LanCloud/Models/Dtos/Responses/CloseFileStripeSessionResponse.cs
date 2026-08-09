using LanCloud.Models.Dtos;

namespace LanCloud.Models.Dtos.Responses;

public class CloseFileStripeSessionResponse
{
    public CloseFileStripeSessionResponse(FileStripeDto fileStripeDto)
    {
        FileStripeDto = fileStripeDto;
    }

    public FileStripeDto FileStripeDto { get; }
}