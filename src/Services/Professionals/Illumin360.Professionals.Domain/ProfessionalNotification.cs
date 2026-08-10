using Illumin360.SharedKernel;

namespace Illumin360.Professionals.Domain;

/// <summary>An in-app notification for a professional (application updates, job alerts, etc.).</summary>
public sealed class ProfessionalNotification : Entity<Guid>
{
    private ProfessionalNotification(Guid id)
        : base(id)
    {
    }

    /// <summary>Creates an unread notification.</summary>
    /// <param name="id">Row identity.</param>
    /// <param name="professionalId">Recipient professional.</param>
    /// <param name="kind">Category (e.g. "application", "job-alert").</param>
    /// <param name="text">Human-readable text.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    public ProfessionalNotification(Guid id, ProfessionalId professionalId, string kind, string text, DateTimeOffset createdAt)
        : base(id)
    {
        ProfessionalId = professionalId;
        Kind = kind;
        Text = text;
        CreatedAt = createdAt;
    }

    /// <summary>Recipient professional.</summary>
    public ProfessionalId ProfessionalId { get; private set; }

    /// <summary>Category tag.</summary>
    public string Kind { get; private set; } = string.Empty;

    /// <summary>Human-readable text.</summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>Whether the recipient has read it.</summary>
    public bool IsRead { get; private set; }

    /// <summary>When it was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Marks the notification as read.</summary>
    public void MarkRead() => IsRead = true;
}
