﻿FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Wordle.Api.Realtime/Wordle.Api.Realtime.csproj", "Wordle.Api.Realtime/"]
COPY ["Wordle.Apps.Common/Wordle.Apps.Common.csproj", "Wordle.Apps.Common/"]
COPY ["Wordle.Aws.DictionaryImpl/Wordle.Aws.DictionaryImpl.csproj", "Wordle.Aws.DictionaryImpl/"]
COPY ["Wordle.Dictionary/Wordle.Dictionary.csproj", "Wordle.Dictionary/"]
COPY ["Wordle.Model/Wordle.Model.csproj", "Wordle.Model/"]
COPY ["Wordle.Aws.EventBridge/Wordle.Aws.EventBridge.csproj", "Wordle.Aws.EventBridge/"]
COPY ["Wordle.Aws.Common/Wordle.Aws.Common.csproj", "Wordle.Aws.Common/"]
COPY ["Wordle.Events/Wordle.Events.csproj", "Wordle.Events/"]
COPY ["Wordle.Logger/Wordle.Logger.csproj", "Wordle.Logger/"]
COPY ["Wordle.Clock/Wordle.Clock.csproj", "Wordle.Clock/"]
COPY ["Wordle.CommandHandlers/Wordle.CommandHandlers.csproj", "Wordle.CommandHandlers/"]
COPY ["Wordle.Commands/Wordle.Commands.csproj", "Wordle.Commands/"]
COPY ["Wordle.Common/Wordle.Common.csproj", "Wordle.Common/"]
COPY ["Wordle.Persistence.Dynamo/Wordle.Persistence.Dynamo.csproj", "Wordle.Persistence.Dynamo/"]
COPY ["Wordle.Persistence/Wordle.Persistence.csproj", "Wordle.Persistence/"]
COPY ["Wordle.Queries/Wordle.Queries.csproj", "Wordle.Queries/"]
COPY ["Wordle.QueryHandlers.Dynamo/Wordle.QueryHandlers.Dynamo.csproj", "Wordle.QueryHandlers.Dynamo/"]
COPY ["Wordle.Render/Wordle.Render.csproj", "Wordle.Render/"]
COPY ["Wordle.Kafka.Consumer/Wordle.Kafka.Consumer.csproj", "Wordle.Kafka.Consumer/"]
COPY ["Wordle.Kafka.Common/Wordle.Kafka.Common.csproj", "Wordle.Kafka.Common/"]
COPY ["Wordle.Kafka.Publisher/Wordle.Kafka.Publisher.csproj", "Wordle.Kafka.Publisher/"]
RUN dotnet restore "Wordle.Api.Realtime/Wordle.Api.Realtime.csproj"
COPY . .
WORKDIR "/src/Wordle.Api.Realtime"
RUN dotnet build "Wordle.Api.Realtime.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Wordle.Api.Realtime.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Wordle.Api.Realtime.dll"]