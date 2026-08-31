using gAPI.Generated;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace LanCloud.Api.Controllers;

[Route("test")]
public class TestController(
    IClientContext clientContext)
    : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        await clientContext.HostHub.ToAll.StartTest(ct);

        async IAsyncEnumerable<string> test()
        {
            yield return "1";
            yield return "2";
            yield return "3";
        }


        var tries = 10;
        Stopwatch watch = Stopwatch.StartNew();
        for (int i = 0; i < tries; i++)
        {
            var list = clientContext.HostHub.ToAll.Test6("test", test(), test(), ct);

            await foreach (var item in list)
            {
            }
        }
        var avg = watch.ElapsedMilliseconds / tries;
        Console.WriteLine($"{avg}ms");

        return Content($"{avg}ms");
    }
}
