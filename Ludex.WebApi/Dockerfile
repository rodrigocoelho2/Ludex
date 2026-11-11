# Ludex.WebApi — accounts, the IGDB catalog, libraries and friends. Not published on any port: only the website reaches it.
# Build context: the solution folder.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY Ludex.Library/Ludex.Library.csproj Ludex.Library/
COPY Ludex.WebApi/Ludex.WebApi.csproj Ludex.WebApi/
RUN dotnet restore Ludex.WebApi/Ludex.WebApi.csproj
COPY Ludex.Library/ Ludex.Library/
COPY Ludex.WebApi/ Ludex.WebApi/
# Files copied from a NAS share can keep owner-only permissions; the app runs as the non-root "app" user.
RUN dotnet publish Ludex.WebApi/Ludex.WebApi.csproj -c Release -o /app --no-restore \
    && chmod -R a+rX /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    TZ=Europe/Lisbon

EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Ludex.WebApi.dll"]
