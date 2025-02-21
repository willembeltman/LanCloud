using System.Linq;

namespace LanCloud.Services
{
    public static class IndexesService
    {
        public static bool Matches(this int[] thisOne, int[] compareTo)
        {
            return thisOne.Length == compareTo.Length &&
                thisOne.All(a => compareTo.Any(b => a == b));
        }
        public static string ToUniqueKey(this int[] thisOne)
        {
            return string.Join("_", thisOne.OrderBy(a => a));
        }
    }
}
