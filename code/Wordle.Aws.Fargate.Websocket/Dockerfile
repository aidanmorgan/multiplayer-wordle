FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Wordle.Aws.Fargate.Websocket/Wordle.Aws.Fargate.Websocket.csproj", "Wordle.Aws.Fargate.Websocket/"]
RUN dotnet restore "Wordle.Aws.Fargate.Websocket/Wordle.Aws.Fargate.Websocket.csproj"
COPY . .
WORKDIR "/src/Wordle.Aws.Fargate.Websocket"
RUN dotnet build "Wordle.Aws.Fargate.Websocket.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Wordle.Aws.Fargate.Websocket.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Wordle.Aws.Fargate.Websocket.dll"]
