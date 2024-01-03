FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Wordle.Apps.GameEventProcessor/Wordle.Apps.GameEventProcessor.csproj", "Wordle.Apps.GameEventProcessor/"]
COPY ["Wordle.Apps.Common/Wordle.Apps.Common.csproj", "Wordle.Apps.Common/"]
COPY ["Wordle.Dictionary.DynamoDb/Wordle.Dictionary.DynamoDb.csproj", "Wordle.Dictionary.DynamoDb/"]
COPY ["Wordle.Dictionary/Wordle.Dictionary.csproj", "Wordle.Dictionary/"]
COPY ["Wordle.Model/Wordle.Model.csproj", "Wordle.Model/"]
COPY ["Wordle.Aws.EventBridge/Wordle.Aws.EventBridge.csproj", "Wordle.Aws.EventBridge/"]
COPY ["Wordle.Common/Wordle.Common.csproj", "Wordle.Common/"]
COPY ["Wordle.Events/Wordle.Events.csproj", "Wordle.Events/"]
COPY ["Wordle.Clock/Wordle.Clock.csproj", "Wordle.Clock/"]
COPY ["Wordle.CommandHandlers/Wordle.CommandHandlers.csproj", "Wordle.CommandHandlers/"]
COPY ["Wordle.Commands/Wordle.Commands.csproj", "Wordle.Commands/"]
COPY ["Wordle.Persistence/Wordle.Persistence.csproj", "Wordle.Persistence/"]
COPY ["Wordle.Queries/Wordle.Queries.csproj", "Wordle.Queries/"]
COPY ["Wordle.Persistence.DynamoDb/Wordle.Persistence.DynamoDb.csproj", "Wordle.Persistence.DynamoDb/"]
COPY ["Wordle.QueryHandlers.DynamoDb/Wordle.QueryHandlers.DynamoDb.csproj", "Wordle.QueryHandlers.DynamoDb/"]
COPY ["Wordle.Render/Wordle.Render.csproj", "Wordle.Render/"]
COPY ["Wordle.Kafka.Consumer/Wordle.Kafka.Consumer.csproj", "Wordle.Kafka.Consumer/"]
COPY ["Wordle.Kafka.Common/Wordle.Kafka.Common.csproj", "Wordle.Kafka.Common/"]
COPY ["Wordle.Kafka.Publisher/Wordle.Kafka.Publisher.csproj", "Wordle.Kafka.Publisher/"]
COPY ["Wordle.Redis.Common/Wordle.Redis.Common.csproj", "Wordle.Redis.Common/"]
COPY ["Wordle.Redis.Publisher/Wordle.Redis.Publisher.csproj", "Wordle.Redis.Publisher/"]
COPY ["Wordle.Redis.Consumer/Wordle.Redis.Consumer.csproj", "Wordle.Redis.Consumer/"]
COPY ["Wordle.EfCore/Wordle.EfCore.csproj", "Wordle.EfCore/"]
COPY ["Wordle.Persistence.EfCore/Wordle.Persistence.EfCore.csproj", "Wordle.Persistence.EfCore/"]
COPY ["Wordle.QueryHandlers.EfCore/Wordle.QueryHandlers.EfCore.csproj", "Wordle.QueryHandlers.EfCore/"]
COPY ["Wordle.Dictionary.EfCore/Wordle.Dictionary.EfCore.csproj", "Wordle.Dictionary.EfCore/"]
RUN dotnet restore "Wordle.Apps.GameEventProcessor/Wordle.Apps.GameEventProcessor.csproj"
COPY . .
WORKDIR "/src/Wordle.Apps.GameEventProcessor"
RUN dotnet build "Wordle.Apps.GameEventProcessor.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Wordle.Apps.GameEventProcessor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Wordle.Apps.GameEventProcessor.dll"]
