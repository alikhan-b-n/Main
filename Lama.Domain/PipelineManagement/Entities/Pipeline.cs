using Lama.Domain.Common;

namespace Lama.Domain.PipelineManagement.Entities;

public class Pipeline : AggregateRoot
{
    public string Name { get; private set; }
    public PipelineType Type { get; private set; }
    public Guid? TeamId { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<Stage> _stages = new();
    public IReadOnlyCollection<Stage> Stages => _stages.AsReadOnly();

    private Pipeline() { }

    private Pipeline(string name, PipelineType type)
    {
        Name = name;
        Type = type;
        IsActive = true;
    }

    public static Pipeline Create(string name, PipelineType type, Guid? teamId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Pipeline name cannot be empty", nameof(name));

        return new Pipeline(name, type)
        {
            TeamId = teamId
        };
    }

    public void UpdatePipelineInfo(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Pipeline name cannot be empty", nameof(name));

        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddStage(Stage stage)
    {
        if (_stages.Any(s => s.Id == stage.Id))
            throw new InvalidOperationException("Stage already exists in this pipeline");

        _stages.Add(stage);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToTeam(Guid teamId)
    {
        TeamId = teamId;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum PipelineType
{
    Deal,
    Ticket
}
