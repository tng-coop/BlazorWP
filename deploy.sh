#!/usr/bin/env bash
set -euo pipefail

# Repo root
PROJECT_DIR="$(pwd)"

# Publish *outside* the project, e.g. one level up
PUBLISH_DIR="$PROJECT_DIR/../blazor-publish"
WWWROOT_DIR="$PUBLISH_DIR/wwwroot"
DESIRED_BASE="/blazorapp/"

# Remote info
REMOTE_USER="yasuaki"
REMOTE_HOST="claudette.mayfirst.org"
REMOTE_WEBPATH="~/web/blazorapp"

echo "→ Cleaning old publish…"
rm -rf "$PUBLISH_DIR"

echo "→ Publishing Blazor WASM to $PUBLISH_DIR…"
dotnet publish -c Release \
  -o "$PUBLISH_DIR"

echo "→ Patching <base> href…"
sed -i -E "s|<base href=\"[^\"]*\" ?/?>|<base href=\"$DESIRED_BASE\" />|" \
  "$WWWROOT_DIR/index.html"

echo "→ Rsync to server…"
rsync -avz --delete \
  "$WWWROOT_DIR/" \
  "$REMOTE_USER@$REMOTE_HOST:$REMOTE_WEBPATH/"

echo "✅ Deployment complete!"
