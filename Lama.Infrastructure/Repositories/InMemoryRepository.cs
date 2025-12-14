using Lama.Application.Common;
using Lama.Domain.Common;

namespace Lama.Infrastructure.Repositories;

public class InMemoryRepository<T> : IRepository<T> where T : AggregateRoot
{
    private readonly Dictionary<Guid, T> _storage = new();
    private readonly object _lock = new();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _storage.TryGetValue(id, out var entity);
            return Task.FromResult(entity);
        }
    }

    public Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_storage.Values.AsEnumerable());
        }
    }

    public Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_storage.ContainsKey(entity.Id))
                throw new InvalidOperationException($"Entity with ID {entity.Id} already exists");

            _storage[entity.Id] = entity;
            return Task.CompletedTask;
        }
    }

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_storage.ContainsKey(entity.Id))
                throw new InvalidOperationException($"Entity with ID {entity.Id} not found");

            _storage[entity.Id] = entity;
            return Task.CompletedTask;
        }
    }

    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _storage.Remove(entity.Id);
            return Task.CompletedTask;
        }
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_storage.ContainsKey(id));
        }
    }
}
