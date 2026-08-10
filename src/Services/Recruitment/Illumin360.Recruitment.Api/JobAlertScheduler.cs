using Illumin360.Recruitment.Application.Recruitment;

namespace Illumin360.Recruitment.Api;

/// <summary>
/// Periodically runs alert-enabled saved searches and publishes job-alert digests. Interval + enablement
/// are read from the <c>JobAlerts</c> configuration section. Each tick runs in its own DI scope and is
/// wrapped in try/catch so a transient failure (broker/DB blip) never tears down the host.
/// </summary>
/// <param name="scopeFactory">Scope factory for per-tick service resolution.</param>
/// <param name="configuration">App configuration.</param>
/// <param name="logger">Logger.</param>
public sealed partial class JobAlertScheduler(
    IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<JobAlertScheduler> logger)
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<JobAlertScheduler> _logger = logger;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seconds = _configuration.GetValue<int?>("JobAlerts:IntervalSeconds") ?? 3600;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(30, seconds)));

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var runner = scope.ServiceProvider.GetRequiredService<JobAlertRunner>();
                var published = await runner.RunOnceAsync(DateTimeOffset.UtcNow, stoppingToken).ConfigureAwait(false);
                LogRun(published);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogFailed(ex);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Job-alert run published {Published} digest(s).")]
    private partial void LogRun(int published);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Job-alert run failed; will retry next tick.")]
    private partial void LogFailed(Exception ex);
}
