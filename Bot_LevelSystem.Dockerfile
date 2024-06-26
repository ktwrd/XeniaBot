FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN dotnet tool install -g dotnet-t4
ENV PATH="/root/.dotnet/tools:${PATH}"
WORKDIR /src
COPY ["./XeniaBot.Core.LevelSystem/XeniaBot.Core.LevelSystem.csproj", "./"]
RUN dotnet restore "XeniaBot.Core.LevelSystem.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./XeniaBot.Core.LevelSystem/XeniaBot.Core.LevelSystem.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "./XeniaBot.Core.LevelSystem/XeniaBot.Core.LevelSystem.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "XeniaBot.Core.LevelSystem.dll"]