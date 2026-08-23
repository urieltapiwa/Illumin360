using System.Globalization;
using Illumin360.Billing.Application.Abstractions;
using Illumin360.Billing.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Billing.Application.Billing;

/// <summary>One month's monthly-recurring-revenue figure.</summary>
/// <param name="Label">Short month label (e.g. "Aug").</param>
/// <param name="MrrMinor">Monthly recurring revenue that month, in minor currency units.</param>
public sealed record MrrPointDto(string Label, long MrrMinor);

/// <summary>Monthly recurring revenue over the last six months.</summary>
/// <param name="Currency">ISO-4217 currency of the figures.</param>
/// <param name="Points">Six monthly points, oldest first.</param>
public sealed record MrrTrendDto(string Currency, IReadOnlyList<MrrPointDto> Points);

/// <summary>Platform MRR trend over the last six months.</summary>
public sealed record GetMrrTrendQuery : IQuery<MrrTrendDto>;

/// <summary>
/// Handles <see cref="GetMrrTrendQuery"/> by summing, for each of the last six calendar months, the
/// monthly-equivalent price of every non-cancelled subscription that existed by that month's end.
/// </summary>
/// <param name="repository">The billing repository.</param>
public sealed class GetMrrTrendQueryHandler(IBillingRepository repository) : IQueryHandler<GetMrrTrendQuery, MrrTrendDto>
{
    private readonly IBillingRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<MrrTrendDto>> HandleAsync(GetMrrTrendQuery query, CancellationToken cancellationToken)
    {
        var plans = (await _repository.ListPlansAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(p => p.Id);
        var subs = await _repository.ListAllSubscriptionsAsync(cancellationToken).ConfigureAwait(false);

        var currency = plans.Values.Select(p => p.Currency).FirstOrDefault() ?? "NAD";
        var now = DateTimeOffset.UtcNow;
        var firstOfThisMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
        var points = new List<MrrPointDto>(6);
        for (var i = 5; i >= 0; i--)
        {
            var monthEnd = firstOfThisMonth.AddMonths(1 - i).AddTicks(-1);
            long mrr = 0;
            foreach (var s in subs)
            {
                if (s.CreatedAt > monthEnd || s.Status == SubscriptionStatus.Canceled)
                {
                    continue;
                }

                if (plans.TryGetValue(s.PlanId, out var plan))
                {
                    mrr += plan.Interval == BillingInterval.Annual ? plan.PriceMinor / 12 : plan.PriceMinor;
                }
            }

            points.Add(new MrrPointDto(monthEnd.ToString("MMM", CultureInfo.InvariantCulture), mrr));
        }

        return new MrrTrendDto(currency, points);
    }
}
