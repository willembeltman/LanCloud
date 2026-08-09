//using LanCloudSimple.Api.Services;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.StaticFiles;

//namespace LanCloudSimple.Api.Controllers;

//[ApiController]
//[Route("api/[controller]")]
//public class CloudController : ControllerBase
//{
//    private readonly CloudService _cloudService;
//    private readonly ILogger<CloudController> _logger;

//    public CloudController(CloudService cloudService, ILogger<CloudController> logger)
//    {
//        _cloudService = cloudService;
//        _logger = logger;
//    }

//    [HttpGet("browse")]
//    public IActionResult Browse([FromQuery] string? path)
//    {
//        try
//        {
//            return Ok(_cloudService.Browse(path ?? ""));
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error browsing path: {path}", path);
//            return StatusCode(500, ex.Message);
//        }
//    }

//    [HttpGet("file")]
//    public async Task<IActionResult> GetFile([FromQuery] string path)
//    {
//        if (string.IsNullOrEmpty(path))
//            return BadRequest("Path is required.");

//        try
//        {
//            var resolved = _cloudService.ResolveMergedPath(path);
//            if (resolved == null)
//                return NotFound($"File not found: {path}");

//            var result = await _cloudService.GetFileStreamAsync(resolved.Value.ClientId, resolved.Value.ClientPath);
//            if (result == null)
//                return NotFound($"File stream unavailable: {path}");

//            var provider = new FileExtensionContentTypeProvider();
//            if (!provider.TryGetContentType(path, out var contentType))
//                contentType = "application/octet-stream";

//            return File(result.Value.Stream, contentType, System.IO.Path.GetFileName(path), enableRangeProcessing: true);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error streaming file: {path}", path);
//            return StatusCode(500, ex.Message);
//        }
//    }

//    [HttpPost("upload")]
//    [DisableRequestSizeLimit]
//    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string path)
//    {
//        if (file == null || file.Length == 0)
//            return BadRequest("No file provided.");
//        if (string.IsNullOrEmpty(path))
//            return BadRequest("Path is required.");

//        try
//        {
//            await using var stream = file.OpenReadStream();
//            await _cloudService.SaveFileAsync(path, stream, CancellationToken.None);
//            _logger.LogInformation("Uploaded file to local storage: {path}", path);
//            return Ok(new { Message = "Upload successful.", Path = path });
//        }
//        catch (UnauthorizedAccessException ex)
//        {
//            return BadRequest(ex.Message);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error uploading file: {path}", path);
//            return StatusCode(500, ex.Message);
//        }
//    }
//}
