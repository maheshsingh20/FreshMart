using MediatR;
using SharedKernel.Domain;

namespace SharedKernel.CQRS;

/// <summary>
/// CQRS (Command Query Responsibility Segregation) contracts.
///
/// This file defines the core interfaces that enforce the CQRS pattern
/// across all microservices. Every write operation goes through ICommand,
/// every read operation goes through IQuery.
///
/// Built on top of MediatR — handlers are auto-discovered via DI and
/// dispatched by IMediator.Send() in controllers.
/// </summary>

// Commands - write side
// All commands return a Result (success/failure) — never throw exceptions for business failures.
public interface ICommand : IRequest<Result> { }
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }

// Queries - read side
// Queries return data directly — no Result wrapper needed since reads don't fail with business errors.
public interface IQuery<TResponse> : IRequest<TResponse> { }

// Handlers
// Each command/query has exactly one handler. MediatR resolves the correct handler at runtime.
public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand { }

public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse> { }
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse> { }

