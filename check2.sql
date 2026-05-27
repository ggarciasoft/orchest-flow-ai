SELECT 
  json_array_elements(def->'edges')->>'source' as src,
  json_array_elements(def->'edges')->>'target' as tgt
FROM (
  SELECT wv."DefinitionJson"::json as def
  FROM "WorkflowVersions" wv
  JOIN "Workflows" w ON w."Id" = wv."WorkflowId"
  WHERE w."Name" = 'Test Data' AND wv."IsActive" = true
) d;
