FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY building-blocks/Lending.Contracts/Lending.Contracts.csproj building-blocks/Lending.Contracts/
COPY services/Origination.Service/Origination.Domain/Origination.Domain.csproj services/Origination.Service/Origination.Domain/
COPY services/Origination.Service/Origination.Application/Origination.Application.csproj services/Origination.Service/Origination.Application/
COPY services/Origination.Service/Origination.Infrastructure/Origination.Infrastructure.csproj services/Origination.Service/Origination.Infrastructure/
COPY services/Origination.Service/Origination.Api/Origination.Api.csproj services/Origination.Service/Origination.Api/
RUN dotnet restore services/Origination.Service/Origination.Api/Origination.Api.csproj
COPY . .
RUN dotnet publish services/Origination.Service/Origination.Api/Origination.Api.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Origination.Api.dll"]
