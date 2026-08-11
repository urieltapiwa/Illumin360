using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>An interview kit summary.</summary>
/// <param name="Id">Kit id.</param>
/// <param name="Name">Kit name.</param>
/// <param name="QuestionCount">Number of questions.</param>
/// <param name="CreatedAt">Creation timestamp.</param>
public sealed record InterviewKitDto(Guid Id, string Name, int QuestionCount, DateTimeOffset CreatedAt);

/// <summary>An interview kit question.</summary>
/// <param name="Id">Question id.</param>
/// <param name="QuestionOrder">Order within the kit.</param>
/// <param name="Text">Question text.</param>
/// <param name="Skill">Skill assessed (optional).</param>
public sealed record InterviewKitQuestionDto(Guid Id, int QuestionOrder, string Text, string? Skill);

/// <summary>A kit with its questions.</summary>
/// <param name="Kit">The kit summary.</param>
/// <param name="Questions">Its questions, in order.</param>
public sealed record InterviewKitDetailDto(InterviewKitDto Kit, IReadOnlyList<InterviewKitQuestionDto> Questions);

/// <summary>Creates an interview kit.</summary>
/// <param name="Name">Kit name.</param>
public sealed record CreateInterviewKitCommand(string Name) : ICommand<InterviewKitDto>;

/// <summary>Adds a question to a kit.</summary>
/// <param name="KitId">The kit.</param>
/// <param name="Text">Question text.</param>
/// <param name="Skill">Skill assessed (optional).</param>
public sealed record AddKitQuestionCommand(Guid KitId, string Text, string? Skill) : ICommand<InterviewKitQuestionDto>;

/// <summary>Lists all interview kits.</summary>
public sealed record ListInterviewKitsQuery : IQuery<IReadOnlyList<InterviewKitDto>>;

/// <summary>Gets a kit with its questions.</summary>
/// <param name="KitId">The kit.</param>
public sealed record GetInterviewKitQuery(Guid KitId) : IQuery<InterviewKitDetailDto>;

/// <summary>Handles <see cref="CreateInterviewKitCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class CreateInterviewKitCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<CreateInterviewKitCommand, InterviewKitDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<InterviewKitDto>> HandleAsync(CreateInterviewKitCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var kit = InterviewKit.Create(command.Name, DateTimeOffset.UtcNow);
        if (kit.IsFailure)
        {
            return kit.Error!;
        }

        _repository.AddInterviewKit(kit.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new InterviewKitDto(kit.Value!.Id, kit.Value!.Name, 0, kit.Value!.CreatedAt);
    }
}

/// <summary>Handles <see cref="AddKitQuestionCommand"/> — appends a question at the next order.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class AddKitQuestionCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<AddKitQuestionCommand, InterviewKitQuestionDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<InterviewKitQuestionDto>> HandleAsync(AddKitQuestionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var kit = await _repository.GetInterviewKitAsync(command.KitId, cancellationToken).ConfigureAwait(false);
        if (kit is null)
        {
            return Error.NotFound("kit.not_found", "Interview kit not found.");
        }

        var existing = await _repository.ListKitQuestionsAsync(command.KitId, cancellationToken).ConfigureAwait(false);
        var nextOrder = existing.Count == 0 ? 1 : existing.Max(q => q.QuestionOrder) + 1;

        var question = InterviewKitQuestion.Create(command.KitId, nextOrder, command.Text, command.Skill, DateTimeOffset.UtcNow);
        if (question.IsFailure)
        {
            return question.Error!;
        }

        _repository.AddKitQuestion(question.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new InterviewKitQuestionDto(question.Value!.Id, question.Value!.QuestionOrder, question.Value!.Text, question.Value!.Skill);
    }
}

/// <summary>Handles <see cref="ListInterviewKitsQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class ListInterviewKitsQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<ListInterviewKitsQuery, IReadOnlyList<InterviewKitDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<InterviewKitDto>>> HandleAsync(ListInterviewKitsQuery query, CancellationToken cancellationToken)
    {
        var kits = await _repository.ListInterviewKitsAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<InterviewKitDto>(kits.Count);
        foreach (var k in kits)
        {
            var questions = await _repository.ListKitQuestionsAsync(k.Id, cancellationToken).ConfigureAwait(false);
            result.Add(new InterviewKitDto(k.Id, k.Name, questions.Count, k.CreatedAt));
        }

        return result;
    }
}

/// <summary>Handles <see cref="GetInterviewKitQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetInterviewKitQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetInterviewKitQuery, InterviewKitDetailDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<InterviewKitDetailDto>> HandleAsync(GetInterviewKitQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var kit = await _repository.GetInterviewKitAsync(query.KitId, cancellationToken).ConfigureAwait(false);
        if (kit is null)
        {
            return Error.NotFound("kit.not_found", "Interview kit not found.");
        }

        var questions = await _repository.ListKitQuestionsAsync(query.KitId, cancellationToken).ConfigureAwait(false);
        return new InterviewKitDetailDto(
            new InterviewKitDto(kit.Id, kit.Name, questions.Count, kit.CreatedAt),
            questions.OrderBy(q => q.QuestionOrder).Select(q => new InterviewKitQuestionDto(q.Id, q.QuestionOrder, q.Text, q.Skill)).ToList());
    }
}
