# Custom Jenkins image with .NET SDK 8, Node.js 20, and Docker CLI
# Installs dotnet via apt using Microsoft's feed (most reliable method)

FROM jenkins/jenkins:lts

USER root

# Install prerequisites + Docker CLI + Node.js
RUN apt-get update -qq && apt-get install -y -qq \
    curl \
    apt-transport-https \
    ca-certificates \
    gnupg \
    lsb-release \
    && curl -fsSL https://download.docker.com/linux/debian/gpg \
        | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] \
        https://download.docker.com/linux/debian $(lsb_release -cs) stable" \
        > /etc/apt/sources.list.d/docker.list \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get update -qq \
    && apt-get install -y -qq docker-ce-cli nodejs libicu-dev \
    && rm -rf /var/lib/apt/lists/*

# Install .NET 10 SDK using the official install script
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet \
    && ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet \
    && rm /tmp/dotnet-install.sh

# Verify tools
RUN node --version && npm --version && docker --version
RUN dotnet --version

USER jenkins
