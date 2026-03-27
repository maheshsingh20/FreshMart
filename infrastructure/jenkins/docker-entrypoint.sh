#!/bin/bash
# Fix Docker socket permissions so jenkins user can access it
# This runs as root (via sudo) before Jenkins starts
if [ -S /var/run/docker.sock ]; then
    chmod 666 /var/run/docker.sock
fi

# Start Jenkins normally
exec /usr/bin/tini -- /usr/local/bin/jenkins.sh "$@"
