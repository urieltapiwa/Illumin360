using Illumin360.Employers.Application.Abstractions;
using Illumin360.Employers.Application.Employers;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Employers.Application;

/// <summary>Registers Application-layer use-case handlers.</summary>
public static class DependencyInjection
{
    /// <summary>Adds CQRS handlers for the Employers context.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddEmployersApplication(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetEmployerQuery, EmployerDto>, GetEmployerQueryHandler>();
        services.AddScoped<ICommandHandler<RegisterEmployerCommand, EmployerDto>, RegisterEmployerCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateEmployerProfileCommand, EmployerDto>, UpdateEmployerProfileCommandHandler>();
        services.AddScoped<IQueryHandler<ListTeamMembersQuery, IReadOnlyList<TeamMemberDto>>, ListTeamMembersQueryHandler>();
        services.AddScoped<ICommandHandler<InviteTeamMemberCommand, TeamMemberDto>, InviteTeamMemberCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeTeamMemberRoleCommand, TeamMemberDto>, ChangeTeamMemberRoleCommandHandler>();
        services.AddScoped<ICommandHandler<RemoveTeamMemberCommand, bool>, RemoveTeamMemberCommandHandler>();
        return services;
    }
}
