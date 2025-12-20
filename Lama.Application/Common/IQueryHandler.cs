using MediatR;

namespace Lama.Application.Common;

// Marker interface for query handlers that return a response
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}
