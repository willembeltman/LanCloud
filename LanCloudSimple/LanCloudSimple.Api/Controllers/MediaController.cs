//using LanCloudSimple.Api.Services;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.StaticFiles;

//namespace LanCloudSimple.Api.Controllers;

//[ApiController]
//[Route("api/[controller]")]
//public class MediaController : ControllerBase
//{
//    private readonly CloudService _mediaService;
//    private readonly ILogger<MediaController> _logger;

//    public MediaController(CloudService mediaService, ILogger<MediaController> logger)
//    {
//        _mediaService = mediaService;
//        _logger = logger;
//    }

//    [HttpGet("browse")]
//    public IActionResult Browse([FromQuery] string? path)
//    {
//        try
//        {
//            var nodes = _mediaService.Browse(path ?? "");
//            return Ok(nodes);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error browsing path {path}", path);
//            return StatusCode(500, ex.Message);
//        }
//    }

//    [HttpGet("file")]
//    public async Task<IActionResult> GetFile([FromQuery] string path)
//    {
//        if (string.IsNullOrEmpty(path))
//        {
//            return BadRequest("Path is required.");
//        }

//        try
//        {
//            var resolved = _mediaService.ResolveMergedPath(path);
//            if (resolved == null)
//            {
//                return NotFound($"File not found in index: {path}");
//            }

//            var result = await _mediaService.GetFileStreamAsync(resolved.Value.ClientId, resolved.Value.ClientPath);
//            if (result == null)
//            {
//                return NotFound($"File stream not available: {path}");
//            }

//            var provider = new FileExtensionContentTypeProvider();
//            if (!provider.TryGetContentType(path, out var contentType))
//            {
//                contentType = "application/octet-stream";
//            }

//            // Return the stream, ASP.NET Core will dispose it automatically
//            return File(result.Value.Stream, contentType, Path.GetFileName(path), enableRangeProcessing: true);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error getting file {path}", path);
//            return StatusCode(500, ex.Message);
//        }
//    }

//    [HttpPost("upload")]
//    [DisableRequestSizeLimit]
//    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string path)
//    {
//        if (file == null || file.Length == 0)
//        {
//            return BadRequest("No file was uploaded.");
//        }
//        if (string.IsNullOrEmpty(path))
//        {
//            return BadRequest("Path is required.");
//        }

//        try
//        {
//            // The path is the destination relative to API's local storage root
//            // E.g., "Folder1/newfile.jpg"
//            using var fileStream = file.OpenReadStream();
//            await _mediaService.SaveFileAsync(path, fileStream, CancellationToken.None);
//            _logger.LogInformation("Successfully uploaded file to local storage: {path}", path);
//            return Ok(new { Message = "File uploaded successfully.", Path = path });
//        }
//        catch (UnauthorizedAccessException ex)
//        {
//            return BadRequest(ex.Message);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error uploading file to {path}", path);
//            return StatusCode(500, ex.Message);
//        }
//    }
//}
