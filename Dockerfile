# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Skip the Husky git-hook bootstrap inside the container build.
ENV HUSKY=0

# Restore first (leverages Docker layer caching for dependencies).
# Directory.Build.* and .editorconfig carry the shared build/analyzer settings.
COPY Directory.Build.props Directory.Build.targets .editorconfig ./
COPY src/TaskFlow.Api/TaskFlow.Api.csproj src/TaskFlow.Api/
RUN dotnet restore src/TaskFlow.Api/TaskFlow.Api.csproj

# Copy the API sources and publish.
COPY src/ src/
RUN dotnet publish src/TaskFlow.Api/TaskFlow.Api.csproj -c Release -o /app --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Run as the non-root user that the .NET runtime image already provides.
USER app

COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskFlow.Api.dll"]
