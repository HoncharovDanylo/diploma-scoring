FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY gateway/ApiGateway/ApiGateway.csproj gateway/ApiGateway/
RUN dotnet restore gateway/ApiGateway/ApiGateway.csproj
COPY gateway/ApiGateway/ gateway/ApiGateway/
RUN dotnet publish gateway/ApiGateway/ApiGateway.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker
EXPOSE 8080
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
