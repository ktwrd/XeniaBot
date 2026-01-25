#==== BASE
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

RUN apt-get update -y \
    && apt-get install -y --no-install-recommends \
    gnupg \
    apt-transport-https \
    ca-certificates \
    adduser \
    && rm -rf /var/lib/apt/lists/*

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN /usr/sbin/adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
EXPOSE 8080
EXPOSE 8081

RUN apt-get install \
    fonts-recommended \
    fontconfig

#==== BUILD
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . ./
RUN dotnet restore "XeniaBot.Core/XeniaBot.Core.csproj"
WORKDIR "/src/XeniaBot.Core"
RUN dotnet build "XeniaBot.Core.csproj" -c $BUILD_CONFIGURATION -o /app/build

#==== PUBLISH
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "XeniaBot.Core.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

#==== FINAL
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .