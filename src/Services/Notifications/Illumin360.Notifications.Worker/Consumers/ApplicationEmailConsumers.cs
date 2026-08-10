using Illumin360.Email;
using Illumin360.Recruitment.IntegrationEvents;
using MassTransit;

namespace Illumin360.Notifications.Worker.Consumers;

/// <summary>Emails an applicant to acknowledge a freshly-submitted application.</summary>
/// <param name="emailSender">The SMTP email sender.</param>
/// <param name="logger">Logger.</param>
public sealed partial class ApplicationSubmittedConsumer(IEmailSender emailSender, ILogger<ApplicationSubmittedConsumer> logger)
    : IConsumer<ApplicationSubmitted>
{
    private readonly IEmailSender _emailSender = emailSender;
    private readonly ILogger<ApplicationSubmittedConsumer> _logger = logger;

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ApplicationSubmitted> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var msg = context.Message;
        var email = NotificationTemplates.ApplicationReceived();
        await _emailSender.SendAsync(ToAddress(msg.TalentType, msg.TalentId), email.Subject, email.HtmlBody, context.CancellationToken).ConfigureAwait(false);
        LogAck(msg.ApplicationId, msg.TalentId);
    }

    // Demo recipient derived from the talent id; production resolves the real address from the profile.
    private static string ToAddress(string talentType, Guid talentId) =>
        $"{(string.IsNullOrWhiteSpace(talentType) ? "talent" : talentType)}+{talentId}@illumin360.test";

    [LoggerMessage(Level = LogLevel.Information, Message = "Application-received email sent for {ApplicationId} (talent {TalentId}).")]
    private partial void LogAck(Guid applicationId, Guid talentId);
}

/// <summary>Emails an applicant when a recruiter advances or rejects their application.</summary>
/// <param name="emailSender">The SMTP email sender.</param>
/// <param name="logger">Logger.</param>
public sealed partial class ApplicationStatusChangedConsumer(IEmailSender emailSender, ILogger<ApplicationStatusChangedConsumer> logger)
    : IConsumer<ApplicationStatusChanged>
{
    private readonly IEmailSender _emailSender = emailSender;
    private readonly ILogger<ApplicationStatusChangedConsumer> _logger = logger;

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ApplicationStatusChanged> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var msg = context.Message;
        var email = NotificationTemplates.ApplicationStatusChanged(roleTitle: string.Empty, status: msg.Status);
        await _emailSender.SendAsync(ToAddress(msg.TalentType, msg.TalentId), email.Subject, email.HtmlBody, context.CancellationToken).ConfigureAwait(false);
        LogStatus(msg.ApplicationId, msg.Status);
    }

    private static string ToAddress(string talentType, Guid talentId) =>
        $"{(string.IsNullOrWhiteSpace(talentType) ? "talent" : talentType)}+{talentId}@illumin360.test";

    [LoggerMessage(Level = LogLevel.Information, Message = "Application-status email sent for {ApplicationId} (now {Status}).")]
    private partial void LogStatus(Guid applicationId, string status);
}
