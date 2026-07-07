# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AerodyneCompressors.csproj .
RUN dotnet restore AerodyneCompressors.csproj

COPY . .
RUN dotnet publish AerodyneCompressors.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AerodyneCompressors.dll"]
