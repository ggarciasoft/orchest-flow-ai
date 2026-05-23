FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["services/OrchestAI.Api/OrchestAI.Api.csproj", "services/OrchestAI.Api/"]
COPY ["packages/OrchestAI.Application/OrchestAI.Application.csproj", "packages/OrchestAI.Application/"]
COPY ["packages/OrchestAI.Infrastructure/OrchestAI.Infrastructure.csproj", "packages/OrchestAI.Infrastructure/"]
COPY ["packages/OrchestAI.Domain/OrchestAI.Domain.csproj", "packages/OrchestAI.Domain/"]
COPY ["packages/OrchestAI.Contracts/OrchestAI.Contracts.csproj", "packages/OrchestAI.Contracts/"]
COPY ["packages/OrchestAI.Engine/OrchestAI.Engine.csproj", "packages/OrchestAI.Engine/"]
COPY ["packages/OrchestAI.SDK/OrchestAI.SDK.csproj", "packages/OrchestAI.SDK/"]
COPY ["packages/OrchestAI.Observability/OrchestAI.Observability.csproj", "packages/OrchestAI.Observability/"]
COPY ["services/OrchestAI.AI/OrchestAI.AI.csproj", "services/OrchestAI.AI/"]
RUN dotnet restore "services/OrchestAI.Api/OrchestAI.Api.csproj"
COPY . .
WORKDIR "/src/services/OrchestAI.Api"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrchestAI.Api.dll"]
