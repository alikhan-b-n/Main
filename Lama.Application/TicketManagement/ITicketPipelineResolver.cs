namespace Lama.Application.TicketManagement;

public interface ITicketPipelineResolver
{
    Task<(Guid PipelineId, Guid StageId)> ResolveDefaultAsync(CancellationToken cancellationToken = default);
}
