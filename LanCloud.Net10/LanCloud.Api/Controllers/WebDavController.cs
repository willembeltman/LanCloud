using LanCloud.Api.Models;
using LanCloud.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.Text;
using System.Xml.Linq;

namespace LanCloud.Api.Controllers;

[ApiController]
[Route("dav")]
public class WebDavController(
    FileSystem fileSystem,
    ILogger<WebDavController> logger)
    : ControllerBase
{
    [HttpOptions]
    [HttpOptions("{*path}")]
    public IActionResult Options()
    {
        Response.Headers.Append("DAV", "1");
        Response.Headers.Append(
            "Allow",
            "OPTIONS, GET, HEAD, PROPFIND, PUT, DELETE, MKCOL, MOVE");

        Response.Headers.Append("MS-Author-Via", "DAV");

        return Ok();
    }

    [HttpHead("{*path}")]
    public async Task<IActionResult> Head(string? path, CancellationToken ct)
    {
        var authorizationResult = await AuthorizeDav(ct);
        if (authorizationResult is not null)
            return authorizationResult;

        path = NormalizePath(path);

        var entry = await fileSystem.Get(path, ct);

        if (entry is null)
            return NotFound();

        if (entry.IsDirectory)
            return BadRequest();

        var provider = new FileExtensionContentTypeProvider();

        if (!provider.TryGetContentType(
                entry.Name,
                out var contentType))
        {
            contentType = "application/octet-stream";
        }

        Response.ContentType = contentType;
        Response.ContentLength = entry.Size;

        return Ok();
    }

    [AcceptVerbs("PROPFIND")]
    [Route("")]
    [Route("{*path}")]
    public async Task<IActionResult> PropFind(string? path, CancellationToken ct)
    {
        var authorizationResult = await AuthorizeDav(ct);
        if (authorizationResult is not null)
            return authorizationResult;

        path = NormalizePath(path);

        var depth = Request.Headers["Depth"]
            .FirstOrDefault() ?? "0";

        logger.LogTrace(
            "PROPFIND '{Path}' (Depth: {Depth} Headers: {Headers})",
            path,
            depth,
            string.Join(", ", Request.Headers.Select(x => $"{x.Key}={x.Value}")));

        var entry = await fileSystem.Get(path, ct);

        if (entry is null)
            return NotFound();

        var multistatus = new XElement(
            Dav + "multistatus");

        AddResponse(multistatus, entry);

        if (entry.IsDirectory && depth != "0")
        {
            await foreach (var child in fileSystem.ListDirectory(path, ct))
            {
                AddResponse(multistatus, child);
            }
        }

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            multistatus);

        //logger.LogTrace(
        //    "PROPFIND response for '{Path}': {Xml}",
        //    path,
        //    document.ToString(SaveOptions.DisableFormatting));

        Response.StatusCode = StatusCodes.Status207MultiStatus;

        return Content(
            document.ToString(),
            "application/xml; charset=utf-8");
    }

    [HttpGet("{*path}")]
    public async Task<IActionResult> Get(string? path, CancellationToken ct)
    {
        var authorizationResult = await AuthorizeDav(ct);
        if (authorizationResult is not null)
            return authorizationResult;

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
        var authorizationResult = await AuthorizeDav(ct);
        if (authorizationResult is not null)
            return authorizationResult;

        path = NormalizePath(path);

        logger.LogTrace(
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
        var authorizationResult = await AuthorizeDav(ct);
        if (authorizationResult is not null)
            return authorizationResult;

        path = NormalizePath(path);

        logger.LogTrace(
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
        var authorizationResult = await AuthorizeDav(ct);
        if (authorizationResult is not null)
            return authorizationResult;

        path = NormalizePath(path);

        logger.LogTrace(
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

    [AcceptVerbs("MOVE")]
    [Route("{*path}")]
    public async Task<IActionResult> Move(string path, CancellationToken ct)
    {
        var authorizationResult = await AuthorizeDav(ct);
        if (authorizationResult is not null)
            return authorizationResult;

        path = NormalizePath(path);

        var destinationHeader = Request.Headers["Destination"].FirstOrDefault();
        if (string.IsNullOrEmpty(destinationHeader))
            return BadRequest("Missing Destination header.");

        var destUri = new Uri(destinationHeader);
        var destPath = NormalizePath(destUri.AbsolutePath);
        if (destPath.StartsWith("dav/", StringComparison.OrdinalIgnoreCase))
            destPath = destPath.Substring(4);
        else if (destPath.StartsWith("/dav/", StringComparison.OrdinalIgnoreCase))
            destPath = destPath.Substring(5);

        logger.LogTrace(
            "WebDAV MOVE: '{SourcePath}' -> '{DestPath}'",
            path,
            destPath);

        try
        {
            await fileSystem.Move(path, destPath, ct);
            return Created(GetDavUrl(destPath), null);
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
                "MOVE failed: '{SourcePath}' -> '{DestPath}'",
                path,
                destPath);

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
                entry.Name),

            new XElement(
                Dav + "creationdate",
                entry.Created
                    .ToUniversalTime()
                    .ToString("yyyy-MM-ddTHH:mm:ssZ")),

            new XElement(
                Dav + "getlastmodified",
                entry.LastModified
                    .ToUniversalTime()
                    .ToString("R")),

            new XElement(
                Dav + "getetag",
                CreateETag(entry)),

            new XElement(
                Dav + "resourcetype",
                entry.IsDirectory
                    ? new XElement(Dav + "collection")
                    : null));

        if (!entry.IsDirectory)
        {
            prop.Add(
                new XElement(
                    Dav + "getcontentlength",
                    entry.Size));

            prop.Add(
                new XElement(
                    Dav + "getcontenttype",
                    GetContentType(entry.Name)));
        }

        multistatus.Add(
            new XElement(
                Dav + "response",

                new XElement(
                    Dav + "href",
                    href),

                new XElement(
                    Dav + "propstat",
                    prop,

                    new XElement(
                        Dav + "status",
                        "HTTP/1.1 200 OK"))));
    }
    private static string CreateETag(FileSystemEntry entry)
    {
        return $"W/\"{entry.Size}-{entry.LastModified.Ticks}\"";
    }
    private static string GetContentType(string fileName)
    {
        var provider = new FileExtensionContentTypeProvider();

        return provider.TryGetContentType(
            fileName,
            out var contentType)
                ? contentType
                : "application/octet-stream";
    }
    private async Task<IActionResult?> AuthorizeDav(CancellationToken ct)
    {
        var auth = await fileSystem.GetAuthenticationInfo(ct);

        if (!auth.Required)
            return null;

        if (!Request.Headers.TryGetValue(
                "Authorization",
                out var header))
        {
            return UnauthorizedDav(auth.Realm);
        }

        var value = header.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(value) ||
            !value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return UnauthorizedDav(auth.Realm);
        }

        string decoded;

        try
        {
            decoded = Encoding.UTF8.GetString(
                Convert.FromBase64String(value["Basic ".Length..].Trim()));
        }
        catch (FormatException)
        {
            return UnauthorizedDav(auth.Realm);
        }

        var separator = decoded.IndexOf(':');

        if (separator < 0)
            return UnauthorizedDav(auth.Realm);

        var username = decoded[..separator];
        var password = decoded[(separator + 1)..];

        if (!await fileSystem.Authenticate(
                username,
                password,
                ct))
        {
            return UnauthorizedDav(auth.Realm);
        }

        return null;
    }
    private IActionResult UnauthorizedDav(string realm)
    {
        Response.Headers.WWWAuthenticate =
            $"Basic realm=\"{realm}\"";

        return Unauthorized();
    }
    private static string NormalizePath(string? path)
    {
        return (path ?? "")
            .Replace('\\', '/')
            .Trim('/');
    }
    private string GetDavUrl(string path)
    {
        var segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString);

        var relativePath = "/dav/" + string.Join("/", segments);

        return $"{Request.Scheme}://{Request.Host}{relativePath}";
    }


    //private static string CreateETag(FileSystemEntry entry)
    //{
    //    return $"\"{entry.Size:x}-{entry.LastModified.Ticks:x}\"";
    //}

    //[AcceptVerbs("PROPFIND")]
    //[Route("")]
    //[Route("{*path}")]
    //public async Task<IActionResult> PropFind(string? path, CancellationToken ct)
    //{
    //    var authorizationResult = await AuthorizeDav(ct);
    //    if (authorizationResult is not null)
    //        return authorizationResult;

    //    path = NormalizePath(path);

    //    var depth = Request.Headers["Depth"]
    //        .FirstOrDefault() ?? "1";

    //    logger.LogTrace(
    //        "PROPFIND '{Path}' (Depth: {Depth})",
    //        path,
    //        depth);

    //    var entry = await fileSystem.Get(path, ct);

    //    if (entry is null)
    //        return NotFound();

    //    var multistatus = new XElement(
    //        Dav + "multistatus");

    //    AddResponse(multistatus, entry);

    //    if (entry.IsDirectory && depth != "0")
    //    {
    //        await foreach (var child
    //            in fileSystem.ListDirectory(path, ct))
    //        {
    //            AddResponse(multistatus, child);
    //        }
    //    }

    //    var document = new XDocument(
    //        new XDeclaration("1.0", "utf-8", null),
    //        multistatus);

    //    var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" + document.ToString();

    //    Response.StatusCode = 207;
    //    return Content(xml, "application/xml; charset=utf-8");
    //}

    //private void AddResponse(XElement multistatus, FileSystemEntry entry)
    //{
    //    var href = GetDavUrl(entry.Path);

    //    if (entry.IsDirectory &&
    //        !href.EndsWith('/'))
    //    {
    //        href += "/";
    //    }

    //    var prop = new XElement(
    //        Dav + "prop",
    //        new XElement(
    //            Dav + "displayname",
    //            entry.Name));

    //    if (entry.IsDirectory)
    //    {
    //        prop.Add(
    //            new XElement(
    //                Dav + "resourcetype",
    //                new XElement(
    //                    Dav + "collection")));
    //    }
    //    else
    //    {
    //        prop.Add(
    //            new XElement(
    //                Dav + "resourcetype"));

    //        prop.Add(
    //            new XElement(
    //                Dav + "getcontentlength",
    //                entry.Size));

    //        prop.Add(
    //            new XElement(
    //                Dav + "getlastmodified",
    //                entry.LastModified.ToUniversalTime()
    //                    .ToString("R")));

    //        prop.Add(
    //            new XElement(
    //                Dav + "creationdate",
    //                entry.Created.ToUniversalTime()
    //                    .ToString("yyyy-MM-ddTHH:mm:ssZ")));
    //    }

    //    multistatus.Add(
    //        new XElement(
    //            Dav + "response",
    //            new XElement(Dav + "href", href),
    //            new XElement(
    //                Dav + "propstat",
    //                prop,
    //                new XElement(
    //                    Dav + "status",
    //                    "HTTP/1.1 200 OK"))));
    //}

    //private static string GetDavUrl(string path)
    //{
    //    var segments = path
    //        .Split('/', StringSplitOptions.RemoveEmptyEntries)
    //        .Select(Uri.EscapeDataString)
    //        .ToList();

    //    if (segments.Count == 0)
    //        return "/dav/";

    //    return "/dav/" + string.Join("/", segments);
    //}
    //private IActionResult UnauthorizedDav()
    //{
    //    Response.Headers.WWWAuthenticate =
    //        $"Basic realm=\"{davOptions.Value.Realm}\"";

    //    return Unauthorized();
    //}
}
