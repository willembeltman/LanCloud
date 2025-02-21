using LanCloud.Domain.Share;

namespace LanCloud.Models.Dtos
{
    public class ShareStripeDto
    {
        public ShareStripeDto() { }
        public ShareStripeDto(LocalShareStripe localShareStripe)
        {
            Indexes = localShareStripe.Indexes;
        }

        public int[] Indexes { get; set; }
    }
}