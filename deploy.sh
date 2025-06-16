#!/usr/bin/env bash
set -e

# Ensure required environment variables are set
: "${Server__User:?Environment variable Server__User must be set}"
: "${Server__Host:?Environment variable Server__Host must be set}"
: "${Server__RemoteDir:?Environment variable Server__RemoteDir must be set}"
: "${Server__Group:?Environment variable Server__Group must be set}"

# Build settings
PROJECT="BlazorWP.csproj"
PUBLISH_ROOT="./publish"
STATIC_DIR="$PUBLISH_ROOT/wwwroot"

# Build the project
echo "🔨  Building $PROJECT..."
dotnet publish "$PROJECT" -c Release -o "$PUBLISH_ROOT"

# Deploy static assets using environment variables directly
echo "📤  Syncing static assets to ${Server__User}@${Server__Host}:${Server__RemoteDir}..."
rsync -avz --delete "$STATIC_DIR/" "${Server__User}@${Server__Host}:${Server__RemoteDir}"

# Fix permissions on the server
echo "🛡️  Setting group ownership to '${Server__Group}' on the server..."
ssh "${Server__User}@${Server__Host}" "chgrp -R ${Server__Group} '${Server__RemoteDir}' && echo '🔒  chgrp complete.'"

echo "✅  Deployment complete!"
