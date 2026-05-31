export interface DocEntry {
  slug: string;
  title: string;
  category: string;
  filename: string;
  description?: string;
}

export const docs: DocEntry[] = [
  // Getting Started
  { slug: 'setup',                title: 'Getting Started',              category: 'Getting Started', filename: 'SETUP.md',               description: 'Install, run locally, and deploy OrchestFlowAI.' },

  // How To
  { slug: 'howto-designer',       title: 'How To: Visual Designer',      category: 'How To',          filename: 'HOWTO-DESIGNER.md',      description: 'Build workflows with the drag-and-drop canvas.' },
  { slug: 'howto-form-builder',   title: 'How To: Form Builder',         category: 'How To',          filename: 'HOWTO-FORM-BUILDER.md',  description: 'Create forms that pause workflows for human input.' },
  { slug: 'howto-ai-builder',     title: 'How To: AI Builder',           category: 'How To',          filename: 'HOWTO-AI-BUILDER.md',    description: 'Use the AI assistant to generate and modify workflows.' },
  { slug: 'howto-external-data',  title: 'How To: External Data Intake', category: 'How To',          filename: 'HOWTO-EXTERNAL-DATA.md', description: 'Validate and ingest external data into workflows.' },
  { slug: 'howto-config',         title: 'How To: Workflow Configuration', category: 'How To',        filename: 'HOWTO-CONFIG.md',        description: 'Store and read persistent key-value configuration.' },
  { slug: 'howto-team',           title: 'How To: Manage Your Team',     category: 'How To',          filename: 'HOWTO-TEAM.md',          description: 'Invite members, assign roles, and configure email.' },

  // Core Concepts
  { slug: 'architecture',         title: 'Architecture',                 category: 'Core Concepts',   filename: 'ARCHITECTURE.md',        description: 'System overview, domain model, and design decisions.' },
  { slug: 'workflow-engine',      title: 'Workflow Engine',              category: 'Core Concepts',   filename: 'WORKFLOW-ENGINE.md',     description: 'Execution runtime, state machine, retries, and loops.' },
  { slug: 'nodes',                title: 'Nodes Reference',              category: 'Core Concepts',   filename: 'NODES.md',               description: 'All built-in node types and their configuration.' },

  // Developers
  { slug: 'node-sdk',             title: 'Node SDK',                     category: 'Developers',      filename: 'NODE-SDK.md',            description: 'Build custom nodes with the INode interface.' },
  { slug: 'api',                  title: 'API Reference',                category: 'Developers',      filename: 'API.md',                 description: 'Full REST API documentation for all endpoints.' },
  { slug: 'ai-runtime',           title: 'AI Runtime',                   category: 'Developers',      filename: 'AI-RUNTIME.md',          description: 'LLM abstraction, providers, and structured outputs.' },
  { slug: 'database',             title: 'Database',                     category: 'Developers',      filename: 'DATABASE.md',            description: 'Schema, EF Core conventions, and migrations.' },
  { slug: 'frontend',             title: 'Frontend',                     category: 'Developers',      filename: 'FRONTEND.md',            description: 'Next.js app structure, RBAC, screens, and API client.' },

  // Operations
  { slug: 'security',             title: 'Security',                     category: 'Operations',      filename: 'SECURITY.md',            description: 'AuthN/AuthZ, invitation flow, secrets, and hardening.' },
  { slug: 'observability',        title: 'Observability',                category: 'Operations',      filename: 'OBSERVABILITY.md',       description: 'Logging, tracing, and metrics.' },

  // Reference
  { slug: 'glossary',             title: 'Glossary',                     category: 'Reference',       filename: 'GLOSSARY.md',            description: 'Key terms and definitions.' },
  { slug: 'roadmap',              title: 'Roadmap',                      category: 'Reference',       filename: 'ROADMAP.md',             description: 'Phases, milestones, and future plans.' },
];

export const categories = [...new Set(docs.map(d => d.category))];
