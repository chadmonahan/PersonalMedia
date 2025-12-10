using Microsoft.EntityFrameworkCore;
using PersonalMedia.Core.Entities;
using PersonalMedia.Data;

namespace PersonalMedia.Blazor.Services;

public interface IMediaService
{
    Task<List<MediaSet>> GetMediaSetsAsync(bool includeInactive = false);
    Task<ReactionResult> ToggleReactionAsync(int mediaItemId, string reactionType);
}

public class ReactionResult
{
    public bool HasLike { get; set; }
    public bool HasDislike { get; set; }
}

public class MediaService : IMediaService
{
    private readonly PersonalMediaDbContext _context;

    public MediaService(PersonalMediaDbContext context)
    {
        _context = context;
    }

    public async Task<List<MediaSet>> GetMediaSetsAsync(bool includeInactive = false)
    {
        var query = _context.MediaSets
            .Include(s => s.MediaItems.Where(m => includeInactive || m.IsActive).OrderBy(m => m.DisplayOrder))
            .ThenInclude(m => m.Reactions)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync();
    }

    public async Task<ReactionResult> ToggleReactionAsync(int mediaItemId, string reactionType)
    {
        var mediaItem = await _context.MediaItems
            .Include(m => m.Reactions)
            .FirstOrDefaultAsync(m => m.Id == mediaItemId);

        if (mediaItem == null)
        {
            throw new InvalidOperationException("Media item not found");
        }

        var type = reactionType.ToLower() == "like"
            ? ReactionType.Like
            : ReactionType.Dislike;

        var oppositeType = type == ReactionType.Like
            ? ReactionType.Dislike
            : ReactionType.Like;

        var existingReaction = mediaItem.Reactions
            .FirstOrDefault(r => r.ReactionType == type);

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
                MediaItemId = mediaItemId,
                ReactionType = type,
                CreatedDate = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        var hasLike = mediaItem.Reactions.Any(r => r.ReactionType == ReactionType.Like && r.Id != existingReaction?.Id && r.Id != oppositeReaction?.Id)
            || (existingReaction == null && type == ReactionType.Like);

        var hasDislike = mediaItem.Reactions.Any(r => r.ReactionType == ReactionType.Dislike && r.Id != existingReaction?.Id && r.Id != oppositeReaction?.Id)
            || (existingReaction == null && type == ReactionType.Dislike);

        return new ReactionResult
        {
            HasLike = hasLike,
            HasDislike = hasDislike
        };
    }
}
