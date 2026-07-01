# Build stage, ensure we have the SDK to build our app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
 
# Clear Windows-specific NuGet fallback folders that break Linux builds 
ENV NUGET_FALLBACK_PACKAGES=""
 
# Copy solution and project files first (layer cache for restore)
COPY ["src/CampusEats.Web/CampusEats.Web.csproj", "src/CampusEats.Web/"]
COPY ["src/CampusEats.Core/CampusEats.Core.csproj", "src/CampusEats.Core/"]
COPY ["src/CampusEats.Infrastructure/CampusEats.Infrastructure.csproj", "src/CampusEats.Infrastructure/"]

# Run from Web as this is a web app and it will find the other projects via project references
RUN dotnet restore "src/CampusEats.Web/CampusEats.Web.csproj"
 
# Copy everything else and publish
COPY . .
RUN dotnet publish "src/CampusEats.Web/CampusEats.Web.csproj" \
    -c Release \
    -o /app/publish 

# Runtime stage, lets make our app run! // LINE 24 is just a missing lib for kerberos auth, which is used by the app to connect to SQL Server
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .

# Set the entry point to run the app when the container starts
ENTRYPOINT ["dotnet", "CampusEats.Web.dll"]