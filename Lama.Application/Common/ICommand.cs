using MediatR;

namespace Lama.Application.Common;

// Marker interface for commands that return a response
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

// Marker interface for commands that don't return a response
public interface ICommand : IRequest
{
}
