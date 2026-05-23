using Microsoft.Extensions.Hosting;
using OrchestAI.AI.Extensions;
using OrchestAI.Application.Extensions;
using OrchestAI.Engine.Extensions;
using OrchestAI.Infrastructure.Extensions;
using OrchestAI.Nodes;
using OrchestAI.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddOrchestAIApplication();
builder.Services.AddOrchestAIInfrastructure(builder.Configuration);
builder.Services.AddOrchestAIAI(builder.Configuration);
builder.Services.AddOrchestAIEngine();
builder.Services.AddOrchestAINodes();
builder.Services.AddHostedService<ExecutionWorker>();
builder.Services.AddHostedService<ResumeWorker>();
var host = builder.Build();
host.Run();