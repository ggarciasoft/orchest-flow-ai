SELECT ne."NodeType", ne."Status", ne."InputJson", LEFT(ne."OutputJson", 300) as output
FROM "NodeExecutions" ne
JOIN "WorkflowExecutions" we ON we."Id" = ne."WorkflowExecutionId"
JOIN "Workflows" w ON w."Id" = we."WorkflowId"
WHERE w."Name" = 'Test Data'
ORDER BY we."StartedAt" DESC, ne."Step" ASC
LIMIT 10;
