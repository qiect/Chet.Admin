FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Chet.Admin.Api/Chet.Admin.Api.csproj", "Chet.Admin.Api/"]
COPY ["Chet.Admin.Core/Chet.Admin.Contracts/Chet.Admin.Contracts.csproj", "Chet.Admin.Core/Chet.Admin.Contracts/"]
COPY ["Chet.Admin.Core/Chet.Admin.Domain/Chet.Admin.Domain.csproj", "Chet.Admin.Core/Chet.Admin.Domain/"]
COPY ["Chet.Admin.Core/Chet.Admin.Shared/Chet.Admin.Shared.csproj", "Chet.Admin.Core/Chet.Admin.Shared/"]
COPY ["Chet.Admin.Infrastructure/Chet.Admin.Caching/Chet.Admin.Caching.csproj", "Chet.Admin.Infrastructure/Chet.Admin.Caching/"]
COPY ["Chet.Admin.Infrastructure/Chet.Admin.Configuration/Chet.Admin.Configuration.csproj", "Chet.Admin.Infrastructure/Chet.Admin.Configuration/"]
COPY ["Chet.Admin.Infrastructure/Chet.Admin.Data/Chet.Admin.Data.csproj", "Chet.Admin.Infrastructure/Chet.Admin.Data/"]
COPY ["Chet.Admin.Infrastructure/Chet.Admin.Logging/Chet.Admin.Logging.csproj", "Chet.Admin.Infrastructure/Chet.Admin.Logging/"]
COPY ["Chet.Admin.Application/Chet.Admin.DTOs/Chet.Admin.DTOs.csproj", "Chet.Admin.Application/Chet.Admin.DTOs/"]
COPY ["Chet.Admin.Application/Chet.Admin.Mapping/Chet.Admin.Mapping.csproj", "Chet.Admin.Application/Chet.Admin.Mapping/"]
COPY ["Chet.Admin.Application/Chet.Admin.Services/Chet.Admin.Services.csproj", "Chet.Admin.Application/Chet.Admin.Services/"]
RUN dotnet restore "Chet.Admin.Api/Chet.Admin.Api.csproj"
COPY . .
WORKDIR "/src/Chet.Admin.Api"
RUN dotnet build "Chet.Admin.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Chet.Admin.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/api/v1/health || exit 1

ENTRYPOINT ["dotnet", "Chet.Admin.Api.dll"]
