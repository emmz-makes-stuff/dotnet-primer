# TODO 4 (solved): two stages. The SDK image builds the app; only the published output goes into a
# chiseled runtime image, which runs it as the non-root `app` user.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The project file first, then restore: this layer comes from the cache until the project changes.
COPY Containers.Solution.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Containers.Solution.dll"]
