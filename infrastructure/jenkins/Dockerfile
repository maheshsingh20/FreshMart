# Custom Jenkins image with .NET SDK 8, Node.js 20, and Docker CLI
# Uses multi-stage build to copy dotnet from official image (no download needed)

# Stage 1: source of dotnet binaries
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet-source

# Stage 2: Jenkins with all tools
FROM jenkins/jenkins:lts

USER root

# Install prerequisites + libicu (required by .NET) + Docker CLI + Node.js
RUN apt-get update -qq && apt-get install -y -qq \
    curl \
    apt-transport-https \
    ca-certificates \
    gnupg \
    lsb-release \
    libicu-dev \
    libssl-dev \
    && curl -fsSL https://download.docker.com/linux/debian/gpg \
        | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] \
        https://download.docker.com/linux/debian $(lsb_release -cs) stable" \
        > /etc/apt/sources.list.d/docker.list \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get update -qq \
    && apt-get install -y -qq docker-ce-cli nodejs \
    && rm -rf /var/lib/apt/lists/*

# Copy .NET SDK from official image (avoids downloading 200MB during build)
COPY --from=dotnet-source /usr/share/dotnet /usr/share/dotnet
RUN ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet

# Set dotnet to use invariant globalization as fallback if ICU still missing
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Verify all tools installed correctly
RUN dotnet --version && node --version && npm --version && docker --version

USER jenkins
