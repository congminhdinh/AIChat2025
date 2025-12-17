# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["Services/DocumentService/DocumentService.csproj", "Services/DocumentService/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
RUN dotnet restore "./Services/DocumentService/DocumentService.csproj"

# Copy all source code
COPY . .
WORKDIR "/src/Services/DocumentService"

# Build the project
RUN dotnet build "./DocumentService.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Install EF Core tools for migration bundle generation
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

# Generate migration bundle
RUN dotnet ef migrations bundle -c DocumentDbContext -o /app/efbundle --self-contained

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DocumentService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Copy migration bundle from build stage
COPY --from=build /app/efbundle ./efbundle

# Create entrypoint script that runs migrations before starting the application
RUN echo '#!/bin/bash' > /app/entrypoint.sh && \
    echo './efbundle' >> /app/entrypoint.sh && \
    echo 'dotnet DocumentService.dll' >> /app/entrypoint.sh && \
    chmod +x /app/entrypoint.sh && \
    chmod +x /app/efbundle

# Set entrypoint to the custom script
ENTRYPOINT ["./entrypoint.sh"]
