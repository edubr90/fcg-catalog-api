FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY fcg-shared-events/ fcg-shared-events/
COPY fcg-catalog-api/ fcg-catalog-api/
WORKDIR /src/fcg-catalog-api
RUN dotnet restore src/CatalogAPI/CatalogAPI.csproj
RUN dotnet publish src/CatalogAPI/CatalogAPI.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CatalogAPI.dll"]