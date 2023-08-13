FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["XeniaBot.WebPanel/XeniaBot.WebPanel.csproj", "XeniaBot.WebPanel/"]
RUN dotnet restore "XeniaBot.WebPanel/XeniaBot.WebPanel.csproj"
COPY . .
WORKDIR "/src/XeniaBot.WebPanel"
RUN dotnet build "XeniaBot.WebPanel.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "XeniaBot.WebPanel.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "XeniaBot.WebPanel.dll"]
