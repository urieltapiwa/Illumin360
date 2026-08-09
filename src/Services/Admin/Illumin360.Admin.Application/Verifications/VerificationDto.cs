using Illumin360.Admin.Domain;

namespace Illumin360.Admin.Application.Verifications;

/// <summary>Transport DTO for a verification (matches the Admin portal's verification-panel contract).</summary>
/// <param name="Id">Verification id.</param>
/// <param name="Entity">Entity under review.</param>
/// <param name="Kind">Kind of verification.</param>
/// <param name="Risk">Risk band (Low/Medium/High).</param>
/// <param name="Submitted">Relative submitted-time label.</param>
/// <param name="Status">Decision state (pending/approved/rejected).</param>
/// <param name="DecidedBy">Deciding admin username, if decided.</param>
public sealed record VerificationDto(
    Guid Id,
    string Entity,
    string Kind,
    string Risk,
    string Submitted,
    string Status,
    string? DecidedBy)
{
    /// <summary>Projects a domain <see cref="Verification"/> into the transport DTO.</summary>
    /// <param name="v">The verification.</param>
    /// <returns>The transport DTO.</returns>
    public static VerificationDto FromDomain(Verification v)
    {
        ArgumentNullException.ThrowIfNull(v);
        var status = v.Status switch
        {
            VerificationStatus.Approved => "approved",
            VerificationStatus.Rejected => "rejected",
            _ => "pending",
        };

        return new VerificationDto(
            v.Id.Value,
            v.Entity,
            v.Kind,
            v.RiskLevel,
            v.SubmittedLabel,
            status,
            v.DecidedBy);
    }
}
