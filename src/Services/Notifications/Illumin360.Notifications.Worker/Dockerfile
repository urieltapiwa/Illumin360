# syntax=docker/dockerfile:1
# Multi-stage build for the Notifications worker. Non-root, chiseled runtime (charter Part 2 & DoD Part 19).
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
# .editorconfig MUST be copied or StyleCop/analyzers run with defaults and fail the strict build.
COPY Directory.Build.props Directory.Packages.props global.json .editorconfig ./
COPY src/ ./src/
RUN dotnet restore "src/Services/Notifications/Illumin360.Notifications.Worker/Illumin360.Notifications.Worker.csproj"
RUN dotnet publish "src/Services/Notifications/Illumin360.Notifications.Worker/Illumin360.Notifications.Worker.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

# Chiseled, non-root runtime
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Illumin360.Notifications.Worker.dll"]
