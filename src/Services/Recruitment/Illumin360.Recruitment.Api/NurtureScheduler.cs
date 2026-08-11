using Illumin360.Recruitment.Application.Recruitment;

namespace Illumin360.Recruitment.Api;

/// <summary>
/// Periodically advances due nurture-sequence enrolments (sends the next step, schedules the following one).
/// Interval + enablement are read from the <c>Nurture</c> configuration section. Each tick runs in its own DI
/// scope and is wrapped in try/catch so a transient failure never tears down the host.
/// </summary>
/// <param name="scopeFactory">Scope factory for per-tick service resolution.</param>
/// <param name="configuration">App configuration.</param>
/// <param name="logger">Logger.</param>
public sealed partial class NurtureScheduler(
    IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<NurtureScheduler> logger)
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<NurtureScheduler> _logger = logger;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seconds = _configuration.GetValue<int?>("Nurture:IntervalSeconds") ?? 3600;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(30, seconds)));

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var runner = scope.ServiceProvider.GetRequiredService<NurtureRunner>();
                var sent = await runner.RunOnceAsync(DateTimeOffset.UtcNow, stoppingToken).ConfigureAwait(false);
                LogRun(sent);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Nurture run sent {Sent} step email(s).")]
    private partial void LogRun(int sent);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Nurture run failed; will retry next tick.")]
    private partial void LogFailed(Exception ex);
}
