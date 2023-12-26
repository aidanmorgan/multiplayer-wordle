FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Wordle.Aws.Fargate.PrivateApi/Wordle.Aws.Fargate.PrivateApi.csproj", "Wordle.Aws.Fargate.PrivateApi/"]
COPY ["Wordle.Aws.Common/Wordle.Aws.Common.csproj", "Wordle.Aws.Common/"]
COPY ["Wordle.Persistence.Dynamo/Wordle.Persistence.Dynamo.csproj", "Wordle.Persistence.Dynamo/"]
COPY ["Wordle.Common/Wordle.Common.csproj", "Wordle.Common/"]
COPY ["Wordle.Model/Wordle.Model.csproj", "Wordle.Model/"]
COPY ["Wordle.Persistence/Wordle.Persistence.csproj", "Wordle.Persistence/"]
COPY ["Wordle.Aws.DictionaryImpl/Wordle.Aws.DictionaryImpl.csproj", "Wordle.Aws.DictionaryImpl/"]
COPY ["Wordle.Dictionary/Wordle.Dictionary.csproj", "Wordle.Dictionary/"]
COPY ["Wordle.Aws.EventBridgeImpl/Wordle.Aws.EventBridgeImpl.csproj", "Wordle.Aws.EventBridgeImpl/"]
COPY ["Wordle.Events/Wordle.Events.csproj", "Wordle.Events/"]
COPY ["Wordle.Logger/Wordle.Logger.csproj", "Wordle.Logger/"]
COPY ["Wordle.Clock/Wordle.Clock.csproj", "Wordle.Clock/"]
COPY ["Wordle.CommandHandlers/Wordle.CommandHandlers.csproj", "Wordle.CommandHandlers/"]
COPY ["Wordle.Commands/Wordle.Commands.csproj", "Wordle.Commands/"]
COPY ["Wordle.Queries/Wordle.Queries.csproj", "Wordle.Queries/"]
COPY ["Wordle.QueryHandlers.Dynamo/Wordle.QueryHandlers.Dynamo.csproj", "Wordle.QueryHandlers.Dynamo/"]
RUN dotnet restore "Wordle.Aws.Fargate.PrivateApi/Wordle.Aws.Fargate.PrivateApi.csproj"
COPY . .
WORKDIR "/src/Wordle.Aws.Fargate.PrivateApi"
RUN dotnet build "Wordle.Aws.Fargate.PrivateApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Wordle.Aws.Fargate.PrivateApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Wordle.Aws.Fargate.PrivateApi.dll"]
