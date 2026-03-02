# -------- Build stage --------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY ./ ./

RUN echo "Building MarketData Adapter Handler container"

WORKDIR /app/src/MarketData.Adapter.Handler
RUN ls
RUN dotnet restore

RUN dotnet build "MarketData.Adapter.Handler.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MarketData.Adapter.Handler.csproj" -c Release -o /app/publish

# -------- Runtime stage --------
FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "MarketData.Adapter.Handler.dll"]
