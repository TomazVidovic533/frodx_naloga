FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/OrderIngestion.MockApi/OrderIngestion.MockApi.csproj", "src/OrderIngestion.MockApi/"]
RUN dotnet restore "src/OrderIngestion.MockApi/OrderIngestion.MockApi.csproj"

COPY . .
WORKDIR "/src/src/OrderIngestion.MockApi"
RUN dotnet build "OrderIngestion.MockApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrderIngestion.MockApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderIngestion.MockApi.dll"]