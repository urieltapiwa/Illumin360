using Illumin360.Billing.Application.Billing;

namespace Illumin360.Billing.Api;

/// <summary>
/// Periodically charges due subscription renewals (interval + enablement via the <c>Billing</c> config section).
/// Each tick runs in its own DI scope, wrapped in try/catch so a transient failure never tears down the host.
/// Mirrors the recruitment JobAlert/Nurture schedulers.
/// </summary>
/// <param name="scopeFactory">Scope factory for per-tick service resolution.</param>
/// <param name="configuration">App configuration.</param>
/// <param name="logger">Logger.</param>
public sealed partial class BillingScheduler(
    IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<BillingScheduler> logger)
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<BillingScheduler> _logger = logger;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seconds = _configuration.GetValue<int?>("Billing:IntervalSeconds") ?? 3600;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(30, seconds)));

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var runner = scope.ServiceProvider.GetRequiredService<BillingRunner>();
                var charged = await runner.RunOnceAsync(DateTimeOffset.UtcNow, stoppingToken).ConfigureAwait(false);
                LogRun(charged);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Billing run charged {Charged} renewal(s).")]
    private partial void LogRun(int charged);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Billing run failed; will retry next tick.")]
    private partial void LogFailed(Exception ex);
}
