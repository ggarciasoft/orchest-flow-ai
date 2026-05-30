-- ⚠️ DESTRUCTIVE — clears all workflow/form/execution data, keeps tenants/users/settings

-- Execution data (child → parent order)
TRUNCATE TABLE
    "CorrelationTokens",
    "FormSubmissions",
    "ApprovalComments",
    "ApprovalRequests",
    "AIUsageLogs",
    "NodeExecutions",
    "ExecutionQueue",
    "WorkflowExecutions"
CASCADE;

-- Workflow definitions
TRUNCATE TABLE
    "WorkflowVersions",
    "Workflows"
CASCADE;

-- Forms
TRUNCATE TABLE
    "FormVersions",
    "Forms"
CASCADE;
