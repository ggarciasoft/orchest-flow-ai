SELECT ne."OutputJson"
FROM "NodeExecutions" ne
JOIN "WorkflowExecutions" we ON we."Id" = ne."WorkflowExecutionId"
JOIN "Workflows" w ON w."Id" = we."WorkflowId"
WHERE w."Name" = 'Test Data' AND ne."NodeType" = 'ai.extract'
ORDER BY we."StartedAt" DESC
LIMIT 1;
