using FluentAssertions;
using Illumin360.Billing.Application.Abstractions;
using Illumin360.Billing.Application.Billing;
using Illumin360.Billing.Domain;
using Illumin360.Billing.Infrastructure.Providers;
using NSubstitute;
using Xunit;

namespace Illumin360.Billing.UnitTests;

public class SubscriptionTests
{
    private static Plan ProPlan()
        => Plan.Create("pro", "Pro", 50000, "NAD", BillingInterval.Monthly, ["ats.advanced", "reports.export"], DateTimeOffset.UnixEpoch).Value!;

    [Fact]
    public void Plan_requires_a_valid_currency_and_records_features()
    {
        Plan.Create("pro", "Pro", 1000, "dollars", BillingInterval.Monthly, null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        var plan = ProPlan();
        plan.Features.Should().BeEquivalentTo("ats.advanced", "reports.export");
        plan.NextPeriodEnd(DateTimeOffset.UnixEpoch).Should().Be(DateTimeOffset.UnixEpoch.AddMonths(1));
    }

    [Fact]
    public async Task Subscribe_starts_active_and_records_a_paid_first_invoice()
    {
        var plan = ProPlan();
        var customer = Guid.NewGuid();
        var repo = Substitute.For<IBillingRepository>();
        repo.GetPlanByCodeAsync("pro", Arg.Any<CancellationToken>()).Returns(plan);
        repo.GetActiveSubscriptionForCustomerAsync(customer, Arg.Any<CancellationToken>()).Returns((Subscription?)null);
        Invoice? invoice = null;
        repo.When(r => r.AddInvoice(Arg.Any<Invoice>())).Do(ci => invoice = ci.Arg<Invoice>());

        var result = await new SubscribeCommandHandler(repo, new FakeBillingProvider()).HandleAsync(new SubscribeCommand(customer, "pro"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Active");
        repo.Received(1).AddSubscription(Arg.Any<Subscription>());
        invoice!.Status.Should().Be(InvoiceStatus.Paid);
        invoice.AmountMinor.Should().Be(50000);
    }

    [Fact]
    public async Task Subscribe_conflicts_if_the_customer_already_has_one()
    {
        var customer = Guid.NewGuid();
        var repo = Substitute.For<IBillingRepository>();
        repo.GetPlanByCodeAsync("pro", Arg.Any<CancellationToken>()).Returns(ProPlan());
        repo.GetActiveSubscriptionForCustomerAsync(customer, Arg.Any<CancellationToken>())
            .Returns(Subscription.Start(customer, Guid.NewGuid(), DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddMonths(1), DateTimeOffset.UnixEpoch).Value!);

        var result = await new SubscribeCommandHandler(repo, new FakeBillingProvider()).HandleAsync(new SubscribeCommand(customer, "pro"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("subscription.exists");
    }

    [Fact]
    public async Task Entitlements_reflect_the_active_plan_features()
    {
        var plan = ProPlan();
        var customer = Guid.NewGuid();
        var sub = Subscription.Start(customer, plan.Id, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddMonths(1), DateTimeOffset.UnixEpoch).Value!;
        var repo = Substitute.For<IBillingRepository>();
        repo.GetActiveSubscriptionForCustomerAsync(customer, Arg.Any<CancellationToken>()).Returns(sub);
        repo.GetPlanAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        var result = await new GetEntitlementsQueryHandler(repo).HandleAsync(new GetEntitlementsQuery(customer), CancellationToken.None);

        result.Value!.PlanCode.Should().Be("pro");
        result.Value!.Features.Should().Contain("ats.advanced");
    }

    [Fact]
    public async Task No_subscription_grants_no_entitlements()
    {
        var repo = Substitute.For<IBillingRepository>();
        repo.GetActiveSubscriptionForCustomerAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Subscription?)null);

        var result = await new GetEntitlementsQueryHandler(repo).HandleAsync(new GetEntitlementsQuery(Guid.NewGuid()), CancellationToken.None);

        result.Value!.Features.Should().BeEmpty();
        result.Value!.Status.Should().Be("none");
    }

    [Fact]
    public async Task Runner_renews_a_due_subscription_on_a_successful_charge()
    {
        var plan = ProPlan();
        var due = Subscription.Start(Guid.NewGuid(), plan.Id, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddMonths(1), DateTimeOffset.UnixEpoch).Value!;
        var repo = Substitute.For<IBillingRepository>();
        repo.ListDueSubscriptionsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(new[] { due });
        repo.GetPlanAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        Invoice? invoice = null;
        repo.When(r => r.AddInvoice(Arg.Any<Invoice>())).Do(ci => invoice = ci.Arg<Invoice>());

        var charged = await new BillingRunner(repo, new FakeBillingProvider()).RunOnceAsync(DateTimeOffset.UnixEpoch.AddMonths(2), CancellationToken.None);

        charged.Should().Be(1);
        invoice!.Status.Should().Be(InvoiceStatus.Paid);
        due.Status.Should().Be(SubscriptionStatus.Active);
        due.CurrentPeriodEnd.Should().Be(DateTimeOffset.UnixEpoch.AddMonths(2));
    }

    [Fact]
    public async Task Runner_marks_past_due_when_the_charge_fails()
    {
        var plan = ProPlan();
        var due = Subscription.Start(Guid.NewGuid(), plan.Id, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddMonths(1), DateTimeOffset.UnixEpoch).Value!;
        var repo = Substitute.For<IBillingRepository>();
        repo.ListDueSubscriptionsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(new[] { due });
        repo.GetPlanAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        var provider = Substitute.For<IBillingProvider>();
        provider.ChargeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new BillingResult(false, string.Empty, Error: "card declined"));

        var charged = await new BillingRunner(repo, provider).RunOnceAsync(DateTimeOffset.UnixEpoch.AddMonths(2), CancellationToken.None);

        charged.Should().Be(0);
        due.Status.Should().Be(SubscriptionStatus.PastDue);
    }
}
