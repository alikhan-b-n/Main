using Lama.Domain.Common;

namespace Lama.Domain.PipelineManagement.Entities;

public class Stage : Entity
{
    public string Name { get; private set; }
    public int Order { get; private set; }
    public bool IsClosed { get; private set; }
    public Guid PipelineId { get; private set; }
    public int? Probability { get; private set; } // For deal stages only

    private Stage() { }

    private Stage(string name, int order, Guid pipelineId)
    {
        Name = name;
        Order = order;
        PipelineId = pipelineId;
        IsClosed = false;
    }

    public static Stage Create(string name, int order, Guid pipelineId, int? probability = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Stage name cannot be empty", nameof(name));
        if (pipelineId == Guid.Empty)
            throw new ArgumentException("Pipeline ID cannot be empty", nameof(pipelineId));
        if (probability.HasValue && (probability.Value < 0 || probability.Value > 100))
            throw new ArgumentException("Probability must be between 0 and 100", nameof(probability));

        return new Stage(name, order, pipelineId)
        {
            Probability = probability
        };
    }

    public void UpdateStageInfo(string name, int order, int? probability)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Stage name cannot be empty", nameof(name));
        if (probability.HasValue && (probability.Value < 0 || probability.Value > 100))
            throw new ArgumentException("Probability must be between 0 and 100", nameof(probability));

        Name = name;
        Order = order;
        Probability = probability;
    }

    public void MarkAsClosed()
    {
        IsClosed = true;
    }

    public void MarkAsOpen()
    {
        IsClosed = false;
    }
}
