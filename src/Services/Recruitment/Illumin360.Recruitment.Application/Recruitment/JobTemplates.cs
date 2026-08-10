using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A reusable requisition template.</summary>
/// <param name="Id">Template id.</param>
/// <param name="Name">Template name.</param>
/// <param name="Title">Default role title.</param>
/// <param name="City">Default city.</param>
/// <param name="Positions">Default positions.</param>
/// <param name="SalaryMin">Default lower salary bound.</param>
/// <param name="SalaryMax">Default upper salary bound.</param>
/// <param name="Currency">Default currency.</param>
/// <param name="EmploymentType">Default employment type.</param>
/// <param name="Remote">Default remote flag.</param>
/// <param name="Tags">Default tags.</param>
public sealed record JobTemplateDto(Guid Id, string Name, string Title, string? City, int Positions, int? SalaryMin, int? SalaryMax, string Currency, string EmploymentType, bool Remote, IReadOnlyList<string> Tags)
{
    /// <summary>Projects a domain <see cref="JobTemplate"/> into the transport DTO.</summary>
    /// <param name="t">The template.</param>
    /// <returns>The transport DTO.</returns>
    public static JobTemplateDto FromDomain(JobTemplate t)
    {
        ArgumentNullException.ThrowIfNull(t);
        return new JobTemplateDto(t.Id, t.Name, t.Title, t.City, t.Positions, t.SalaryMin, t.SalaryMax, t.Currency, t.EmploymentType.ToWire(), t.Remote, t.Tags);
    }
}

/// <summary>Lists all job templates, newest first.</summary>
public sealed record GetJobTemplatesQuery : IQuery<IReadOnlyList<JobTemplateDto>>;

/// <summary>Creates a job template.</summary>
/// <param name="Name">Template name.</param>
/// <param name="Title">Default role title.</param>
/// <param name="City">Default city.</param>
/// <param name="Positions">Default positions.</param>
/// <param name="SalaryMin">Default lower salary bound.</param>
/// <param name="SalaryMax">Default upper salary bound.</param>
/// <param name="Currency">Default currency.</param>
/// <param name="EmploymentType">Default employment type.</param>
/// <param name="Remote">Default remote flag.</param>
/// <param name="Tags">Default tags.</param>
public sealed record CreateJobTemplateCommand(string Name, string Title, string? City, int Positions, int? SalaryMin, int? SalaryMax, string? Currency, string? EmploymentType, bool Remote, IReadOnlyList<string>? Tags) : ICommand<JobTemplateDto>;

/// <summary>Deletes a job template.</summary>
/// <param name="Id">Template id.</param>
public sealed record DeleteJobTemplateCommand(Guid Id) : ICommand<bool>;

/// <summary>Creates a new requisition (+ enrichment + tags) from a template.</summary>
/// <param name="TemplateId">Template id.</param>
/// <param name="CompanyId">Hiring company id.</param>
public sealed record UseJobTemplateCommand(Guid TemplateId, Guid CompanyId) : ICommand<RecruitmentRequestDto>;

/// <summary>Handles <see cref="GetJobTemplatesQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetJobTemplatesQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetJobTemplatesQuery, IReadOnlyList<JobTemplateDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<JobTemplateDto>>> HandleAsync(GetJobTemplatesQuery query, CancellationToken cancellationToken)
    {
        var templates = await _repository.ListJobTemplatesAsync(cancellationToken).ConfigureAwait(false);
        return templates.Select(JobTemplateDto.FromDomain).ToList();
    }
}

/// <summary>Handles <see cref="CreateJobTemplateCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class CreateJobTemplateCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<CreateJobTemplateCommand, JobTemplateDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<JobTemplateDto>> HandleAsync(CreateJobTemplateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var creation = JobTemplate.Create(command.Name, command.Title, command.City, command.Positions, command.SalaryMin, command.SalaryMax, command.Currency, command.EmploymentType, command.Remote, command.Tags, DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        if (await _repository.JobTemplateNameExistsAsync(creation.Value!.Name, cancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict("template.name_exists", "A template with that name already exists.");
        }

        _repository.AddJobTemplate(creation.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return JobTemplateDto.FromDomain(creation.Value!);
    }
}

/// <summary>Handles <see cref="DeleteJobTemplateCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class DeleteJobTemplateCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<DeleteJobTemplateCommand, bool>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(DeleteJobTemplateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var template = await _repository.GetJobTemplateAsync(command.Id, cancellationToken).ConfigureAwait(false);
        if (template is null)
        {
            return Error.NotFound("template.not_found", "No matching template was found.");
        }

        _repository.RemoveJobTemplate(template);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}

/// <summary>Handles <see cref="UseJobTemplateCommand"/> — posts a new requisition pre-filled from a template.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class UseJobTemplateCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<UseJobTemplateCommand, RecruitmentRequestDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<RecruitmentRequestDto>> HandleAsync(UseJobTemplateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var template = await _repository.GetJobTemplateAsync(command.TemplateId, cancellationToken).ConfigureAwait(false);
        if (template is null)
        {
            return Error.NotFound("template.not_found", "No matching template was found.");
        }

        var creation = RecruitmentRequest.Post(command.CompanyId, template.Title, template.City ?? string.Empty, template.Positions);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        var request = creation.Value!;
        _repository.Add(request);

        // Carry the template's enrichment onto the new requisition.
        var detail = RequisitionDetail.Create(request.Id.Value, template.SalaryMin, template.SalaryMax, template.Currency, template.EmploymentType.ToWire(), template.Remote, DateTimeOffset.UtcNow);
        if (detail.IsSuccess)
        {
            _repository.AddRequisitionDetail(detail.Value!);
        }

        foreach (var label in template.Tags)
        {
            var tag = RequisitionTag.Create(request.Id.Value, label);
            if (tag.IsSuccess)
            {
                _repository.AddRequisitionTag(tag.Value!);
            }
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return RecruitmentRequestDto.FromDomain(request);
    }
}
