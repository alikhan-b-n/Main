using Lama.Domain.ActivityManagement.Entities;

namespace Lama.Integrations.AI.Interfaces;

/// <summary>
/// Repository interface for Activity entity used by AI services.
/// Implementation should be provided by the infrastructure layer.
/// </summary>
public interface IActivityRepository
{
    Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Activity activity, CancellationToken cancellationToken = default);
}