# Custom Jenkins image with .NET SDK 10, Node.js 20, and Docker CLI
FROM jenkins/jenkins:lts

USER root

# Install prerequisites + Docker CLI + Node.js + libicu (required by .NET)
RUN apt-get update -qq && apt-get install -y -qq \
    curl \
    apt-transport-https \
    ca-certificates \
    gnupg \
    lsb-release \
    libicu-dev \
    wget \
    sudo \
    && curl -fsSL https://download.docker.com/linux/debian/gpg \
        | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] \
        https://download.docker.com/linux/debian $(lsb_release -cs) stable" \
        > /etc/apt/sources.list.d/docker.list \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get update -qq \
    && apt-get install -y -qq docker-ce-cli nodejs \
    && rm -rf /var/lib/apt/lists/*

# Allow jenkins to run chmod on docker socket without password
RUN echo "jenkins ALL=(root) NOPASSWD: /bin/chmod 666 /var/run/docker.sock" \
    >> /etc/sudoers.d/jenkins-docker \
    && chmod 0440 /etc/sudoers.d/jenkins-docker

# Install .NET 10 SDK — retry up to 3 times in case of transient network errors
RUN for i in 1 2 3; do \
      curl -fsSL --retry 5 --retry-delay 5 \
        https://dot.net/v1/dotnet-install.sh \
        -o /tmp/dotnet-install.sh && break; \
      echo "Attempt $i failed, retrying..."; \
      sleep 10; \
    done \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet \
    && ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet \
    && rm /tmp/dotnet-install.sh

USER jenkins
