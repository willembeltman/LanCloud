using LanCloud.Shared.Interfaces;

namespace LanCloud.Api.Services;

public class HostApi : IHostApi
{
    public async Task Test(string name, IAsyncEnumerable<string> test, IAsyncEnumerable<string> test2)
    {
        await foreach (var testItem in test)
        {
            //await Task.Delay(40000);
        }
        await foreach (var testItem in test2)
        {
        }
    }

    public async Task<string> Test2()
    {
        return "";
    }
    public async IAsyncEnumerable<string> Test3(string name, IAsyncEnumerable<string> test, IAsyncEnumerable<string> test2)
    {
        await foreach (var testItem in test)
        {
            //await Task.Delay(40000);
        }
        await foreach (var testItem in test2)
        {
        }

        yield return "1";
        await Task.Yield();
        yield return "2";
        await Task.Yield();
        yield return "3";
        await Task.Yield();
    }
    public async Task Test4()
    {
    }
}
