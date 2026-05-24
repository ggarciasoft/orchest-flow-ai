using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrchestAI.AI.Extensions;
using OrchestAI.Application.Extensions;
using OrchestAI.Engine.Extensions;
using OrchestAI.Infrastructure.Extensions;
using OrchestAI.Nodes;
using OrchestAI.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrchestAIApplication();
builder.Services.AddOrchestAIInfrastructure(builder.Configuration);
builder.Services.AddOrchestAIAI(builder.Configuration);
builder.Services.AddOrchestAIEngine();
builder.Services.AddOrchestAINodes();
//builder.Services.AddOrchestAIObservability();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger / OpenAPI — available in all environments for ease of development
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OrchestAI API",
        Version = "v1",
        Description = "REST API for the OrchestAI workflow automation platform.\n\n" +
                      "Use `POST /api/auth/login` to obtain a JWT token, then click **Authorize** and enter `Bearer <token>`.",
        Contact = new OpenApiContact { Name = "OrchestAI", Url = new Uri("https://github.com/ggarciasoft/orchestai") }
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
            ValidateIssuer = true, ValidIssuer = builder.Configuration["Auth:JwtIssuer"] ?? "orchestai",
            ValidateAudience = true, ValidAudience = builder.Configuration["Auth:JwtAudience"] ?? "orchestai-web",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCorrelationId();
app.UseCors();

// Swagger UI — always enabled (not just Development) so the API is explorable locally and in staging
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "OrchestAI API v1");
    c.RoutePrefix = "swagger"; // accessible at /swagger
    c.DocumentTitle = "OrchestAI API";
    c.DisplayRequestDuration();
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Auto-apply EF Core migrations and seed dev data on startup when PostgreSQL is configured
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var config = services.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("Default")
        ?? Environment.GetEnvironmentVariable("CONNECTION_STRING");

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        try
        {
            var db = services.GetRequiredService<OrchestAI.Infrastructure.Persistence.OrchestAIDbContext>();
            // Apply any pending migrations — creates tables on first run
            await db.Database.MigrateAsync();
            app.Logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Failed to apply database migrations.");
        }
    }
    else
    {
        app.Logger.LogInformation("No CONNECTION_STRING configured — using in-memory repositories.");
    }
}

app.Run();
