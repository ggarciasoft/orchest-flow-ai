using OrchestFlowAI.Domain.Enums;

namespace OrchestFlowAI.Domain.Entities;

/// <summary>
/// Represents the execution of a workflow instance within the OrchestFlowAI system.
/// Maintains the execution state, inputs, outputs, and lifecycle events such as start, pause, and complete.
/// </summary>
public sealed class WorkflowExecution
{
    /// <summary>
    /// Gets the unique identifier for this workflow execution.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the tenant under which this workflow execution runs.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the identifier of the workflow being executed.
    /// </summary>
    public Guid WorkflowId { get; private set; }

    /// <summary>
    /// Gets the version identifier of the workflow being executed.
    /// </summary>
    public Guid WorkflowVersionId { get; private set; }

    /// <summary>
    /// Gets the current execution status of the workflow.
    /// </summary>
    public ExecutionStatus Status { get; private set; }

    /// <summary>
    /// Gets the date and time when the workflow execution started.
    /// </summary>
    public DateTime StartedAt { get; private set; }

    /// <summary>
    /// Gets the date and time when the workflow execution was completed, if applicable.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Gets the identifier of the user or entity that triggered the execution, if applicable.
    /// </summary>
    public Guid? TriggeredBy { get; private set; }

    /// <summary>
    /// Gets the input parameters for the workflow in serialized JSON format.
    /// </summary>
    public string InputJson { get; private set; } = "{}";

    /// <summary>
    /// Gets the output data of the workflow execution in serialized JSON format, if applicable.
    /// </summary>
    public string? OutputJson { get; private set; }

    /// <summary>
    /// Gets the error message describing the failure reason, if applicable.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets a unique identifier that correlates this execution with external systems.
    /// </summary>
    public string CorrelationId { get; private set; } = default!;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowExecution"/> class.
    /// </summary>
    private WorkflowExecution() { }

    /// <summary>
    /// Factory method to initialize a new workflow execution instance.
    /// </summary>
    /// <param name="tenantId">The tenant under which the workflow execution runs.</param>
    /// <param name="workflowId">The ID of the workflow being executed.</param>
    /// <param name="workflowVersionId">The version ID of the workflow.</param>
    /// <param name="triggeredBy">The ID of the user or entity that triggered the execution (optional).</param>
    /// <param name="inputJson">The input parameters for the workflow in serialized JSON format.</param>
    /// <param name="correlationId">A unique identifier correlating this execution with external systems.</param>
    /// <returns>A newly created instance of WorkflowExecution.</returns>
    public static WorkflowExecution Create(Guid tenantId, Guid workflowId, Guid workflowVersionId, Guid? triggeredBy, string inputJson, string correlationId)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WorkflowId = workflowId,
            WorkflowVersionId = workflowVersionId,
            Status = ExecutionStatus.Queued,
            StartedAt = DateTime.UtcNow,
            TriggeredBy = triggeredBy,
            InputJson = inputJson,
            CorrelationId = correlationId
        };

    /// <summary>
    /// Marks the workflow execution as running.
    /// </summary>
    public void Start() => Status = ExecutionStatus.Running;

    /// <summary>
    /// Marks the workflow execution as completed and records the output.
    /// </summary>
    /// <param name="outputJson">The output data of the completed workflow in serialized JSON format.</param>
    public void Complete(string? outputJson)
    {
        Status = ExecutionStatus.Completed;
        OutputJson = outputJson;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the workflow execution as failed and records the error message.
    /// </summary>
    /// <param name="errorMessage">Details about the failure cause.</param>
    public void Fail(string errorMessage)
    {
        Status = ExecutionStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Pauses the workflow execution.
    /// </summary>
    public void Pause() => Status = ExecutionStatus.Paused;

    /// <summary>
    /// Resumes the workflow execution.
    /// </summary>
    public void Resume() => Status = ExecutionStatus.Running;

    /// <summary>
    /// Cancels the workflow execution and records the cancellation time.
    /// </summary>
    public void Cancel()
    {
        Status = ExecutionStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }
}