using Lama.Domain.LeadManagement.Entities;

namespace Lama.Application.LeadManagement;

/// <summary>
/// Leads need filtered paging, duplicate lookups and the event timeline, which the
/// generic <see cref="Common.IRepository{T}"/> does not cover.
/// </summary>
public interface ILeadRepository
{
    /// <summary>Loads the lead together with its timeline, tracked for updates.</summary>
    Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Lead?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>An open lead from the same Telegram user or with the same email.</summary>
    Task<Lead?> FindOpenDuplicateAsync(long? telegramId, string email, CancellationToken cancellationToken = default);

    Task<PagedResult<Lead>> SearchAsync(LeadFilter filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeadStatsRow>> GetStatsRowsAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Lead lead, CancellationToken cancellationToken = default);

    /// <summary>Persists changes made to leads loaded through this repository.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task DeleteAsync(Lead lead, CancellationToken cancellationToken = default);
}

public record LeadFilter(
    LeadStatus? Status = null,
    LeadTemperature? Temperature = null,
    string? Source = null,
    string? TargetDegree = null,
    string? Country = null,
    string? Search = null,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null,
    int Page = 1,
    int PageSize = 25
);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>The handful of columns the dashboard aggregates over.</summary>
public record LeadStatsRow(
    LeadStatus Status,
    LeadTemperature Temperature,
    string Source,
    string? AnnualBudget,
    string? TargetDegree,
    IReadOnlyList<string> TargetCountries,
    DateTime CreatedAt
);

public class LeadNotFoundException : Exception
{
    public LeadNotFoundException(Guid id) : base($"Lead with ID {id} not found")
    {
    }
}
