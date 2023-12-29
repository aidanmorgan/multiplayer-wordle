FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Wordle.Apps.BoardGenerator/Wordle.Apps.BoardGenerator.csproj", "Wordle.Apps.BoardGenerator/"]
COPY ["Wordle.Clock/Wordle.Clock.csproj", "Wordle.Clock/"]
COPY ["Wordle.Logger/Wordle.Logger.csproj", "Wordle.Logger/"]
COPY ["Wordle.Persistence/Wordle.Persistence.csproj", "Wordle.Persistence/"]
COPY ["Wordle.Model/Wordle.Model.csproj", "Wordle.Model/"]
COPY ["Wordle.Apps.Common/Wordle.Apps.Common.csproj", "Wordle.Apps.Common/"]
COPY ["Wordle.Aws.DictionaryImpl/Wordle.Aws.DictionaryImpl.csproj", "Wordle.Aws.DictionaryImpl/"]
COPY ["Wordle.Dictionary/Wordle.Dictionary.csproj", "Wordle.Dictionary/"]
COPY ["Wordle.Aws.EventBridgeImpl/Wordle.Aws.EventBridgeImpl.csproj", "Wordle.Aws.EventBridgeImpl/"]
COPY ["Wordle.Events/Wordle.Events.csproj", "Wordle.Events/"]
COPY ["Wordle.CommandHandlers/Wordle.CommandHandlers.csproj", "Wordle.CommandHandlers/"]
COPY ["Wordle.Commands/Wordle.Commands.csproj", "Wordle.Commands/"]
COPY ["Wordle.Common/Wordle.Common.csproj", "Wordle.Common/"]
COPY ["Wordle.Persistence.Dynamo/Wordle.Persistence.Dynamo.csproj", "Wordle.Persistence.Dynamo/"]
COPY ["Wordle.Queries/Wordle.Queries.csproj", "Wordle.Queries/"]
COPY ["Wordle.QueryHandlers.Dynamo/Wordle.QueryHandlers.Dynamo.csproj", "Wordle.QueryHandlers.Dynamo/"]
RUN dotnet restore "Wordle.Apps.BoardGenerator/Wordle.Apps.BoardGenerator.csproj"
COPY . .
WORKDIR "/src/Wordle.Apps.BoardGenerator"
RUN dotnet build "Wordle.Apps.BoardGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Wordle.Apps.BoardGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Wordle.Apps.BoardGenerator.dll"]
