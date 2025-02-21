using LanCloud.Domain.FileStripe;

namespace LanCloud.Models.Share.Responses
{
    public class CreateFileStripeSessionResponse
    {
        public CreateFileStripeSessionResponse(FileStripeDto fileStripeDto)
        {
            FileStripeDto = fileStripeDto;
        }

        public FileStripeDto FileStripeDto { get; }
    }
}