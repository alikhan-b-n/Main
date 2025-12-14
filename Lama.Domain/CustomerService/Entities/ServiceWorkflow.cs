using Lama.Domain.Common;

namespace Lama.Domain.CustomerService.Entities;

public class ServiceWorkflow : AggregateRoot
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public CaseType TriggerCaseType { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<WorkflowStep> _steps = new();
    public IReadOnlyCollection<WorkflowStep> Steps => _steps.AsReadOnly();

    private ServiceWorkflow() { }

    private ServiceWorkflow(string name, CaseType triggerCaseType)
    {
        Name = name;
        TriggerCaseType = triggerCaseType;
        IsActive = true;
    }

    public static ServiceWorkflow Create(string name, CaseType triggerCaseType, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workflow name cannot be empty", nameof(name));

        return new ServiceWorkflow(name, triggerCaseType)
        {
            Description = description
        };
    }

    public void UpdateWorkflowInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workflow name cannot be empty", nameof(name));

        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddStep(WorkflowStep step)
    {
        _steps.Add(step);
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

    public bool ShouldTrigger(SupportCase supportCase)
    {
        return IsActive && supportCase.Type == TriggerCaseType;
    }
}

public class WorkflowStep
{
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowAction Action { get; set; }
    public int DelayMinutes { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public enum WorkflowAction
{
    SendEmail,
    AssignToUser,
    UpdatePriority,
    AddNote,
    CreateTask,
    EscalateToManager,
    RequestFeedback
}
