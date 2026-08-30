using gAPI.Core.Attributes;

namespace LanCloud.Shared.Interfaces;

[GenerateApi]
public interface ITestApi
{
    Task Test1();
    Task<string> Test2();
    IAsyncEnumerable<string> Test3(CancellationToken ct);
    Task Test4(string name, IAsyncEnumerable<string> test, IAsyncEnumerable<string> test2);
    Task<string> Test5(string name, IAsyncEnumerable<string> test, IAsyncEnumerable<string> test2);
    IAsyncEnumerable<string> Test6(string name, IAsyncEnumerable<string> test, IAsyncEnumerable<string> test2, CancellationToken ct);
}
