FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore
COPY EShop/EShop.csproj ./EShop/
RUN dotnet restore EShop/EShop.csproj

# Copy all files and build
COPY EShop/ .
RUN dotnet publish EShop/EShop.csproj -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:$PORT
EXPOSE $PORT
ENTRYPOINT ["dotnet", "EShop.dll"]
