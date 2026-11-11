# TODO 4: One stage, on the SDK image. So the image you run is the image you built with: the whole
#   .NET SDK (nearly 1 GB), a shell, and the app running as root. Split it into two stages:
#   - build on mcr.microsoft.com/dotnet/sdk:10.0, copying the .csproj and restoring before the rest
#     of the source, so a code change doesn't restore again;
#   - then copy only the published app into mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled, and
#     run it as the image's non-root user (USER $APP_UID).

FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app
ENTRYPOINT ["dotnet", "/app/Containers.Starter.dll"]
