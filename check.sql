SELECT wv."VersionNumber", wv."IsActive", substring(wv."DefinitionJson", 1, 2000) as def
FROM "WorkflowVersions" wv
JOIN "Workflows" w ON w."Id" = wv."WorkflowId"
WHERE w."Name" = 'Test Data'
ORDER BY wv."VersionNumber" DESC LIMIT 1;
