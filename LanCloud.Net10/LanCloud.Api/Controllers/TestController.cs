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
        var list = await clientContext.HostHub.ToAll.ListDirectory("", ct).ToArrayAsync(ct);
        var files = list.Where(a => a.IsDirectory == false).ToArray();
        var file = files.FirstOrDefault();
        if (file == null || file.SessionId == null)
            throw new FileNotFoundException();
        var chunks = clientContext.HostHub.ToSession(file.SessionId.Value)
            .ReadFile(file.Name, 0, ct);

        await foreach (var chunk in chunks)
        {
            var offset = chunk.Offset;
        }

        return Content("Hoi");
    }
}
