FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Wordle.Api.Realtime/Wordle.Api.Realtime.csproj", "Wordle.Api.Realtime/"]
COPY ["Wordle.Apps.Common/Wordle.Apps.Common.csproj", "Wordle.Apps.Common/"]
COPY ["Wordle.Clock/Wordle.Clock.csproj", "Wordle.Clock/"]
COPY ["Wordle.CommandHandlers/Wordle.CommandHandlers.csproj", "Wordle.CommandHandlers/"]
COPY ["Wordle.Commands/Wordle.Commands.csproj", "Wordle.Commands/"]
COPY ["Wordle.Model/Wordle.Model.csproj", "Wordle.Model/"]
COPY ["Wordle.Common/Wordle.Common.csproj", "Wordle.Common/"]
COPY ["Wordle.Events/Wordle.Events.csproj", "Wordle.Events/"]
COPY ["Wordle.Dictionary/Wordle.Dictionary.csproj", "Wordle.Dictionary/"]
COPY ["Wordle.Persistence/Wordle.Persistence.csproj", "Wordle.Persistence/"]
COPY ["Wordle.Queries/Wordle.Queries.csproj", "Wordle.Queries/"]
COPY ["Wordle.Render/Wordle.Render.csproj", "Wordle.Render/"]
COPY ["Wordle.ActiveMq.Publisher/Wordle.ActiveMq.Publisher.csproj", "Wordle.ActiveMq.Publisher/"]
COPY ["Wordle.ActiveMq.Common/Wordle.ActiveMq.Common.csproj", "Wordle.ActiveMq.Common/"]
COPY ["Wordle.EfCore/Wordle.EfCore.csproj", "Wordle.EfCore/"]
COPY ["Wordle.Persistence.EfCore/Wordle.Persistence.EfCore.csproj", "Wordle.Persistence.EfCore/"]
COPY ["Wordle.QueryHandlers.EfCore/Wordle.QueryHandlers.EfCore.csproj", "Wordle.QueryHandlers.EfCore/"]
COPY ["Wordle.Dictionary.EfCore/Wordle.Dictionary.EfCore.csproj", "Wordle.Dictionary.EfCore/"]
COPY ["Wordle.ActiveMq.Consumer/Wordle.ActiveMq.Consumer.csproj", "Wordle.ActiveMq.Consumer/"]
COPY ["Wordle.Api.Common/Wordle.Api.Common.csproj", "Wordle.Api.Common/"]
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
