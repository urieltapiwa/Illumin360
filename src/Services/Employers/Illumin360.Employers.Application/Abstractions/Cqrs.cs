using Illumin360.SharedKernel;

namespace Illumin360.Employers.Application.Abstractions;

/// <summary>Marker for a query (read) request returning <typeparamref name="TResult"/>.</summary>
/// <typeparam name="TResult">The query result type.</typeparam>
public interface IQuery<TResult>;

/// <summary>Handles a query of type <typeparamref name="TQuery"/> (hand-rolled CQRS — charter Part 5).</summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResult">The result payload type.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>Executes the query.</summary>
    /// <param name="query">The query instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> wrapping the payload.</returns>
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

/// <summary>Marker for a command (write) request returning <typeparamref name="TResult"/>.</summary>
/// <typeparam name="TResult">The command result type.</typeparam>
public interface ICommand<TResult>;

/// <summary>Handles a command of type <typeparamref name="TCommand"/> (hand-rolled CQRS — charter Part 5).</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The result payload type.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    /// <summary>Executes the command.</summary>
    /// <param name="command">The command instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> wrapping the payload.</returns>
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
