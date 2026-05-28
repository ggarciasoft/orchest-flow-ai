using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OrchestFlowAI.AI.Extensions;
using OrchestFlowAI.Api.Hubs;
using OrchestFlowAI.Api.Notifications;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Notifications;
using OrchestFlowAI.Application.Extensions;
using OrchestFlowAI.Engine.Extensions;
using OrchestFlowAI.Infrastructure.Extensions;
using OrchestFlowAI.Infrastructure.Notifications;
using OrchestFlowAI.Nodes;
using OrchestFlowAI.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrchestFlowAIApplication();
builder.Services.AddOrchestFlowAIInfrastructure(builder.Configuration);
builder.Services.AddOrchestFlowAIAI(builder.Configuration);
builder.Services.AddOrchestFlowAIEngine();
builder.Services.AddOrchestFlowAINodes();
//builder.Services.AddOrchestFlowAIObservability();

builder.Services.AddSignalR();

// Override IExecutionNotifier with SignalR when CONNECTION_STRING is set
// (Infrastructure registers StubExecutionNotifier as the default; last registration wins)
var _notifierCs = builder.Configuration.GetConnectionString("Default")
    ?? Environment.GetEnvironmentVariable("CONNECTION_STRING");
if (!string.IsNullOrWhiteSpace(_notifierCs))
    builder.Services.AddScoped<IExecutionNotifier, SignalRExecutionNotifier>();

builder.Services.AddScoped<OrchestFlowAI.Api.Services.WorkflowGenerationService>();
builder.Services.AddSingleton<OrchestFlowAI.Api.Services.FormNodeRegistrar>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OrchestFlowAI.Api.Services.FormNodeRegistrar>());
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger / OpenAPI — available in all environments for ease of development
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OrchestFlowAI API",
        Version = "v1",
        Description = "REST API for the OrchestFlowAI workflow automation platform.\n\n" +
                      "Use `POST /api/auth/login` to obtain a JWT token, then click **Authorize** and enter `Bearer <token>`.",
        Contact = new OpenApiContact { Name = "OrchestFlowAI", Url = new Uri("https://github.com/ggarciasoft/OrchestFlowAI") }
    });

    // Enable JWT Bearer auth button in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: **Bearer eyJ...**"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var jwtKey = builder.Configuration["Auth:JwtSigningKey"] ?? "dev-signing-key-change-in-production-32chars";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = builder.Configuration["Auth:JwtIssuer"] ?? "OrchestFlowAI",
            ValidateAudience = true, ValidAudience = builder.Configuration["Auth:JwtAudience"] ?? "OrchestFlowAI-web",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// RBAC authorization policies — enforces role hierarchy across all controller endpoints
builder.Services.AddAuthorization(options =>
{
    // ViewerOrAbove: any authenticated user with a recognized role may read resources
    options.AddPolicy("ViewerOrAbove", policy =>
        policy.RequireRole("Viewer", "Editor", "Admin", "Approver"));

    // EditorOrAbove: Editor and Admin roles may create/update/delete resources
    options.AddPolicy("EditorOrAbove", policy =>
        policy.RequireRole("Editor", "Admin"));

    // AdminOnly: only Admin role may manage users, tenants, and system settings
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});
// Allow any origin with credentials for SignalR WebSocket connections
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
app.UseCorrelationId();
app.UseCors();

// Swagger UI — always enabled (not just Development) so the API is explorable locally and in staging
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "OrchestFlowAI API v1");
    c.RoutePrefix = "swagger"; // accessible at /swagger
    c.DocumentTitle = "OrchestFlowAI API";
    c.DisplayRequestDuration();
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ExecutionHub>("/hubs/execution");

// Auto-apply EF Core migrations and seed dev data on startup when PostgreSQL is configured
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var config = services.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("Default")
        ?? Environment.GetEnvironmentVariable("CONNECTION_STRING");

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        const int maxRetries = 5;
        const int delayMs = 3000;
        Exception? lastEx = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var db = services.GetRequiredService<OrchestFlowAI.Infrastructure.Persistence.OrchestFlowAIDbContext>();
                // Apply any pending migrations — creates tables on first run
                await db.Database.MigrateAsync();
                app.Logger.LogInformation("Database migrations applied successfully.");
                lastEx = null;
                break;
            }
            catch (Exception ex)
            {
                lastEx = ex;
                app.Logger.LogWarning(ex, "Migration attempt {Attempt}/{Max} failed. Retrying in {Delay}ms...", attempt, maxRetries, delayMs);
                if (attempt < maxRetries)
                    await Task.Delay(delayMs);
            }
        }

        // If all retries exhausted, fail hard — running without tables is worse than not starting
        if (lastEx != null)
        {
            app.Logger.LogCritical(lastEx, "Database migration failed after {Max} attempts. Shutting down.", maxRetries);
            throw new InvalidOperationException("Database migration failed. Cannot start application.", lastEx);
        }
    }
    else
    {
        app.Logger.LogInformation("No CONNECTION_STRING configured — using in-memory repositories.");
    }
}

app.Run();
