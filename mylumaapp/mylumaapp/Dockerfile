# Use the official .NET SDK (7.0) image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the project file and restore dependencies
COPY *.csproj .
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build and publish in Release mode
RUN dotnet publish -c Release -o /out

# Use a smaller runtime image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /out .

# Expose port 80 for the application
EXPOSE 80

# Set the entrypoint to your application DLL
ENTRYPOINT ["dotnet", "MyLumaApp.dll"]
