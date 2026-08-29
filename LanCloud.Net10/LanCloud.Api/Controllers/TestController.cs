using gAPI.Generated;
using Microsoft.AspNetCore.Mvc;

namespace LanCloud.Api.Controllers;

[Route("test")]
public class TestController(
    IClientContext clientContext)
    : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        async IAsyncEnumerable<string> test()
        {
            yield return "1";
            await Task.Yield();
            yield return "2";
            await Task.Yield();
            yield return "3";
            await Task.Yield();
        }

        await clientContext.HostHub.ToAll.Test("test", test(), test());

        //var list = await clientContext.HostHub.ToAll.ListDirectory("", ct).ToArrayAsync(ct);
        //var files = list.Where(a => a.IsDirectory == false).ToArray();
        //var file = files.FirstOrDefault();
        //if (file == null || file.SessionId == null)
        //    throw new FileNotFoundException();
        //var chunks = clientContext.HostHub.ToSession(file.SessionId.Value)
        //    .ReadFile(file.Name, 0, ct);

        //await foreach (var chunk in chunks)
        //{
        //    var offset = chunk.Offset;
        //}

        return Content("Hoi");
    }
}
