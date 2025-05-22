FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

RUN echo "deb http://deb.debian.org/debian bookworm contrib non-free" > /etc/apt/sources.list.d/contrib.list
RUN apt-get update && apt-get install -y ttf-mscorefonts-installer fontconfig
RUN mkdir -p /usr/share/fonts/truetype/msttcorefonts/
RUN wget -q https://github.com/martinring/clide/blob/master/doc/fonts/segoeui.ttf?raw=true -O /usr/share/fonts/truetype/msttcorefonts/segoeui.ttf # regular
RUN wget -q https://github.com/martinring/clide/blob/master/doc/fonts/segoeuib.ttf?raw=true -O /usr/share/fonts/truetype/msttcorefonts/segoeuib.ttf # bold
RUN wget -q https://github.com/martinring/clide/blob/master/doc/fonts/segoeuib.ttf?raw=true -O /usr/share/fonts/truetype/msttcorefonts/segoeuii.ttf # italic
RUN wget -q https://github.com/martinring/clide/blob/master/doc/fonts/segoeuiz.ttf?raw=true -O /usr/share/fonts/truetype/msttcorefonts/segoeuiz.ttf # bold italic
RUN wget -q https://github.com/martinring/clide/blob/master/doc/fonts/segoeuil.ttf?raw=true -O /usr/share/fonts/truetype/msttcorefonts/segoeuil.ttf # light
# RUN wget -q https://github.com/martinring/clide/blob/master/doc/fonts/seguili.ttf?raw=true -O /usr/share/fonts/truetype/msttcorefonts/seguili.ttf # light italic
RUN wget -q https://github.com/martinring/clide/blob/master/doc/fonts/segoeuisl.ttf?raw=true -O /usr/share/fonts/truetype/msttcorefonts/segoeuisl.ttf # semilight
RUN wget -q https://github.com/martinring/clide/blob/master/doc/fonts/seguisli.ttf?raw=true -O /usr/share/fonts/truetype/msttcorefonts/seguisli.ttf # semilight italic
RUN wget -q https://github.com/martinring/clide/blob/master/doc/fonts/seguisb.ttf?raw=true -O /usr/share/fonts/truetype/msttcorefonts/seguisb.ttf # semibold
RUN wget -q https://github.com/martinring/clide/blob/master/doc/fonts/seguisbi.ttf?raw=true -O /usr/share/fonts/truetype/msttcorefonts/seguisbi.ttf # semibold italic
RUN fc-cache -f /usr/share/fonts/truetype/msttcorefonts/

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["circlebot.MissAnalyser/circlebot.MissAnalyser.csproj", "circlebot.MissAnalyser/"]
RUN dotnet restore "circlebot.MissAnalyser/circlebot.MissAnalyser.csproj"
COPY . .
WORKDIR "/src/circlebot.MissAnalyser"
RUN dotnet build "circlebot.MissAnalyser.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "circlebot.MissAnalyser.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "circlebot.MissAnalyser.dll"]
