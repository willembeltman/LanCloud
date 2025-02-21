using LanCloud.Domain.FileStripe;

namespace LanCloud.Models.Share.Responses
{
    public class CloseFileStripeSessionResponse
    {
        public CloseFileStripeSessionResponse(FileStripeDto fileStripeDto)
        {
            FileStripeDto = fileStripeDto;
        }

        public FileStripeDto FileStripeDto { get; }
    }
}