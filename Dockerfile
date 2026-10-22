# One image for the whole system: the React client is built and served by the ASP.NET Core API.
# Build from the repository root:  docker build -t insurance-claims .

# ---------- 1. React client ----------
FROM node:22-alpine AS client
WORKDIR /client
COPY client/package.json client/package-lock.json ./
RUN npm ci
COPY client/ ./
RUN npm run build

# ---------- 2. .NET API ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Project files first, so the restore layer is cached until a package changes.
COPY server/src/InsuranceClaims.Core/InsuranceClaims.Core.csproj server/src/InsuranceClaims.Core/
COPY server/src/InsuranceClaims.Data/InsuranceClaims.Data.csproj server/src/InsuranceClaims.Data/
COPY server/src/InsuranceClaims.Service/InsuranceClaims.Service.csproj server/src/InsuranceClaims.Service/
COPY server/src/InsuranceClaims.API/InsuranceClaims.API.csproj server/src/InsuranceClaims.API/
RUN dotnet restore server/src/InsuranceClaims.API/InsuranceClaims.API.csproj
COPY server/src/ server/src/
RUN dotnet publish server/src/InsuranceClaims.API/InsuranceClaims.API.csproj -c Release -o /app/publish --no-restore

# ---------- 3. Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./
COPY --from=client /client/dist ./wwwroot

# NLog writes a rolling file next to the console output; the app user needs to own that folder.
RUN mkdir -p /app/logs && chown -R $APP_UID /app/logs
USER $APP_UID

ENV ASPNETCORE_ENVIRONMENT=Production
# Listens on 8080 by default. Hosts that set PORT (Render) are honoured by Program.cs.
EXPOSE 8080
ENTRYPOINT ["dotnet", "InsuranceClaims.API.dll"]
