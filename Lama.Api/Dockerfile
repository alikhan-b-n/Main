FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Lama.Api/Lama.Api.csproj", "Lama.Api/"]
COPY ["Lama.Application/Lama.Application.csproj", "Lama.Application/"]
COPY ["Lama.Domain/Lama.Domain.csproj", "Lama.Domain/"]
COPY ["Integrations/Lama.Integrations.AI/Lama.Integrations.AI.csproj", "Integrations/Lama.Integrations.AI/"]
COPY ["Lama.Infrastructure/Lama.Infrastructure.csproj", "Lama.Infrastructure/"]
RUN dotnet restore "Lama.Api/Lama.Api.csproj"
COPY . .
WORKDIR "/src/Lama.Api"
RUN dotnet build "./Lama.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Lama.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lama.Api.dll"]
