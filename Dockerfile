# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["MockPaymentService/MockPaymentService.csproj", "MockPaymentService/"]
RUN dotnet restore "MockPaymentService/MockPaymentService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/MockPaymentService"
RUN dotnet build "MockPaymentService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MockPaymentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Create logs directory and set permissions
RUN mkdir -p /app/logs && chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Copy published app
COPY --from=publish --chown=appuser:appuser /app/publish .

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Set the entry point
ENTRYPOINT ["dotnet", "MockPaymentService.dll"]