using LanCloudSimple.Api.Helpers;
using LanCloudSimple.Api.Models;
using LanCloudSimple.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.Text;
using System.Xml.Linq;

namespace LanCloudSimple.Api.Controllers;

[ApiController]
[Route("dav")]
public class WebDavController : ControllerBase
{
    private readonly CloudService _cloudService;
    private readonly ILogger<WebDavController> _logger;

    public WebDavController(CloudService cloudService, ILogger<WebDavController> logger)
    {
        _cloudService = cloudService;
        _logger = logger;
    }

    [HttpOptions]
    [HttpOptions("{*path}")]
    public IActionResult Options()
    {
        Response.Headers.Append("DAV", "1, 2");
        Response.Headers.Append("Allow", "OPTIONS, GET, HEAD, PROPFIND, PUT, DELETE, MKCOL");
        Response.Headers.Append("MS-Author-Via", "DAV");
        return Ok();
    }

    [AcceptVerbs("PROPFIND")]
    [Route("{*path}")]
    public IActionResult PropFind(string? path)
    {
        path = (path ?? "").Replace('\\', '/').Trim('/');
        var depth = Request.Headers["Depth"].FirstOrDefault() ?? "1";

        _logger.LogInformation("PROPFIND '{path}' (Depth: {depth})", path, depth);

        bool isRoot = string.IsNullOrEmpty(path);
        bool isDirectory = isRoot;
        BrowseNode? targetFile = null;

        if (!isRoot)
        {
            var parentPath = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "";
            var selfItem = _cloudService.Browse(parentPath)
                .FirstOrDefault(x => x.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

            if (selfItem != null)
            {
                isDirectory = selfItem.IsDirectory;
                if (!isDirectory) targetFile = selfItem;
            }
            else
            {
                if (_cloudService.Browse(path).Any())
                    isDirectory = true;
                else
                    return NotFound();
            }
        }

        XNamespace d = "DAV:";
        var multistatus = new XElement(d + "multistatus");

        void AddResponse(string hrefPath, string displayName, bool isDir, long size, DateTime? modTime)
        {
            var href = "/dav/" + string.Join("/", hrefPath.Split('/').Select(Uri.EscapeDataString));
            if (isDir && !href.EndsWith("/")) href += "/";

            var prop = new XElement(d + "prop",
                new XElement(d + "displayname", displayName));

            if (isDir)
            {
                prop.Add(new XElement(d + "resourcetype", new XElement(d + "collection")));
            }
            else
            {
                var t = modTime ?? DateTime.UtcNow;
                prop.Add(new XElement(d + "resourcetype"));
                prop.Add(new XElement(d + "getcontentlength", size));
                prop.Add(new XElement(d + "getlastmodified", t.ToString("R")));
                prop.Add(new XElement(d + "creationdate", t.ToString("yyyy-MM-ddTHH:mm:ssZ")));
            }

            multistatus.Add(new XElement(d + "response",
                new XElement(d + "href", href),
                new XElement(d + "propstat",
                    prop,
                    new XElement(d + "status", "HTTP/1.1 200 OK"))));
        }

        if (isDirectory)
            AddResponse(path, isRoot ? "Root" : System.IO.Path.GetFileName(path), true, 0, null);
        else if (targetFile != null)
            AddResponse(targetFile.Path, targetFile.Name, false, targetFile.Size, targetFile.MediaDate);

        if (isDirectory && depth == "1")
        {
            foreach (var child in _cloudService.Browse(path))
                AddResponse(child.Path, child.Name, child.IsDirectory, child.Size, child.MediaDate);
        }

        var xmlDoc = new XDocument(new XDeclaration("1.0", "utf-8", null), multistatus);
        var writer = new Utf8StringWriter();
        xmlDoc.Save(writer);

        return Content(writer.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("{*path}")]
    public async Task<IActionResult> Get(string? path)
    {
        path = (path ?? "").Replace('\\', '/').Trim('/');
        if (string.IsNullOrEmpty(path))
            return Ok("WebDAV Root");

        var resolved = _cloudService.ResolveMergedPath(path);
        if (resolved == null) return NotFound();

        var streamResult = await _cloudService.GetFileStreamAsync(resolved.Value.ClientId, resolved.Value.ClientPath);
        if (streamResult == null) return NotFound();

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(path, out var contentType))
            contentType = "application/octet-stream";

        return File(streamResult.Value.Stream, contentType, System.IO.Path.GetFileName(path), enableRangeProcessing: true);
    }

    [HttpPut("{*path}")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Put(string path)
    {
        path = path.Replace('\\', '/').Trim('/');
        _logger.LogInformation("WebDAV PUT: {path}", path);

        try
        {
            await _cloudService.SaveFileAsync(path, Request.Body, CancellationToken.None);
            return Created($"/dav/{path}", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PUT failed: {path}", path);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{*path}")]
    public IActionResult Delete(string path)
    {
        path = path.Replace('\\', '/').Trim('/');
        _logger.LogInformation("WebDAV DELETE: {path}", path);

        var resolved = _cloudService.ResolveMergedPath(path);
        if (resolved == null) return NotFound();

        if (!resolved.Value.ClientId.Equals("Local", StringComparison.OrdinalIgnoreCase))
            return Forbid("Remote client files are read-only.");

        try
        {
            // The client path starts with the root directory name (e.g. "LocalStorage/sub/file.jpg").
            // Strip that root to get the path relative to LocalStoragePath.
            var clientPath = resolved.Value.ClientPath;
            var storageName = System.IO.Path.GetFileName(_cloudService.LocalStoragePath);
            var relativePart = clientPath.StartsWith(storageName + "/", StringComparison.OrdinalIgnoreCase)
                ? clientPath[(storageName.Length + 1)..]
                : clientPath;

            _cloudService.DeleteLocalPath(relativePart);
            return NoContent();
        }
        catch (System.IO.FileNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DELETE failed: {path}", path);
            return StatusCode(500, ex.Message);
        }
    }

    [AcceptVerbs("MKCOL")]
    [Route("{*path}")]
    public IActionResult MakeCollection(string path)
    {
        path = path.Replace('\\', '/').Trim('/');
        _logger.LogInformation("WebDAV MKCOL: {path}", path);

        try
        {
            _cloudService.CreateLocalDirectory(path);
            return Created($"/dav/{path}", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MKCOL failed: {path}", path);
            return StatusCode(500, ex.Message);
        }
    }
}
