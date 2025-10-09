namespace LanCloud.Interfaces;

public interface IShareStripe
{
    IShare Share { get; }
    int[] Indexes { get; }
}
