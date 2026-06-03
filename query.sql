SELECT "DefinitionJson" FROM "WorkflowVersions" WHERE "Id" = (SELECT "WorkflowVersionId" FROM "WorkflowExecutions" WHERE "Id" = '56560b5a-d7d0-4276-b3dc-10955c4ac438');
