const { Client } = require('pg');
const client = new Client({ host: 'localhost', database: 'OrchestFlowAI', user: 'OrchestFlowAI', password: 'OrchestFlowAI' });
client.connect().then(async () => {
  const res = await client.query(`
    SELECT wv."Id", wv."VersionNumber", wv."IsActive", wv."DefinitionJson"
    FROM "WorkflowVersions" wv
    JOIN "Workflows" w ON w."Id" = wv."WorkflowId"
    WHERE w."Name" = 'Test Data'
    ORDER BY wv."VersionNumber" DESC LIMIT 1
  `);
  if (!res.rows.length) { console.log('No rows'); await client.end(); return; }
  const row = res.rows[0];
  console.log('Version:', row.VersionNumber, 'Active:', row.IsActive);
  const def = JSON.parse(row.DefinitionJson);
  console.log('\nNode ids and configs:');
  def.nodes.forEach(n => console.log(' ', n.id, '->', n.type, '| config:', JSON.stringify(n.config)));
  console.log('\nEdges:');
  def.edges.forEach(e => console.log(' ', e.source, '->', e.target));
  await client.end();
}).catch(e => { console.error(e.message); process.exit(1); });
