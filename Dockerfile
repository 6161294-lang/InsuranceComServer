# Insurance Claims API (ASP.NET Core). The React client lives in its own repository and is deployed separately.
# Build from the repository root:  docker build -t insurance-claims-api .

# ---------- 1. Build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Project files first, so the restore layer is cached until a package changes.
COPY src/InsuranceClaims.Core/InsuranceClaims.Core.csproj src/InsuranceClaims.Core/
COPY src/InsuranceClaims.Data/InsuranceClaims.Data.csproj src/InsuranceClaims.Data/
COPY src/InsuranceClaims.Service/InsuranceClaims.Service.csproj src/InsuranceClaims.Service/
COPY src/InsuranceClaims.API/InsuranceClaims.API.csproj src/InsuranceClaims.API/
RUN dotnet restore src/InsuranceClaims.API/InsuranceClaims.API.csproj
COPY src/ src/
RUN dotnet publish src/InsuranceClaims.API/InsuranceClaims.API.csproj -c Release -o /app/publish --no-restore

# ---------- 2. Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./

# NLog writes a rolling file next to the console output; the app user needs to own that folder.
RUN mkdir -p /app/logs && chown -R $APP_UID /app/logs
USER $APP_UID

ENV ASPNETCORE_ENVIRONMENT=Production
# Listens on 8080 by default. Hosts that set PORT (Render) are honoured by Program.cs.
EXPOSE 8080
ENTRYPOINT ["dotnet", "InsuranceClaims.API.dll"]
