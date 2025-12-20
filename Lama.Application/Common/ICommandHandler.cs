using MediatR;

namespace Lama.Application.Common;

// Marker interface for command handlers that return a response
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}

// Marker interface for command handlers that don't return a response
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand
{
}
