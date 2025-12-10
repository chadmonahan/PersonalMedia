using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalMedia.Core.Entities;
using PersonalMedia.Data;

namespace PersonalMedia.Blazor.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MediaApiController : ControllerBase
{
    private readonly PersonalMediaDbContext _context;
    private readonly IConfiguration _configuration;

    public MediaApiController(PersonalMediaDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // ========== Authentication ==========

    [HttpPost("signin")]
    public IActionResult SignIn([FromBody] SignInRequest request)
    {
        var validCode = _configuration["AppSettings:AccessCode"] ?? "1234";

        if (request.Code == validCode)
        {
            HttpContext.Session.SetString("Authenticated", "true");
            return Ok(new { success = true, message = "Authentication successful" });
        }

        return Unauthorized(new { success = false, message = "Invalid code" });
    }

    [HttpPost("signout")]
    public IActionResult SignOutUser()
    {
        HttpContext.Session.Clear();
        return Ok(new { success = true, message = "Signed out successfully" });
    }

    [HttpGet("authenticated")]
    public IActionResult CheckAuthentication()
    {
        var isAuthenticated = HttpContext.Session.GetString("Authenticated") == "true";
        return Ok(new { authenticated = isAuthenticated });
    }

    // ========== MediaSets ==========

    [HttpGet("sets")]
    public async Task<IActionResult> GetMediaSets([FromQuery] bool includeInactive = false)
    {
        if (HttpContext.Session.GetString("Authenticated") != "true")
        {
            return Unauthorized();
        }

        var query = _context.MediaSets
            .Include(s => s.MediaItems.OrderBy(m => m.DisplayOrder))
            .ThenInclude(m => m.Reactions)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        var mediaSets = await query
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync();

        return Ok(mediaSets);
    }

    [HttpGet("sets/{id}")]
    public async Task<IActionResult> GetMediaSet(int id)
    {
        if (HttpContext.Session.GetString("Authenticated") != "true")
        {
            return Unauthorized();
        }

        var mediaSet = await _context.MediaSets
            .Include(s => s.MediaItems.OrderBy(m => m.DisplayOrder))
            .ThenInclude(m => m.Reactions)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (mediaSet == null)
        {
            return NotFound(new { message = "Media set not found" });
        }

        return Ok(mediaSet);
    }

    [HttpPost("sets")]
    public async Task<IActionResult> CreateMediaSet([FromBody] CreateMediaSetRequest request)
    {
        if (HttpContext.Session.GetString("Authenticated") != "true")
        {
            return Unauthorized();
        }

        var mediaSet = new MediaSet
        {
            CreatedDate = DateTime.UtcNow,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive
        };

        _context.MediaSets.Add(mediaSet);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMediaSet), new { id = mediaSet.Id }, mediaSet);
    }

    [HttpPut("sets/{id}")]
    public async Task<IActionResult> UpdateMediaSet(int id, [FromBody] UpdateMediaSetRequest request)
    {
        if (HttpContext.Session.GetString("Authenticated") != "true")
        {
            return Unauthorized();
        }

        var mediaSet = await _context.MediaSets.FindAsync(id);

        if (mediaSet == null)
        {
            return NotFound(new { message = "Media set not found" });
        }

        mediaSet.DisplayOrder = request.DisplayOrder;
        mediaSet.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return Ok(mediaSet);
    }

    [HttpDelete("sets/{id}")]
    public async Task<IActionResult> DeleteMediaSet(int id)
    {
        if (HttpContext.Session.GetString("Authenticated") != "true")
        {
            return Unauthorized();
        }

        var mediaSet = await _context.MediaSets
            .Include(s => s.MediaItems)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (mediaSet == null)
        {
            return NotFound(new { message = "Media set not found" });
        }

        _context.MediaSets.Remove(mediaSet);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Media set deleted successfully" });
    }

    // ========== MediaItems ==========

    [HttpGet("items")]
    public async Task<IActionResult> GetMediaItems([FromQuery] int? mediaSetId = null, [FromQuery] bool includeInactive = false)
    {
        if (HttpContext.Session.GetString("Authenticated") != "true")
        {
            return Unauthorized();
        }

        var query = _context.MediaItems
            .Include(m => m.Reactions)
            .Include(m => m.BasePersonImage)
            .AsQueryable();

        if (mediaSetId.HasValue)
        {
            query = query.Where(m => m.MediaSetId == mediaSetId.Value);
        }

        if (!includeInactive)
        {
            query = query.Where(m => m.IsActive);
        }

        var mediaItems = await query
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();

        return Ok(mediaItems);
    }

    [HttpGet("items/{id}")]
    public async Task<IActionResult> GetMediaItem(int id)
    {
        if (HttpContext.Session.GetString("Authenticated") != "true")
        {
            return Unauthorized();
        }

        var mediaItem = await _context.MediaItems
            .Include(m => m.Reactions)
            .Include(m => m.BasePersonImage)
            .Include(m => m.GenerationParameters)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mediaItem == null)
        {
            return NotFound(new { message = "Media item not found" });
        }

        return Ok(mediaItem);
    }

    [HttpPost("items")]
    public async Task<IActionResult> CreateMediaItem([FromBody] CreateMediaItemRequest request)
    {
        if (HttpContext.Session.GetString("Authenticated") != "true")
        {
            return Unauthorized();
        }

        var mediaItem = new MediaItem
        {
            MediaSetId = request.MediaSetId,
            MediaType = request.MediaType,
            AzureStorageUrl = request.AzureStorageUrl,
            ThumbnailUrl = request.ThumbnailUrl,
            CreatedDate = DateTime.UtcNow,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            BasePersonImageId = request.BasePersonImageId,
            GenerationPrompt = request.GenerationPrompt,
            GenerationStatus = request.GenerationStatus
        };

        _context.MediaItems.Add(mediaItem);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMediaItem), new { id = mediaItem.Id }, mediaItem);
    }

    [HttpPut("items/{id}")]
    public async Task<IActionResult> UpdateMediaItem(int id, [FromBody] UpdateMediaItemRequest request)
    {
        if (HttpContext.Session.GetString("Authenticated") != "true")
        {
            return Unauthorized();
        }

        var mediaItem = await _context.MediaItems.FindAsync(id);

        if (mediaItem == null)
        {
            return NotFound(new { message = "Media item not found" });
        }

        mediaItem.DisplayOrder = request.DisplayOrder;
        mediaItem.IsActive = request.IsActive;
        mediaItem.AzureStorageUrl = request.AzureStorageUrl ?? mediaItem.AzureStorageUrl;
        mediaItem.ThumbnailUrl = request.ThumbnailUrl ?? mediaItem.ThumbnailUrl;
        mediaItem.GenerationPrompt = request.GenerationPrompt ?? mediaItem.GenerationPrompt;

        await _context.SaveChangesAsync();

        return Ok(mediaItem);
    }

    [HttpDelete("items/{id}")]
    public async Task<IActionResult> DeleteMediaItem(int id)
    {
        if (HttpContext.Session.GetString("Authenticated") != "true")
        {
            return Unauthorized();
        }

        var mediaItem = await _context.MediaItems.FindAsync(id);

        if (mediaItem == null)
        {
            return NotFound(new { message = "Media item not found" });
        }

        _context.MediaItems.Remove(mediaItem);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Media item deleted successfully" });
    }

    // ========== Reactions ==========

    [HttpPost("react")]
    public async Task<IActionResult> ToggleReaction([FromBody] ReactionRequest request)
    {
        if (HttpContext.Session.GetString("Authenticated") != "true")
        {
            return Unauthorized();
        }

        var mediaItem = await _context.MediaItems
            .Include(m => m.Reactions)
            .FirstOrDefaultAsync(m => m.Id == request.MediaItemId);

        if (mediaItem == null)
        {
            return NotFound(new { message = "Media item not found" });
        }

        var reactionType = request.ReactionType.ToLower() == "like"
            ? ReactionType.Like
            : ReactionType.Dislike;

        var oppositeType = reactionType == ReactionType.Like
            ? ReactionType.Dislike
            : ReactionType.Like;

        var existingReaction = mediaItem.Reactions
            .FirstOrDefault(r => r.ReactionType == reactionType);

        var oppositeReaction = mediaItem.Reactions
            .FirstOrDefault(r => r.ReactionType == oppositeType);

        if (oppositeReaction != null)
        {
            _context.MediaReactions.Remove(oppositeReaction);
        }

        if (existingReaction != null)
        {
            _context.MediaReactions.Remove(existingReaction);
        }
        else
        {
            _context.MediaReactions.Add(new MediaReaction
            {
                MediaItemId = request.MediaItemId,
                ReactionType = reactionType,
                CreatedDate = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        var hasLike = mediaItem.Reactions.Any(r => r.ReactionType == ReactionType.Like && r.Id != existingReaction?.Id && r.Id != oppositeReaction?.Id)
            || (existingReaction == null && reactionType == ReactionType.Like);

        var hasDislike = mediaItem.Reactions.Any(r => r.ReactionType == ReactionType.Dislike && r.Id != existingReaction?.Id && r.Id != oppositeReaction?.Id)
            || (existingReaction == null && reactionType == ReactionType.Dislike);

        return Ok(new
        {
            hasLike,
            hasDislike
        });
    }

    [HttpGet("items/{id}/reactions")]
    public async Task<IActionResult> GetReactions(int id)
    {
        if (HttpContext.Session.GetString("Authenticated") != "true")
        {
            return Unauthorized();
        }

        var mediaItem = await _context.MediaItems
            .Include(m => m.Reactions)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mediaItem == null)
        {
            return NotFound(new { message = "Media item not found" });
        }

        var hasLike = mediaItem.Reactions.Any(r => r.ReactionType == ReactionType.Like);
        var hasDislike = mediaItem.Reactions.Any(r => r.ReactionType == ReactionType.Dislike);

        return Ok(new
        {
            mediaItemId = id,
            hasLike,
            hasDislike,
            reactions = mediaItem.Reactions
        });
    }
}

// ========== Request/Response Models ==========

public class SignInRequest
{
    public string Code { get; set; } = string.Empty;
}

public class CreateMediaSetRequest
{
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateMediaSetRequest
{
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateMediaItemRequest
{
    public int MediaSetId { get; set; }
    public MediaType MediaType { get; set; }
    public string? AzureStorageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int? BasePersonImageId { get; set; }
    public string? GenerationPrompt { get; set; }
    public GenerationStatus GenerationStatus { get; set; } = GenerationStatus.Pending;
}

public class UpdateMediaItemRequest
{
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public string? AzureStorageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? GenerationPrompt { get; set; }
}

public class ReactionRequest
{
    public int MediaItemId { get; set; }
    public string ReactionType { get; set; } = string.Empty;
}
