using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace PersonalMedia.Functions.Functions;

public class TestTimerFunction
{
    private readonly ILogger<TestTimerFunction> _logger;

    public TestTimerFunction(ILogger<TestTimerFunction> logger)
    {
        _logger = logger;
    }

    // Runs at 11:00 PM Eastern Time (4:00 AM UTC)
    // Note: Azure Functions use UTC. Eastern is UTC-5 (EST) or UTC-4 (EDT)
    // Using 4 AM UTC which is 11 PM EST (adjust if daylight saving applies)
    [Function("TestTimerFunction")]
    public Task Run([TimerTrigger("0 0 4 * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation("Test timer function executed at: {Time} UTC", DateTime.UtcNow);
        _logger.LogInformation("This corresponds to approximately 11:00 PM Eastern Time");

        if (timerInfo.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next scheduled run: {NextRun}", timerInfo.ScheduleStatus.Next);
        }

        return Task.CompletedTask;
    }
}
