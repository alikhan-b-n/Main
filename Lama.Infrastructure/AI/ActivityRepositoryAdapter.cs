using Lama.Application.Common;
using Lama.Domain.ActivityManagement.Entities;
using Lama.Integrations.AI.Interfaces;

namespace Lama.Infrastructure.AI;

/// <summary>
/// Adapter that implements IActivityRepository from AI integration
/// by delegating to the generic IRepository from Application layer.
/// </summary>
public class ActivityRepositoryAdapter : IActivityRepository
{
    private readonly IRepository<Activity> _repository;

    public ActivityRepositoryAdapter(IRepository<Activity> repository)
    {
        _repository = repository;
    }

    public Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public Task UpdateAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        return _repository.UpdateAsync(activity, cancellationToken);
    }
}