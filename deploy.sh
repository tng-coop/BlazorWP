#!/usr/bin/env bash
set -euo pipefail

### REQUIRE ENVIRONMENT VARIABLES #############################################
: "${Server__User:?Environment variable Server__User must be set}"
: "${Server__Host:?Environment variable Server__Host must be set}"
: "${Server__RemoteBlazorDir:?Environment variable Server__RemoteBlazorDir must be set}"

### CONFIGURATION #############################################################
PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_FILE="$PROJECT_DIR/BlazorWP.csproj"

# Publish outside the project to keep builds clean
PUBLISH_DIR="$PROJECT_DIR/../blazor-publish"
WWWROOT_DIR="$PUBLISH_DIR/wwwroot"
DESIRED_BASE="/blazorapp/"

# Remote deployment info from environment
REMOTE_USER="$Server__User"
REMOTE_HOST="$Server__Host"
REMOTE_WEBPATH="$Server__RemoteBlazorDir"

### STEPS #####################################################################
echo "→ Cleaning old publish…"
rm -rf "$PUBLISH_DIR"

echo "→ Publishing $PROJECT_FILE to $PUBLISH_DIR…"
dotnet publish "$PROJECT_FILE" \
  -c Release \
  -o "$PUBLISH_DIR"

echo "→ Patching <base> href in $WWWROOT_DIR/index.html…"
if [[ -f "$WWWROOT_DIR/index.html" ]]; then
  sed -i -E \
    "s|<base href=\"[^\"]*\" ?/?>|<base href=\"$DESIRED_BASE\" />|" \
    "$WWWROOT_DIR/index.html"
else
  echo "❌ ERROR: $WWWROOT_DIR/index.html not found"
  exit 1
fi

echo "→ Rsyncing to $REMOTE_HOST:$REMOTE_WEBPATH…"
rsync -avz --delete \
  "$WWWROOT_DIR/" \
  "${REMOTE_USER}@${REMOTE_HOST}:${REMOTE_WEBPATH}/"

echo "✅ Deployment complete!"
