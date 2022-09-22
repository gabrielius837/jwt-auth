# Build
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine as build
WORKDIR /build
COPY . .
RUN dotnet restore "./src/App.Api/App.Api.csproj"
RUN dotnet publish "./src/App.Api/App.Api.csproj" -c Release -o /app --no-restore

# Serve
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine
WORKDIR /app
COPY --from=build /app ./

EXPOSE 5000

ENTRYPOINT ["dotnet", "App.Api.dll"]