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
        var list = await clientContext.HostHub.ToAll.ListDirectory("",ct).ToArrayAsync(ct);

        return Content("Hoi");
    }
}
