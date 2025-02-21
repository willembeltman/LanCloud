using System.Linq;

namespace LanCloud.Domain.FileRef
{
    public class FileRefStripeMetadata
    {
        public FileRefStripeMetadata(int[] indexes)
        {
            Indexes = indexes;
        }

        public int[] Indexes { get; }

        public string GetUniqueIdentifier()
        {
            return string.Join("_", Indexes.OrderBy(a => a));
        }
    }
}
