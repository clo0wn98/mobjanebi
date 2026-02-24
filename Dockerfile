# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file
COPY ["EShop/EShop.csproj", "EShop/"]
WORKDIR /src/EShop

# Restore dependencies
RUN dotnet restore

# Copy everything and build
COPY EShop/ .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Create data directory for SQLite database
RUN mkdir -p /app/data

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000
ENV ConnectionStrings__DefaultConnection=Data Source=/app/data/EShop.db

# Expose port
EXPOSE 5000

ENTRYPOINT ["dotnet", "EShop.dll"]
