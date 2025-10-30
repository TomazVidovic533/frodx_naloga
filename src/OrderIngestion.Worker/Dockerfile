FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/OrderIngestion.Domain/OrderIngestion.Domain.csproj", "src/OrderIngestion.Domain/"]
COPY ["src/OrderIngestion.Application/OrderIngestion.Application.csproj", "src/OrderIngestion.Application/"]
COPY ["src/OrderIngestion.Infrastructure/OrderIngestion.Infrastructure.csproj", "src/OrderIngestion.Infrastructure/"]
COPY ["src/OrderIngestion.Worker/OrderIngestion.Worker.csproj", "src/OrderIngestion.Worker/"]
RUN dotnet restore "src/OrderIngestion.Worker/OrderIngestion.Worker.csproj"

COPY . .
WORKDIR "/src/src/OrderIngestion.Worker"
RUN dotnet build "OrderIngestion.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrderIngestion.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderIngestion.Worker.dll"]