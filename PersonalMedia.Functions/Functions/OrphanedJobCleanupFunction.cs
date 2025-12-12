using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersonalMedia.Core.Entities;
using PersonalMedia.Data;

namespace PersonalMedia.Functions.Functions;

public class OrphanedJobCleanupFunction
{
    private readonly ILogger<OrphanedJobCleanupFunction> _logger;
    private readonly PersonalMediaDbContext _context;
    private readonly IConfiguration _configuration;

    public OrphanedJobCleanupFunction(
        ILogger<OrphanedJobCleanupFunction> logger,
        PersonalMediaDbContext context,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _configuration = configuration;
    }

    // [Function("CleanupOrphanedJobs")] // Disabled for testing
    public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation("Starting orphaned job cleanup at: {Time}", DateTime.Now);

        try
        {
            var timeoutMinutes = _configuration.GetValue<int>("RunPod:TimeoutMinutes", 15);
            var cutoffTime = DateTime.UtcNow.AddMinutes(-timeoutMinutes);

            var orphanedJobs = await _context.MediaItems
                .Where(m =>
                    m.GenerationStatus == GenerationStatus.InProgress &&
                    m.JobSubmittedDate.HasValue &&
                    m.JobSubmittedDate.Value < cutoffTime &&
                    !m.WebhookReceivedDate.HasValue)
                .ToListAsync();

            if (orphanedJobs.Count == 0)
            {
                _logger.LogInformation("No orphaned jobs found");
                return;
            }

            _logger.LogWarning("Found {Count} orphaned jobs", orphanedJobs.Count);

            foreach (var job in orphanedJobs)
            {
                _logger.LogWarning(
                    "Marking orphaned job as Retrying: MediaItem {Id}, JobId {JobId}, Submitted {SubmittedDate}",
                    job.Id, job.RunPodJobId, job.JobSubmittedDate);

                job.GenerationStatus = GenerationStatus.Retrying;
                job.ErrorMessage = $"Webhook timeout - no response from RunPod after {timeoutMinutes} minutes";
                job.RetryCount++;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Marked {Count} orphaned jobs for retry", orphanedJobs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during orphaned job cleanup");
        }
    }
}
