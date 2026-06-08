FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY services/Identity.Service/Identity.Domain/Identity.Domain.csproj services/Identity.Service/Identity.Domain/
COPY services/Identity.Service/Identity.Application/Identity.Application.csproj services/Identity.Service/Identity.Application/
COPY services/Identity.Service/Identity.Infrastructure/Identity.Infrastructure.csproj services/Identity.Service/Identity.Infrastructure/
COPY services/Identity.Service/Identity.Api/Identity.Api.csproj services/Identity.Service/Identity.Api/
RUN dotnet restore services/Identity.Service/Identity.Api/Identity.Api.csproj
COPY . .
RUN dotnet publish services/Identity.Service/Identity.Api/Identity.Api.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Identity.Api.dll"]
