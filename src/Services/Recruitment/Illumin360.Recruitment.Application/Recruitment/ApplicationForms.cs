using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A configurable application-form / screening question on a requisition.</summary>
/// <param name="Id">Question id.</param>
/// <param name="Label">Question text.</param>
/// <param name="Kind">Input type (text/textarea/boolean/number/select).</param>
/// <param name="Options">Options (select only).</param>
/// <param name="Required">Whether an answer is required.</param>
/// <param name="SortOrder">Ordering within the form.</param>
public sealed record FormQuestionDto(Guid Id, string Label, string Kind, IReadOnlyList<string> Options, bool Required, int SortOrder)
{
    /// <summary>Projects a domain <see cref="ApplicationFormQuestion"/> into the transport DTO.</summary>
    /// <param name="q">The question.</param>
    /// <returns>The transport DTO.</returns>
    public static FormQuestionDto FromDomain(ApplicationFormQuestion q)
    {
        ArgumentNullException.ThrowIfNull(q);
        return new FormQuestionDto(q.Id, q.Label, q.Kind.ToWire(), q.Options, q.Required, q.SortOrder);
    }
}

/// <summary>A candidate's answer to a form question.</summary>
/// <param name="QuestionId">The question answered.</param>
/// <param name="Label">Snapshot of the question label.</param>
/// <param name="Value">The answer value.</param>
public sealed record AnswerDto(Guid QuestionId, string Label, string Value)
{
    /// <summary>Projects a domain <see cref="ApplicationAnswer"/> into the transport DTO.</summary>
    /// <param name="a">The answer.</param>
    /// <returns>The transport DTO.</returns>
    public static AnswerDto FromDomain(ApplicationAnswer a)
    {
        ArgumentNullException.ThrowIfNull(a);
        return new AnswerDto(a.QuestionId, a.QuestionLabel, a.Value);
    }
}

/// <summary>A single answer being submitted.</summary>
/// <param name="QuestionId">The question id.</param>
/// <param name="Value">The answer value.</param>
public sealed record AnswerInput(Guid QuestionId, string? Value);

/// <summary>Lists a requisition's application-form questions (ascending sort order).</summary>
/// <param name="RequestId">The requisition id.</param>
public sealed record GetFormQuestionsQuery(Guid RequestId) : IQuery<IReadOnlyList<FormQuestionDto>>;

/// <summary>Adds a question to a requisition's application form (appended to the end).</summary>
/// <param name="RequestId">The requisition id.</param>
/// <param name="Label">Question text.</param>
/// <param name="Kind">Input-type name.</param>
/// <param name="Options">Options (select only).</param>
/// <param name="Required">Whether required.</param>
public sealed record AddFormQuestionCommand(Guid RequestId, string Label, string? Kind, IReadOnlyList<string>? Options, bool Required) : ICommand<FormQuestionDto>;

/// <summary>Removes a form question.</summary>
/// <param name="QuestionId">The question id.</param>
public sealed record RemoveFormQuestionCommand(Guid QuestionId) : ICommand<bool>;

/// <summary>Lists a candidate's answers for an application.</summary>
/// <param name="ApplicationId">The application id.</param>
public sealed record GetApplicationAnswersQuery(Guid ApplicationId) : IQuery<IReadOnlyList<AnswerDto>>;

/// <summary>Submits (replaces) a candidate's application-form answers.</summary>
/// <param name="ApplicationId">The application id.</param>
/// <param name="Answers">The answers.</param>
public sealed record SubmitApplicationAnswersCommand(Guid ApplicationId, IReadOnlyList<AnswerInput> Answers) : ICommand<int>;

/// <summary>Handles <see cref="GetFormQuestionsQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetFormQuestionsQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetFormQuestionsQuery, IReadOnlyList<FormQuestionDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FormQuestionDto>>> HandleAsync(GetFormQuestionsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var questions = await _repository.ListFormQuestionsAsync(query.RequestId, cancellationToken).ConfigureAwait(false);
        return questions.Select(FormQuestionDto.FromDomain).ToList();
    }
}

/// <summary>Handles <see cref="AddFormQuestionCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class AddFormQuestionCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<AddFormQuestionCommand, FormQuestionDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<FormQuestionDto>> HandleAsync(AddFormQuestionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await _repository.ListFormQuestionsAsync(command.RequestId, cancellationToken).ConfigureAwait(false);
        var nextOrder = existing.Count == 0 ? 0 : existing.Max(q => q.SortOrder) + 1;

        var creation = ApplicationFormQuestion.Create(
            command.RequestId,
            command.Label,
            command.Kind,
            command.Options,
            command.Required,
            nextOrder,
            DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        _repository.AddFormQuestion(creation.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return FormQuestionDto.FromDomain(creation.Value!);
    }
}

/// <summary>Handles <see cref="RemoveFormQuestionCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class RemoveFormQuestionCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<RemoveFormQuestionCommand, bool>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(RemoveFormQuestionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var question = await _repository.GetFormQuestionAsync(command.QuestionId, cancellationToken).ConfigureAwait(false);
        if (question is null)
        {
            return Error.NotFound("form.question_not_found", "No matching question was found.");
        }

        _repository.RemoveFormQuestion(question);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}

/// <summary>Handles <see cref="GetApplicationAnswersQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetApplicationAnswersQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetApplicationAnswersQuery, IReadOnlyList<AnswerDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AnswerDto>>> HandleAsync(GetApplicationAnswersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var answers = await _repository.ListApplicationAnswersAsync(query.ApplicationId, cancellationToken).ConfigureAwait(false);
        return answers.Select(AnswerDto.FromDomain).ToList();
    }
}

/// <summary>Handles <see cref="SubmitApplicationAnswersCommand"/> — replaces the application's stored answers.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class SubmitApplicationAnswersCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<SubmitApplicationAnswersCommand, int>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<int>> HandleAsync(SubmitApplicationAnswersCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ApplicationId == Guid.Empty)
        {
            return Error.Validation("answer.application_required", "An application id is required.");
        }

        // Replace any prior answers for this application (idempotent re-submit).
        var existing = await _repository.ListApplicationAnswersTrackedAsync(command.ApplicationId, cancellationToken).ConfigureAwait(false);
        foreach (var prior in existing)
        {
            _repository.RemoveApplicationAnswer(prior);
        }

        var now = DateTimeOffset.UtcNow;
        var saved = 0;
        foreach (var input in command.Answers ?? [])
        {
            if (string.IsNullOrWhiteSpace(input.Value))
            {
                continue;
            }

            var question = await _repository.GetFormQuestionAsync(input.QuestionId, cancellationToken).ConfigureAwait(false);
            if (question is null)
            {
                continue; // ignore answers to unknown/removed questions
            }

            var creation = ApplicationAnswer.Create(command.ApplicationId, input.QuestionId, question.Label, input.Value, now);
            if (creation.IsFailure)
            {
                return creation.Error!;
            }

            _repository.AddApplicationAnswer(creation.Value!);
            saved++;
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return saved;
    }
}
