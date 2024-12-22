# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /tools

# Install ML.NET tools
RUN dotnet tool install --tool-path /tools mlnet-linux-x64 \
    && dotnet tool install --tool-path /tools mlnet-predict

WORKDIR /app

# Install .NET 8.0 runtime
RUN wget https://dot.net/v1/dotnet-install.sh \
    && chmod +x dotnet-install.sh \
    && ./dotnet-install.sh --runtime dotnet --version 8.0.0 --install-dir /usr/share/dotnet \
    && rm dotnet-install.sh

# Add tools to PATH
ENV PATH="$PATH:/tools"

# Set environment variables
ENV Storage__BasePath="/var/data/mloop"

# Create necessary directories and set permissions
RUN mkdir -p /var/data/mloop /.nuget/NuGet \
    && chown -R 1000:1000 /var/data/mloop /.nuget \
    && chmod -R 755 /.nuget

# Copy pre-built application files
COPY ./publish /app/

# Set non-root user
USER 1000:1000

# Declare volume mount point
VOLUME ["/var/data"]

# Set entry point
ENTRYPOINT ["dotnet", "MLoop.Worker.dll"]