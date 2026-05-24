export interface DocEntry {
  slug: string;
  title: string;
  category: string;
  filename: string;
}

export const docs: DocEntry[] = [
  { slug: 'setup',           title: 'Getting Started',     category: 'Getting Started', filename: 'SETUP.md' },
  { slug: 'architecture',    title: 'Architecture',         category: 'Core Concepts',   filename: 'ARCHITECTURE.md' },
  { slug: 'workflow-engine', title: 'Workflow Engine',      category: 'Core Concepts',   filename: 'WORKFLOW-ENGINE.md' },
  { slug: 'nodes',           title: 'Nodes Reference',      category: 'Core Concepts',   filename: 'NODES.md' },
  { slug: 'node-sdk',        title: 'Node SDK',             category: 'Developers',      filename: 'NODE-SDK.md' },
  { slug: 'api',             title: 'API Reference',        category: 'Developers',      filename: 'API.md' },
  { slug: 'ai-runtime',      title: 'AI Runtime',           category: 'Developers',      filename: 'AI-RUNTIME.md' },
  { slug: 'database',        title: 'Database',             category: 'Developers',      filename: 'DATABASE.md' },
  { slug: 'frontend',        title: 'Frontend',             category: 'Developers',      filename: 'FRONTEND.md' },
  { slug: 'security',        title: 'Security',             category: 'Operations',      filename: 'SECURITY.md' },
  { slug: 'observability',   title: 'Observability',        category: 'Operations',      filename: 'OBSERVABILITY.md' },
  { slug: 'glossary',        title: 'Glossary',             category: 'Reference',       filename: 'GLOSSARY.md' },
  { slug: 'roadmap',         title: 'Roadmap',              category: 'Reference',       filename: 'ROADMAP.md' },
];

export const categories = [...new Set(docs.map(d => d.category))];
