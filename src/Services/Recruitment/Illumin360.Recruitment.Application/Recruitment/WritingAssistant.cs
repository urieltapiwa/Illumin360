using Illumin360.Ai;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A generated piece of assistant text plus whether a hosted model or the local fallback produced it.</summary>
/// <param name="Text">The generated text.</param>
/// <param name="Source">"hosted" (LLM) or "template" (deterministic fallback).</param>
public sealed record AssistantResultDto(string Text, string Source);

/// <summary>Generates a job description from a title + city + required skills.</summary>
/// <param name="Title">Role title.</param>
/// <param name="City">Role city.</param>
/// <param name="Skills">Required skills.</param>
public sealed record GenerateJobDescriptionCommand(string Title, string? City, IReadOnlyList<string>? Skills) : ICommand<AssistantResultDto>;

/// <summary>Summarises a block of text (e.g. a CV or notes).</summary>
/// <param name="Text">The text to summarise.</param>
public sealed record SummarizeTextCommand(string Text) : ICommand<AssistantResultDto>;

/// <summary>Drafts a candidate message for a context + intent.</summary>
/// <param name="Context">Who/what the message is about.</param>
/// <param name="Intent">The message intent.</param>
public sealed record DraftMessageCommand(string? Context, string? Intent) : ICommand<AssistantResultDto>;

/// <summary>Shared helper: use the hosted model when enabled, else the deterministic local fallback.</summary>
internal static class Assistant
{
    /// <summary>Runs the hosted model when enabled (falling back to the template on empty output), else the template.</summary>
    /// <param name="client">The completion client.</param>
    /// <param name="system">System instruction.</param>
    /// <param name="prompt">User prompt.</param>
    /// <param name="fallback">The deterministic local fallback.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The assistant result with its source.</returns>
    public static async Task<AssistantResultDto> RunAsync(ITextCompletionClient client, string system, string prompt, Func<string> fallback, CancellationToken ct)
    {
        if (!client.Enabled)
        {
            return new AssistantResultDto(fallback(), "template");
        }

        var text = await client.CompleteAsync(system, prompt, ct).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(text)
            ? new AssistantResultDto(fallback(), "template")
            : new AssistantResultDto(text.Trim(), "hosted");
    }
}

/// <summary>Handles <see cref="GenerateJobDescriptionCommand"/>.</summary>
/// <param name="client">The text-completion client (disabled by default → local template).</param>
public sealed class GenerateJobDescriptionCommandHandler(ITextCompletionClient client)
    : ICommandHandler<GenerateJobDescriptionCommand, AssistantResultDto>
{
    private readonly ITextCompletionClient _client = client;

    /// <inheritdoc />
    public async Task<Result<AssistantResultDto>> HandleAsync(GenerateJobDescriptionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return Error.Validation("assistant.title_required", "A role title is required.");
        }

        var skills = command.Skills ?? [];
        var prompt = $"Write a concise, inclusive job description for a \"{command.Title}\" role" +
            (string.IsNullOrWhiteSpace(command.City) ? string.Empty : $" based in {command.City}") +
            (skills.Count > 0 ? $". Required skills: {string.Join(", ", skills)}." : ".") +
            " Use short sections (overview, responsibilities, requirements, what we offer).";

        return await Assistant.RunAsync(
            _client,
            "You are a helpful recruiting copywriter. Write clear, inclusive, bias-free job descriptions.",
            prompt,
            () => AiTemplates.JobDescription(command.Title, command.City, skills),
            cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handles <see cref="SummarizeTextCommand"/>.</summary>
/// <param name="client">The text-completion client.</param>
public sealed class SummarizeTextCommandHandler(ITextCompletionClient client)
    : ICommandHandler<SummarizeTextCommand, AssistantResultDto>
{
    private readonly ITextCompletionClient _client = client;

    /// <inheritdoc />
    public async Task<Result<AssistantResultDto>> HandleAsync(SummarizeTextCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.Text))
        {
            return Error.Validation("assistant.text_required", "Text to summarise is required.");
        }

        return await Assistant.RunAsync(
            _client,
            "You summarise candidate information concisely and factually, without inventing details.",
            $"Summarise this in two or three sentences:\n\n{command.Text}",
            () => AiTemplates.Summarize(command.Text, 3),
            cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handles <see cref="DraftMessageCommand"/>.</summary>
/// <param name="client">The text-completion client.</param>
public sealed class DraftMessageCommandHandler(ITextCompletionClient client)
    : ICommandHandler<DraftMessageCommand, AssistantResultDto>
{
    private readonly ITextCompletionClient _client = client;

    /// <inheritdoc />
    public async Task<Result<AssistantResultDto>> HandleAsync(DraftMessageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var prompt = $"Draft a short, warm, professional message. Context: {command.Context ?? "a candidate"}. " +
            $"Intent: {command.Intent ?? "follow up"}. Keep it under 120 words.";

        return await Assistant.RunAsync(
            _client,
            "You are a recruiter writing brief, friendly, professional candidate messages.",
            prompt,
            () => AiTemplates.DraftMessage(command.Context, command.Intent),
            cancellationToken).ConfigureAwait(false);
    }
}
