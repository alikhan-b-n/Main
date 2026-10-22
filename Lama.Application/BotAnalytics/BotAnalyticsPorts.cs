using Lama.Domain.BotAnalytics;

namespace Lama.Application.BotAnalytics;

public interface ISurveyEventRepository
{
    /// <summary>Stores the events whose ids are new and returns how many that was.</summary>
    Task<int> AddNewAsync(IReadOnlyCollection<SurveyEvent> events, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SurveyEvent>> GetBetweenAsync(
        DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    /// <summary>When the bot started sending events at all; null before the first one.</summary>
    Task<DateTime?> GetFirstEventAtAsync(CancellationToken cancellationToken = default);
}

public interface IBotReminderSettingsRepository
{
    /// <summary>The single row, created with the defaults the first time it is asked for.</summary>
    Task<BotReminderSettings> GetAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(BotReminderSettings settings, CancellationToken cancellationToken = default);
}
