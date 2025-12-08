using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalMedia.Core.Entities;
using PersonalMedia.Data;

namespace PersonalMedia.Web.Controllers;

[Route("api/media")]
[ApiController]
public class MediaApiController : ControllerBase
{
    private readonly PersonalMediaDbContext _context;

    public MediaApiController(PersonalMediaDbContext context)
    {
        _context = context;
    }

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
            return NotFound();
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
}

public class ReactionRequest
{
    public int MediaItemId { get; set; }
    public string ReactionType { get; set; }
}
