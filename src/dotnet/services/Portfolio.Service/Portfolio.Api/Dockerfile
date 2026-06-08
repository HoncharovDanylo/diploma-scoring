FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY building-blocks/Lending.Contracts/Lending.Contracts.csproj building-blocks/Lending.Contracts/
COPY services/Portfolio.Service/Portfolio.Domain/Portfolio.Domain.csproj services/Portfolio.Service/Portfolio.Domain/
COPY services/Portfolio.Service/Portfolio.Application/Portfolio.Application.csproj services/Portfolio.Service/Portfolio.Application/
COPY services/Portfolio.Service/Portfolio.Infrastructure/Portfolio.Infrastructure.csproj services/Portfolio.Service/Portfolio.Infrastructure/
COPY services/Portfolio.Service/Portfolio.Api/Portfolio.Api.csproj services/Portfolio.Service/Portfolio.Api/
RUN dotnet restore services/Portfolio.Service/Portfolio.Api/Portfolio.Api.csproj
COPY . .
RUN dotnet publish services/Portfolio.Service/Portfolio.Api/Portfolio.Api.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Portfolio.Api.dll"]
