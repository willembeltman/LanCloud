using LanCloud.Api.Interfaces;
using LanCloud.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.Xml.Linq;

namespace LanCloud.Api.Controllers;

[ApiController]
[Route("dav")]
public class WebDavController(
    IFileSystem fileSystem,
    ILogger<WebDavController> logger)
    : ControllerBase
{
    [HttpOptions]
    [HttpOptions("{*path}")]
    public IActionResult Options()
    {
        Response.Headers.Append("DAV", "1, 2");
        Response.Headers.Append(
            "Allow",
            "OPTIONS, GET, HEAD, PROPFIND, PUT, DELETE, MKCOL");

        Response.Headers.Append("MS-Author-Via", "DAV");

        return Ok();
    }

    [AcceptVerbs("PROPFIND")]
    [Route("{*path}")]
    public async Task<IActionResult> PropFind(string? path, CancellationToken ct)
    {
        path = NormalizePath(path);

        var depth = Request.Headers["Depth"]
            .FirstOrDefault() ?? "1";

        logger.LogInformation(
            "PROPFIND '{Path}' (Depth: {Depth})",
            path,
            depth);

        var entry = await fileSystem.Get(path, ct);

        if (entry is null)
            return NotFound();

        var multistatus = new XElement(
            Dav + "multistatus");

        AddResponse(multistatus, entry);

        if (entry.IsDirectory && depth == "1")
        {
            await foreach (var child
                in fileSystem.ListDirectory(path, ct))
            {
                AddResponse(multistatus, child);
            }
        }

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            multistatus);

        await using var stream = new MemoryStream();

        document.Save(stream);
        stream.Position = 0;

        return File(
            stream,
            "application/xml; charset=utf-8");
    }

    [HttpGet("{*path}")]
    public async Task<IActionResult> Get(string? path, CancellationToken ct)
    {
        path = NormalizePath(path);

        if (string.IsNullOrEmpty(path))
            return Ok("WebDAV Root");

        var entry = await fileSystem.Get(path, ct);

        if (entry is null)
            return NotFound();

        if (entry.IsDirectory)
            return BadRequest();

        var stream = await fileSystem.OpenRead(path, ct);

        if (stream is null)
            return NotFound();

        var provider = new FileExtensionContentTypeProvider();

        if (!provider.TryGetContentType(
                entry.Name,
                out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return File(
            stream,
            contentType,
            entry.Name,
            enableRangeProcessing: true);
    }

    [HttpPut("{*path}")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Put(string path, CancellationToken ct)
    {
        path = NormalizePath(path);

        logger.LogInformation(
            "WebDAV PUT: {Path}",
            path);

        try
        {
            await fileSystem.Write(
                path,
                Request.Body,
                ct);

            return Created(
                GetDavUrl(path),
                null);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "PUT failed: {Path}",
                path);

            return StatusCode(500);
        }
    }

    [HttpDelete("{*path}")]
    public async Task<IActionResult> Delete(string path, CancellationToken ct)
    {
        path = NormalizePath(path);

        logger.LogInformation(
            "WebDAV DELETE: {Path}",
            path);

        var entry = await fileSystem.Get(path, ct);

        if (entry is null)
            return NotFound();

        try
        {
            await fileSystem.Delete(path, ct);

            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound();
        }
    }

    [AcceptVerbs("MKCOL")]
    [Route("{*path}")]
    public async Task<IActionResult> MakeCollection(string path, CancellationToken ct)
    {
        path = NormalizePath(path);

        logger.LogInformation(
            "WebDAV MKCOL: {Path}",
            path);

        try
        {
            await fileSystem.CreateDirectory(path, ct);

            return Created(
                GetDavUrl(path),
                null);
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "MKCOL failed: {Path}",
                path);

            return StatusCode(500);
        }
    }

    private static readonly XNamespace Dav = "DAV:";

    private void AddResponse(XElement multistatus, FileSystemEntry entry)
    {
        var href = GetDavUrl(entry.Path);

        if (entry.IsDirectory &&
            !href.EndsWith('/'))
        {
            href += "/";
        }

        var prop = new XElement(
            Dav + "prop",
            new XElement(
                Dav + "displayname",
                entry.Name));

        if (entry.IsDirectory)
        {
            prop.Add(
                new XElement(
                    Dav + "resourcetype",
                    new XElement(
                        Dav + "collection")));
        }
        else
        {
            prop.Add(
                new XElement(
                    Dav + "resourcetype"));

            prop.Add(
                new XElement(
                    Dav + "getcontentlength",
                    entry.Size));

            prop.Add(
                new XElement(
                    Dav + "getlastmodified",
                    entry.LastModified.ToUniversalTime()
                        .ToString("R")));

            prop.Add(
                new XElement(
                    Dav + "creationdate",
                    entry.Created.ToUniversalTime()
                        .ToString("yyyy-MM-ddTHH:mm:ssZ")));
        }

        multistatus.Add(
            new XElement(
                Dav + "response",
                new XElement(Dav + "href", href),
                new XElement(
                    Dav + "propstat",
                    prop,
                    new XElement(
                        Dav + "status",
                        "HTTP/1.1 200 OK"))));
    }
    private static string NormalizePath(string? path)
    {
        return (path ?? "")
            .Replace('\\', '/')
            .Trim('/');
    }
    private static string GetDavUrl(string path)
    {
        var segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString);

        return "/dav/" + string.Join("/", segments);
    }
}