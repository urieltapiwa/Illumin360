using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A CRM client company.</summary>
/// <param name="Id">Client id.</param>
/// <param name="Name">Company name.</param>
/// <param name="Industry">Industry, if recorded.</param>
/// <param name="City">City, if recorded.</param>
/// <param name="Status">Relationship status (prospect/active/inactive).</param>
/// <param name="Notes">Notes, if any.</param>
/// <param name="ContactCount">Number of contacts on file.</param>
/// <param name="CreatedAt">When the client was created (UTC).</param>
public sealed record ClientDto(Guid Id, string Name, string? Industry, string? City, string Status, string? Notes, int ContactCount, DateTimeOffset CreatedAt)
{
    /// <summary>Projects a domain <see cref="Client"/> into the transport DTO.</summary>
    /// <param name="c">The client.</param>
    /// <param name="contactCount">Number of contacts on file.</param>
    /// <returns>The transport DTO.</returns>
    public static ClientDto FromDomain(Client c, int contactCount)
    {
        ArgumentNullException.ThrowIfNull(c);
        return new ClientDto(c.Id.Value, c.Name, c.Industry, c.City, c.Status.ToWire(), c.Notes, contactCount, c.CreatedAt);
    }
}

/// <summary>A contact person at a client.</summary>
/// <param name="Id">Contact id.</param>
/// <param name="ClientId">Owning client id.</param>
/// <param name="Name">Contact name.</param>
/// <param name="Title">Job title, if recorded.</param>
/// <param name="Email">Email, if recorded.</param>
/// <param name="Phone">Phone, if recorded.</param>
/// <param name="IsPrimary">Whether this is the primary contact.</param>
public sealed record ClientContactDto(Guid Id, Guid ClientId, string Name, string? Title, string? Email, string? Phone, bool IsPrimary)
{
    /// <summary>Projects a domain <see cref="ClientContact"/> into the transport DTO.</summary>
    /// <param name="c">The contact.</param>
    /// <returns>The transport DTO.</returns>
    public static ClientContactDto FromDomain(ClientContact c)
    {
        ArgumentNullException.ThrowIfNull(c);
        return new ClientContactDto(c.Id.Value, c.ClientId.Value, c.Name, c.Title, c.Email, c.Phone, c.IsPrimary);
    }
}

/// <summary>A client with its contacts.</summary>
/// <param name="Client">The client.</param>
/// <param name="Contacts">The client's contacts.</param>
public sealed record ClientDetailDto(ClientDto Client, IReadOnlyList<ClientContactDto> Contacts);

/// <summary>Lists CRM clients, optionally filtered by status.</summary>
/// <param name="Status">Optional status filter (prospect/active/inactive).</param>
public sealed record ListClientsQuery(string? Status = null) : IQuery<IReadOnlyList<ClientDto>>;

/// <summary>Gets a client with its contacts.</summary>
/// <param name="Id">Client id.</param>
public sealed record GetClientQuery(Guid Id) : IQuery<ClientDetailDto>;

/// <summary>Creates a CRM client.</summary>
public sealed record CreateClientCommand(string Name, string? Industry, string? City, string? Notes) : ICommand<ClientDto>;

/// <summary>Changes a client's relationship status.</summary>
public sealed record ChangeClientStatusCommand(Guid Id, string Status) : ICommand<ClientDto>;

/// <summary>Adds a contact to a client.</summary>
public sealed record AddClientContactCommand(Guid ClientId, string Name, string? Title, string? Email, string? Phone, bool IsPrimary) : ICommand<ClientContactDto>;

/// <summary>Removes a contact from a client.</summary>
public sealed record RemoveClientContactCommand(Guid ContactId) : ICommand<bool>;

/// <summary>Handles <see cref="ListClientsQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class ListClientsQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<ListClientsQuery, IReadOnlyList<ClientDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ClientDto>>> HandleAsync(ListClientsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        ClientStatus? filter = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!ClientStatuses.TryParse(query.Status, out var parsed))
            {
                return Error.Validation("client.status_invalid", "Status must be one of prospect, active or inactive.");
            }

            filter = parsed;
        }

        var clients = await _repository.ListClientsAsync(filter, cancellationToken).ConfigureAwait(false);
        var result = new List<ClientDto>(clients.Count);
        foreach (var c in clients)
        {
            var count = await _repository.CountContactsAsync(c.Id, cancellationToken).ConfigureAwait(false);
            result.Add(ClientDto.FromDomain(c, count));
        }

        return result;
    }
}

/// <summary>Handles <see cref="GetClientQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetClientQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetClientQuery, ClientDetailDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<ClientDetailDto>> HandleAsync(GetClientQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var client = await _repository.GetClientAsync(new ClientId(query.Id), cancellationToken).ConfigureAwait(false);
        if (client is null)
        {
            return Error.NotFound("client.not_found", "No matching client was found.");
        }

        var contacts = await _repository.ListContactsForClientAsync(client.Id, cancellationToken).ConfigureAwait(false);
        return new ClientDetailDto(
            ClientDto.FromDomain(client, contacts.Count),
            contacts.Select(ClientContactDto.FromDomain).ToList());
    }
}

/// <summary>Handles <see cref="CreateClientCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class CreateClientCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<CreateClientCommand, ClientDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<ClientDto>> HandleAsync(CreateClientCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var creation = Client.Create(command.Name, command.Industry, command.City, command.Notes, DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        _repository.AddClient(creation.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ClientDto.FromDomain(creation.Value!, 0);
    }
}

/// <summary>Handles <see cref="ChangeClientStatusCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class ChangeClientStatusCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<ChangeClientStatusCommand, ClientDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<ClientDto>> HandleAsync(ChangeClientStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var client = await _repository.GetClientAsync(new ClientId(command.Id), cancellationToken).ConfigureAwait(false);
        if (client is null)
        {
            return Error.NotFound("client.not_found", "No matching client was found.");
        }

        var change = client.ChangeStatus(command.Status);
        if (change.IsFailure)
        {
            return change.Error!;
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var count = await _repository.CountContactsAsync(client.Id, cancellationToken).ConfigureAwait(false);
        return ClientDto.FromDomain(client, count);
    }
}

/// <summary>Handles <see cref="AddClientContactCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class AddClientContactCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<AddClientContactCommand, ClientContactDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<ClientContactDto>> HandleAsync(AddClientContactCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var client = await _repository.GetClientAsync(new ClientId(command.ClientId), cancellationToken).ConfigureAwait(false);
        if (client is null)
        {
            return Error.NotFound("client.not_found", "No matching client was found.");
        }

        var creation = ClientContact.Create(client.Id, command.Name, command.Title, command.Email, command.Phone, command.IsPrimary, DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        _repository.AddClientContact(creation.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ClientContactDto.FromDomain(creation.Value!);
    }
}

/// <summary>Handles <see cref="RemoveClientContactCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class RemoveClientContactCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<RemoveClientContactCommand, bool>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(RemoveClientContactCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var contact = await _repository.GetClientContactAsync(new ClientContactId(command.ContactId), cancellationToken).ConfigureAwait(false);
        if (contact is null)
        {
            return Error.NotFound("contact.not_found", "No matching contact was found.");
        }

        _repository.RemoveClientContact(contact);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
