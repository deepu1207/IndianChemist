FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY IndianChemist.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish ./
ENV PORT=8080 ASPNETCORE_HTTP_PORTS=8080 DOTNET_EnableDiagnostics=0 ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "IndianChemist.dll"]
