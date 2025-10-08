using LanCloud.Models.Dtos;

namespace LanCloud.Models.Dtos.Responses;

public class CreateFileStripeSessionResponse
{
    public CreateFileStripeSessionResponse(FileStripeDto fileStripeDto)
    {
        FileStripeDto = fileStripeDto;
    }

    public FileStripeDto FileStripeDto { get; }
}