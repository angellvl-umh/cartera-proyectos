FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CarteraProyectos.Core/CarteraProyectos.Core.csproj CarteraProyectos.Core/
COPY CarteraProyectos.Infrastructure/CarteraProyectos.Infrastructure.csproj CarteraProyectos.Infrastructure/
COPY CarteraProyectos.Api/CarteraProyectos.Api.csproj CarteraProyectos.Api/
RUN dotnet restore CarteraProyectos.Api/CarteraProyectos.Api.csproj

COPY . .
RUN dotnet publish CarteraProyectos.Api/CarteraProyectos.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CarteraProyectos.Api.dll"]
