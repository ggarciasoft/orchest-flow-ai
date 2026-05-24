FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["services/OrchestFlowAI.Api/OrchestFlowAI.Api.csproj", "services/OrchestFlowAI.Api/"]
COPY ["packages/OrchestFlowAI.Application/OrchestFlowAI.Application.csproj", "packages/OrchestFlowAI.Application/"]
COPY ["packages/OrchestFlowAI.Infrastructure/OrchestFlowAI.Infrastructure.csproj", "packages/OrchestFlowAI.Infrastructure/"]
COPY ["packages/OrchestFlowAI.Domain/OrchestFlowAI.Domain.csproj", "packages/OrchestFlowAI.Domain/"]
COPY ["packages/OrchestFlowAI.Contracts/OrchestFlowAI.Contracts.csproj", "packages/OrchestFlowAI.Contracts/"]
COPY ["packages/OrchestFlowAI.Engine/OrchestFlowAI.Engine.csproj", "packages/OrchestFlowAI.Engine/"]
COPY ["packages/OrchestFlowAI.SDK/OrchestFlowAI.SDK.csproj", "packages/OrchestFlowAI.SDK/"]
COPY ["packages/OrchestFlowAI.Observability/OrchestFlowAI.Observability.csproj", "packages/OrchestFlowAI.Observability/"]
COPY ["services/OrchestFlowAI.AI/OrchestFlowAI.AI.csproj", "services/OrchestFlowAI.AI/"]
RUN dotnet restore "services/OrchestFlowAI.Api/OrchestFlowAI.Api.csproj"
COPY . .
WORKDIR "/src/services/OrchestFlowAI.Api"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrchestFlowAI.Api.dll"]
