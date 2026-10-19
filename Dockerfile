FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY Directory.Build.props Directory.Packages.props ./
COPY src/DeliveryApi/DeliveryApi.csproj src/DeliveryApi/
RUN dotnet restore src/DeliveryApi/DeliveryApi.csproj

COPY src/ src/
RUN dotnet publish src/DeliveryApi/DeliveryApi.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "DeliveryApi.dll"]
