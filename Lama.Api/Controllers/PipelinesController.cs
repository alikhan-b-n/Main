using Lama.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/pipelines")]
public class PipelinesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public PipelinesController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var pipelines = await _dbContext.Pipelines.ToListAsync();
        var stages = await _dbContext.Stages.ToListAsync();

        var result = pipelines.Select(p => new PipelineDto(
            p.Id,
            p.Name,
            p.Type.ToString(),
            p.IsActive,
            stages
                .Where(s => s.PipelineId == p.Id)
                .OrderBy(s => s.Order)
                .Select(s => new StageDto(s.Id, s.Name, s.Order, s.IsClosed))
                .ToList()
        ));

        return Ok(result);
    }
}

public record PipelineDto(Guid Id, string Name, string Type, bool IsActive, List<StageDto> Stages);
public record StageDto(Guid Id, string Name, int Order, bool IsClosed);
