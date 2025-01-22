# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Install ML.NET tools
RUN dotnet tool install --tool-path /tools mlnet-linux-x64 \
    && dotnet tool install --tool-path /tools mlnet-predict 

# Add tools to PATH
ENV PATH="${PATH}:/tools"

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Copy ML.NET tools from build stage
COPY --from=build /tools /tools
ENV PATH="${PATH}:/tools"

WORKDIR /app

# Install .NET 8.0 runtime (required for some ML.NET operations)
RUN wget https://dot.net/v1/dotnet-install.sh \
    && chmod +x dotnet-install.sh \
    && ./dotnet-install.sh --runtime dotnet --version 8.0.0 --install-dir /usr/share/dotnet \
    && rm dotnet-install.sh

# Set environment variables
ENV Storage__BasePath="/var/data/mloop"
ENV ConnectionStrings__QueueConnection=""
ENV DOTNET_ENVIRONMENT="Production"

# Create necessary directories with correct permissions
RUN mkdir -p /var/data/mloop /.nuget/NuGet \
    && chown -R 1000:1000 /var/data/mloop /.nuget \
    && chmod -R 755 /.nuget

## Health check
#HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 \
    #CMD curl -f http://localhost/health || exit 1

# Copy application files
COPY ./publish /app/

# Set non-root user
USER 1000:1000

# Declare volume mount point
VOLUME ["/var/data"]

# Set entry point
ENTRYPOINT ["dotnet", "MLoop.Worker.dll"]