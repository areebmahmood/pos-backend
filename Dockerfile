# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the dependency layer is cached between code changes.
COPY Directory.Build.props Pos.slnx ./
COPY src/Pos.Domain/Pos.Domain.csproj src/Pos.Domain/
COPY src/Pos.Application/Pos.Application.csproj src/Pos.Application/
COPY src/Pos.Infrastructure/Pos.Infrastructure.csproj src/Pos.Infrastructure/
COPY src/Pos.Api/Pos.Api.csproj src/Pos.Api/
RUN dotnet restore src/Pos.Api/Pos.Api.csproj

COPY src/ src/
RUN dotnet publish src/Pos.Api/Pos.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app .
USER app
ENTRYPOINT ["dotnet", "Pos.Api.dll"]
