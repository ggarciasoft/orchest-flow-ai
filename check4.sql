-- Get the foreach node config from active version
SELECT 
  node->>'id' as id,
  node->>'type' as type,
  node->'config' as config
FROM (
  SELECT json_array_elements(wv."DefinitionJson"::json->'nodes') as node
  FROM "WorkflowVersions" wv
  JOIN "Workflows" w ON w."Id" = wv."WorkflowId"
  WHERE w."Name" = 'Test Data' AND wv."IsActive" = true
) n
WHERE node->>'type' IN ('logic.foreach', 'integrations.gmail.read');
