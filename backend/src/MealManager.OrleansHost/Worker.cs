namespace MealManager.OrleansHost;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    // Placeholder host: the Orleans silo is wired into the API process in Phase 3 (skill bmm-orleans-grains);
    // this project is reserved for extracting silos to dedicated hosts when scaling requires it.
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MealManager.OrleansHost placeholder running — Orleans wiring lands in Phase 3");
        return Task.CompletedTask;
    }
}
