using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>Enrichment for a requisition — salary range, type, remote flag and tags.</summary>
/// <param name="SalaryMin">Lower salary bound, if set.</param>
/// <param name="SalaryMax">Upper salary bound, if set.</param>
/// <param name="Currency">Currency code.</param>
/// <param name="EmploymentType">Employment type (fulltime/parttime/contract/internship/temporary).</param>
/// <param name="Remote">Whether remote.</param>
/// <param name="Internal">Whether internal-only (hidden from public careers).</param>
/// <param name="Tags">Category tags.</param>
public sealed record RequisitionDetailDto(int? SalaryMin, int? SalaryMax, string Currency, string EmploymentType, bool Remote, bool Internal, IReadOnlyList<string> Tags)
{
    /// <summary>Projects a domain detail + tags into the transport DTO.</summary>
    /// <param name="d">The detail, or null if none set yet.</param>
    /// <param name="tags">The requisition's tags.</param>
    /// <returns>The transport DTO (with sensible defaults when no detail exists).</returns>
    public static RequisitionDetailDto FromDomain(RequisitionDetail? d, IReadOnlyList<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        return d is null
            ? new RequisitionDetailDto(null, null, "NAD", Domain.EmploymentType.FullTime.ToWire(), false, false, tags)
            : new RequisitionDetailDto(d.SalaryMin, d.SalaryMax, d.Currency, d.EmploymentType.ToWire(), d.Remote, d.Internal, tags);
    }
}

/// <summary>Gets a requisition's enrichment detail + tags.</summary>
/// <param name="RequestId">The requisition id.</param>
public sealed record GetRequisitionDetailQuery(Guid RequestId) : IQuery<RequisitionDetailDto>;

/// <summary>Upserts a requisition's enrichment detail.</summary>
/// <param name="RequestId">The requisition id.</param>
/// <param name="SalaryMin">Lower salary bound.</param>
/// <param name="SalaryMax">Upper salary bound.</param>
/// <param name="Currency">Currency code.</param>
/// <param name="EmploymentType">Employment-type name.</param>
/// <param name="Remote">Whether remote.</param>
public sealed record SetRequisitionDetailCommand(Guid RequestId, int? SalaryMin, int? SalaryMax, string? Currency, string? EmploymentType, bool Remote) : ICommand<RequisitionDetailDto>;

/// <summary>Sets a requisition's internal-only visibility (upserts the detail if absent).</summary>
/// <param name="RequestId">The requisition id.</param>
/// <param name="Internal">True to hide from the public careers site.</param>
public sealed record SetRequisitionInternalCommand(Guid RequestId, bool Internal) : ICommand<RequisitionDetailDto>;

/// <summary>Adds a tag to a requisition (idempotent).</summary>
/// <param name="RequestId">The requisition id.</param>
/// <param name="Label">The tag label.</param>
public sealed record AddRequisitionTagCommand(Guid RequestId, string Label) : ICommand<IReadOnlyList<string>>;

/// <summary>Removes a tag from a requisition.</summary>
/// <param name="RequestId">The requisition id.</param>
/// <param name="Label">The tag label.</param>
public sealed record RemoveRequisitionTagCommand(Guid RequestId, string Label) : ICommand<IReadOnlyList<string>>;

/// <summary>Handles <see cref="GetRequisitionDetailQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetRequisitionDetailQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetRequisitionDetailQuery, RequisitionDetailDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<RequisitionDetailDto>> HandleAsync(GetRequisitionDetailQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var detail = await _repository.GetRequisitionDetailAsync(query.RequestId, cancellationToken).ConfigureAwait(false);
        var tags = await _repository.ListRequisitionTagsAsync(query.RequestId, cancellationToken).ConfigureAwait(false);
        return RequisitionDetailDto.FromDomain(detail, tags.Select(t => t.Label).ToList());
    }
}

/// <summary>Handles <see cref="SetRequisitionDetailCommand"/> (create or update).</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class SetRequisitionDetailCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<SetRequisitionDetailCommand, RequisitionDetailDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<RequisitionDetailDto>> HandleAsync(SetRequisitionDetailCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = await _repository.GetByIdAsync(new RequestId(command.RequestId), cancellationToken).ConfigureAwait(false);
        if (request is null)
        {
            return Error.NotFound("request.not_found", "No matching requisition was found.");
        }

        var existing = await _repository.GetRequisitionDetailAsync(command.RequestId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            var creation = RequisitionDetail.Create(command.RequestId, command.SalaryMin, command.SalaryMax, command.Currency, command.EmploymentType, command.Remote, DateTimeOffset.UtcNow);
            if (creation.IsFailure)
            {
                return creation.Error!;
            }

            _repository.AddRequisitionDetail(creation.Value!);
        }
        else
        {
            var update = existing.Update(command.SalaryMin, command.SalaryMax, command.Currency, command.EmploymentType, command.Remote);
            if (update.IsFailure)
            {
                return update.Error!;
            }
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var tags = await _repository.ListRequisitionTagsAsync(command.RequestId, cancellationToken).ConfigureAwait(false);
        var detail = await _repository.GetRequisitionDetailAsync(command.RequestId, cancellationToken).ConfigureAwait(false);
        return RequisitionDetailDto.FromDomain(detail, tags.Select(t => t.Label).ToList());
    }
}

/// <summary>Handles <see cref="SetRequisitionInternalCommand"/> — upserts the detail and sets its internal flag.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class SetRequisitionInternalCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<SetRequisitionInternalCommand, RequisitionDetailDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<RequisitionDetailDto>> HandleAsync(SetRequisitionInternalCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = await _repository.GetByIdAsync(new RequestId(command.RequestId), cancellationToken).ConfigureAwait(false);
        if (request is null)
        {
            return Error.NotFound("request.not_found", "No matching requisition was found.");
        }

        var existing = await _repository.GetRequisitionDetailAsync(command.RequestId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            var creation = RequisitionDetail.Create(command.RequestId, null, null, null, null, false, DateTimeOffset.UtcNow);
            if (creation.IsFailure)
            {
                return creation.Error!;
            }

            creation.Value!.SetInternal(command.Internal);
            _repository.AddRequisitionDetail(creation.Value!);
        }
        else
        {
            existing.SetInternal(command.Internal);
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var tags = await _repository.ListRequisitionTagsAsync(command.RequestId, cancellationToken).ConfigureAwait(false);
        var detail = await _repository.GetRequisitionDetailAsync(command.RequestId, cancellationToken).ConfigureAwait(false);
        return RequisitionDetailDto.FromDomain(detail, tags.Select(t => t.Label).ToList());
    }
}

/// <summary>Handles <see cref="AddRequisitionTagCommand"/>. Idempotent per label.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class AddRequisitionTagCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<AddRequisitionTagCommand, IReadOnlyList<string>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> HandleAsync(AddRequisitionTagCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = await _repository.GetByIdAsync(new RequestId(command.RequestId), cancellationToken).ConfigureAwait(false);
        if (request is null)
        {
            return Error.NotFound("request.not_found", "No matching requisition was found.");
        }

        var creation = RequisitionTag.Create(command.RequestId, command.Label);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        if (!await _repository.RequisitionTagExistsAsync(command.RequestId, creation.Value!.Label, cancellationToken).ConfigureAwait(false))
        {
            _repository.AddRequisitionTag(creation.Value!);
            await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var tags = await _repository.ListRequisitionTagsAsync(command.RequestId, cancellationToken).ConfigureAwait(false);
        return tags.Select(t => t.Label).ToList();
    }
}

/// <summary>Handles <see cref="RemoveRequisitionTagCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class RemoveRequisitionTagCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<RemoveRequisitionTagCommand, IReadOnlyList<string>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> HandleAsync(RemoveRequisitionTagCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var label = (command.Label ?? string.Empty).Trim().ToLowerInvariant();
        var tag = await _repository.GetRequisitionTagAsync(command.RequestId, label, cancellationToken).ConfigureAwait(false);
        if (tag is not null)
        {
            _repository.RemoveRequisitionTag(tag);
            await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var tags = await _repository.ListRequisitionTagsAsync(command.RequestId, cancellationToken).ConfigureAwait(false);
        return tags.Select(t => t.Label).ToList();
    }
}
