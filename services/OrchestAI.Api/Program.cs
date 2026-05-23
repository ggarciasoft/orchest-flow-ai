using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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
builder.Services.AddOrchestAIObservability();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "OrchestAI API", Version = "v1" }));

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
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();