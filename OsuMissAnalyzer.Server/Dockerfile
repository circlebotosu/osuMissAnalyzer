FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

RUN sed -i'.bak' 's/$/ contrib/' /etc/apt/sources.list
RUN apt-get update; apt-get install -y ttf-mscorefonts-installer fontconfig

USER $APP_UID
WORKDIR /app
EXPOSE 1234

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OsuMissAnalyzer.Server/OsuMissAnalyzer.Server.csproj", "OsuMissAnalyzer.Server/"]
RUN dotnet restore "OsuMissAnalyzer.Server/OsuMissAnalyzer.Server.csproj"
COPY . .
WORKDIR "/src/OsuMissAnalyzer.Server"
RUN dotnet build "OsuMissAnalyzer.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "OsuMissAnalyzer.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OsuMissAnalyzer.Server.dll"]
