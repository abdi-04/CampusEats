# Build stage, ensure we have the SDK to build our app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
 
# Clear Windows-specific NuGet fallback folders that break Linux builds 
ENV NUGET_FALLBACK_PACKAGES=""
 
# Copy solution and project files first (layer cache for restore)
COPY ["src/CampusEatsv2.Web/CampusEatsv2.Web.csproj", "src/CampusEatsv2.Web/"]
COPY ["src/CampusEatsv2.Core/CampusEatsv2.Core.csproj", "src/CampusEatsv2.Core/"]
COPY ["src/CampusEatsv2.Infrastructure/CampusEatsv2.Infrastructure.csproj", "src/CampusEatsv2.Infrastructure/"]

# Run from Web as this is a web app and it will find the other projects via project references
RUN dotnet restore "src/CampusEatsv2.Web/CampusEatsv2.Web.csproj"
 
# Copy everything else and publish
COPY . .
RUN dotnet publish "src/CampusEatsv2.Web/CampusEatsv2.Web.csproj" \
    -c Release \
    -o /app/publish 

# Runtime stage, lets make our app run! // LINE 24 is just a missing lib for kerberos auth, which is used by the app to connect to SQL Server
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .

# Set the entry point to run the app when the container starts
ENTRYPOINT ["dotnet", "CampusEatsv2.Web.dll"]