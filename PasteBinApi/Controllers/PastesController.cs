using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PasteBinApi.Dto;
using PasteBinApi.Interfaces;

namespace PasteBinApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[EnableRateLimiting("default")]
public class PastesController : ControllerBase
{
    private readonly IPasteService _pasteService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<PastesController> _logger;

    public PastesController(
        IPasteService pasteService,
        IAnalyticsService analyticsService,
        ILogger<PastesController> logger)
    {
        _pasteService = pasteService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<PasteResponse>> CreatePaste([FromBody] CreatePasteRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var clientIp = GetClientIpAddress();
            var paste = await _pasteService.CreatePasteAsync(request, clientIp);

            return CreatedAtAction(nameof(GetPaste), new { id = paste.ShortId }, paste);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create paste");
            return StatusCode(500, new { error = "Failed to create paste" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PasteResponse>> GetPaste(string id, [FromHeader(Name = "X-Paste-Password")] string? password = null)
    {
        try
        {
            var paste = await _pasteService.GetPasteAsync(id, password);
            if (paste == null)
            {
                return NotFound(new { error = "Paste not found" });
            }

            // Record view asynchronously
            var clientIp = GetClientIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();
            var referer = Request.Headers.Referer.ToString();
            
            _ = Task.Run(async () => 
                await _analyticsService.RecordViewAsync(paste.Id, clientIp, userAgent, referer));

            return Ok(paste);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Password required or invalid" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get paste {Id}", id);
            return StatusCode(500, new { error = "Failed to retrieve paste" });
        }
    }

    [HttpGet("{id}/raw")]
    public async Task<IActionResult> GetRawPaste(string id)
    {
        try
        {
            var content = await _pasteService.GetRawContentAsync(id);
            return Content(content, "text/plain; charset=utf-8");
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Paste not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get raw paste {Id}", id);
            return StatusCode(500, "Failed to retrieve paste");
        }
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadPaste(string id)
    {
        try
        {
            var (content, filename) = await _pasteService.GetDownloadAsync(id);
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            
            return File(bytes, "application/octet-stream", filename);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Paste not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download paste {Id}", id);
            return StatusCode(500, "Failed to download paste");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePaste(string id)
    {
        try
        {
            await _pasteService.DeletePasteAsync(id);
            return Ok(new { message = "Paste deleted successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Paste not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete paste {Id}", id);
            return StatusCode(500, new { error = "Failed to delete paste" });
        }
    }

    [HttpGet("recent")]
    public async Task<ActionResult<IEnumerable<PasteListResponse>>> GetRecentPastes([FromQuery] int limit = 10)
    {
        try
        {
            limit = Math.Min(limit, 50); // Cap at 50
            var pastes = await _pasteService.GetRecentPublicPastesAsync(limit);
            return Ok(pastes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent pastes");
            return StatusCode(500, new { error = "Failed to retrieve recent pastes" });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<PasteListResponse>>> SearchPastes(
        [FromQuery] string q,
        [FromQuery] string? language = null,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { error = "Search query is required" });
            }

            limit = Math.Min(limit, 100); // Cap at 100
            var pastes = await _pasteService.SearchPastesAsync(q, language, limit, offset);
            return Ok(pastes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search pastes");
            return StatusCode(500, new { error = "Failed to search pastes" });
        }
    }

    [HttpGet("{id}/analytics")]
    public async Task<ActionResult<AnalyticsResponse>> GetPasteAnalytics(string id)
    {
        try
        {
            // This would typically require authentication/authorization
            var paste = await _pasteService.GetPasteAsync(id);
            if (paste == null)
            {
                return NotFound(new { error = "Paste not found" });
            }

            var analytics = await _analyticsService.GetPasteAnalyticsAsync(paste.Id);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get analytics for paste {Id}", id);
            return StatusCode(500, new { error = "Failed to retrieve analytics" });
        }
    }

    private string? GetClientIpAddress()
    {
        return Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()
            ?? Request.Headers["X-Real-IP"].FirstOrDefault()
            ?? Request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}