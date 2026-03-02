# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY ./ ./

RUN echo 'Building MarketData Api container'

WORKDIR /app/src/MarketData.Adapter.Api
RUN ls
RUN dotnet restore

RUN dotnet build "MarketData.Adapter.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MarketData.Adapter.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MarketData.Adapter.Api.dll"]
