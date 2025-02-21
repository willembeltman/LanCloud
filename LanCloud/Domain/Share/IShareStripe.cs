namespace LanCloud.Domain.Share
{
    public interface IShareStripe
    {
        IShare Share { get; }
        int[] Indexes { get; }
    }
}
