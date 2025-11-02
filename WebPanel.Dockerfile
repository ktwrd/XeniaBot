FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base-pkg

RUN apt-get update && apt-get install -y fonts-recommended fontconfig fonts-noto-cjk fonts-noto-cjk-extra fonts-liberation

FROM base-pkg AS base
WORKDIR /app

EXPOSE 80 8080

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# --- compile project ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN dotnet tool install -g dotnet-t4
ENV PATH="/root/.dotnet/tools:${PATH}"
WORKDIR /src
COPY ["./XeniaBot.WebPanel/XeniaBot.WebPanel.csproj", "./"]
RUN dotnet restore "XeniaBot.WebPanel.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./XeniaBot.WebPanel/XeniaBot.WebPanel.csproj" -c Release -o /app/build

# --- publish project ---
FROM build AS publish
RUN dotnet publish "./XeniaBot.WebPanel/XeniaBot.WebPanel.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "XeniaBot.WebPanel.dll"]