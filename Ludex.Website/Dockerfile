# Ludex.Website — the app people open.
# Build context: the solution folder.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY Ludex.Library/Ludex.Library.csproj Ludex.Library/
COPY Ludex.Sdk/Ludex.Sdk.csproj Ludex.Sdk/
COPY Ludex.Website/Ludex.Website.csproj Ludex.Website/
RUN dotnet restore Ludex.Website/Ludex.Website.csproj
COPY Ludex.Library/ Ludex.Library/
COPY Ludex.Sdk/ Ludex.Sdk/
COPY Ludex.Website/ Ludex.Website/
# Files that reach the build context through a Windows/SMB share or a git checkout can be readable by their owner
# only, and publish copies appsettings.json and wwwroot with those modes: make everything readable for the app user.
RUN dotnet publish Ludex.Website/Ludex.Website.csproj -c Release -o /app --no-restore \
    && chmod -R a+rX /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# /keys keeps the login cookie keys, so "keep me signed in" survives updates. Owned by the non-root "app" user so a
# named volume inherits that ownership.
RUN mkdir -p /keys && chown "$APP_UID" /keys
VOLUME /keys

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    Website__KeysDirectory=/keys \
    TZ=Europe/Lisbon

EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Ludex.Website.dll"]
