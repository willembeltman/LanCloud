using LanCloud.Domain.FileStripe;
using System.IO;

namespace LanCloud.Domain.Share
{
    public class RemoteFileStripe : IFileStripe
    {
        public RemoteFileStripe(RemoteShare remoteShare, FileStripeDto response)
        {
            RemoteShare = remoteShare;
            FileStripeDto = response;
        }

        public RemoteShare RemoteShare { get; }
        public FileStripeDto FileStripeDto { get; }

        public string Extention => ((IFileStripe)FileStripeDto).Extention;

        public string Hash => ((IFileStripe)FileStripeDto).Hash;

        public long Length => ((IFileStripe)FileStripeDto).Length;

        public int[] Indexes => ((IFileStripe)FileStripeDto).Indexes;

        public bool IsTemp => ((IFileStripe)FileStripeDto).IsTemp;

        public FileStream OpenRead()
        {
            throw new System.NotImplementedException();
        }
    }
}