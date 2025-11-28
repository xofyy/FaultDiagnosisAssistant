FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FaultDiagnosis.DataPipeline/FaultDiagnosis.DataPipeline.csproj", "FaultDiagnosis.DataPipeline/"]
COPY ["FaultDiagnosis.Core/FaultDiagnosis.Core.csproj", "FaultDiagnosis.Core/"]
COPY ["FaultDiagnosis.Infrastructure/FaultDiagnosis.Infrastructure.csproj", "FaultDiagnosis.Infrastructure/"]
RUN dotnet restore "./FaultDiagnosis.DataPipeline/FaultDiagnosis.DataPipeline.csproj"
COPY . .
WORKDIR "/src/FaultDiagnosis.DataPipeline"
RUN dotnet build "./FaultDiagnosis.DataPipeline.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FaultDiagnosis.DataPipeline.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FaultDiagnosis.DataPipeline.dll"]
