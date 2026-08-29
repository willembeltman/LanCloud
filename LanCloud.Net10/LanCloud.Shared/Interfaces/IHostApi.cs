using gAPI.Core.Attributes;

namespace LanCloud.Shared.Interfaces;

[GenerateApi]
public interface IHostApi
{
    Task Test(string name, IAsyncEnumerable<string> test, IAsyncEnumerable<string> test2);
    Task<string> Test2();
    IAsyncEnumerable<string> Test3(string name, IAsyncEnumerable<string> test, IAsyncEnumerable<string> test2);
    Task Test4();
}
