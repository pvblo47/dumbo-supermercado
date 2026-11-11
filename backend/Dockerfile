FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props NuGet.config ./
COPY backend/ backend/
RUN dotnet restore backend/src/Dumbo.Api/Dumbo.Api.csproj --locked-mode --configfile NuGet.config
RUN dotnet publish backend/src/Dumbo.Api/Dumbo.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Dumbo.Api.dll"]
