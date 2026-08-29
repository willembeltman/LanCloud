using LanCloud.Shared.Interfaces;

namespace LanCloud.Api.Services;

public class HostApi : IHostApi
{
    public async Task Test(string name, IAsyncEnumerable<string> test, IAsyncEnumerable<string> test2)
    {
        await foreach (var testItem in test)
        {
        }
        await foreach (var testItem in test2)
        {
        }
    }
}
