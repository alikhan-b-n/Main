using MediatR;

namespace Lama.Application.Common;

// Marker interface for queries that return a response
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
