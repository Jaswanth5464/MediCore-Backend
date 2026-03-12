# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ["MediCore.API/MediCore.API.csproj", "MediCore.API/"]
RUN dotnet restore "MediCore.API/MediCore.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/app/MediCore.API"
RUN dotnet build "MediCore.API.csproj" -c Release -o /app/build

# Publish Stage
FROM build-env AS publish
RUN dotnet publish "MediCore.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MediCore.API.dll"]
