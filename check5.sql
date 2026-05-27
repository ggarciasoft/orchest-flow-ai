SELECT ne."NodeType", ne."Status", ne."InputJson", LEFT(ne."OutputJson", 200) as output, ne."ErrorMessage", we."StartedAt"
FROM "NodeExecutions" ne
JOIN "WorkflowExecutions" we ON we."Id" = ne."WorkflowExecutionId"
JOIN "Workflows" w ON w."Id" = we."WorkflowId"
WHERE w."Name" = 'Test Data'
ORDER BY we."StartedAt" DESC, ne."Step" ASC
LIMIT 15;
