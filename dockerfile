# Use the official .NET 8 SDK image for building the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory inside the container
WORKDIR /app

# Copy the project files to the working directory
COPY ./*.sln ./
COPY ./Carhub.Service.Users.Api/*.csproj ./Carhub.Service.Users.Api/
COPY ./Carhub.Service.Users.Core/*.csproj ./Carhub.Service.Users.Core/

# Restore dependencies
RUN dotnet restore

# Copy the remaining files
COPY ./Carhub.Service.Users.Api ./Carhub.Service.Users.Api
COPY ./Carhub.Service.Users.Core ./Carhub.Service.Users.Core

# Build the project in release mode
RUN dotnet publish ./Carhub.Service.Users.Api.Carhub.Service.Users.Api.csproj -c Release -o /app/publish

# Use the official .NET 8 runtime image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory
WORKDIR /app

# Copy the build output to the runtime image
COPY --from=build /app/publih .

# Expose the default port for the API
EXPOSE 80

# Set the entrypoint to run the applicaion
ENTRYPOINT [ "dotnet", "Carhub.Service.Users.Api.dll" ]