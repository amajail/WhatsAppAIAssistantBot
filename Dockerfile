# Use the official .NET 8 runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the official .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["src/WhatsAppAIAssistantBot.Api/WhatsAppAIAssistantBot.Api.csproj", "src/WhatsAppAIAssistantBot.Api/"]
COPY ["src/WhatsAppAIAssistantBot.Application/WhatsAppAIAssistantBot.Application.csproj", "src/WhatsAppAIAssistantBot.Application/"]
COPY ["src/WhatsAppAIAssistantBot.Infrastructure/WhatsAppAIAssistantBot.Infrastructure.csproj", "src/WhatsAppAIAssistantBot.Infrastructure/"]
COPY ["src/WhatsAppAIAssistantBot.Domain/WhatsAppAIAssistantBot.Domain.csproj", "src/WhatsAppAIAssistantBot.Domain/"]
COPY ["src/WhatsAppAIAssistantBot.Models/WhatsAppAIAssistantBot.Models.csproj", "src/WhatsAppAIAssistantBot.Models/"]

# Restore dependencies
RUN dotnet restore "src/WhatsAppAIAssistantBot.Api/WhatsAppAIAssistantBot.Api.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/WhatsAppAIAssistantBot.Api"
RUN dotnet build "WhatsAppAIAssistantBot.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WhatsAppAIAssistantBot.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WhatsAppAIAssistantBot.Api.dll"]