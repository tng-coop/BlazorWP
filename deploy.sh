#!/usr/bin/env bash
set -euo pipefail

# Ensure required environment variables are set
: "${Server__User:?Environment variable Server__User must be set}"
: "${Server__Host:?Environment variable Server__Host must be set}"
: "${Server__RemoteBlazorDir:?Environment variable Server__RemoteBlazorDir must be set}"

# Configuration
PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_FILE="$PROJECT_DIR/BlazorWP.csproj"

# Publish output outside of project to avoid contaminating the source tree
PUBLISH_DIR="$PROJECT_DIR/../blazor-publish"
WWWROOT_DIR="$PUBLISH_DIR/wwwroot"
DESIRED_BASE="/blazorapp/"

# Remote deployment info
REMOTE_USER="$Server__User"
REMOTE_HOST="$Server__Host"
REMOTE_WEBPATH="$Server__RemoteBlazorDir"

# Detect wasm-tools workload version
WASM_TOOLS_VER=$(dotnet workload list --no-cache | grep '^wasm-tools' | awk '{print $2}')

# Build publish flags (always include base settings)
PUBLISH_FLAGS=("-c" "Release" "-o" "$PUBLISH_DIR")
# If version starts with 9.0.7, suppress WASM0001 warning via output filter
if [[ "$WASM_TOOLS_VER" == 9.0.7* ]]; then
  echo "→ Detected wasm-tools version $WASM_TOOLS_VER, will suppress WASM0001 warning via output filter"
  SUPPRESS_WASM0001=true
else
  SUPPRESS_WASM0001=false
fi

# Clean previous publish
echo "→ Cleaning old publish…"
rm -rf "$PUBLISH_DIR"

# Perform publish
echo "→ Publishing $PROJECT_FILE to $PUBLISH_DIR…"
if [[ "$SUPPRESS_WASM0001" == true ]]; then
  # Capture output and exit code, then filter out WASM0001 warnings
  set +o pipefail
  PUBLISH_OUTPUT=$(dotnet publish "$PROJECT_FILE" "${PUBLISH_FLAGS[@]}" 2>&1)
  PUBLISH_EXIT=$?
  set -o pipefail
  echo "$PUBLISH_OUTPUT" | sed '/warning WASM0001/d'
  if [[ $PUBLISH_EXIT -ne 0 ]]; then
    exit $PUBLISH_EXIT
  fi
else
  dotnet publish "$PROJECT_FILE" "${PUBLISH_FLAGS[@]}"
fi

# Patch base href in the generated index.html
echo "→ Patching <base> href in $WWWROOT_DIR/index.html…"
if [[ -f "$WWWROOT_DIR/index.html" ]]; then
  sed -i -E \
    "s|<base href=\"[^\"]*\" ?/?>|<base href=\"$DESIRED_BASE\" />|" \
    "$WWWROOT_DIR/index.html"
else
  echo "❌ ERROR: $WWWROOT_DIR/index.html not found"
  exit 1
fi

# Deploy via rsync
echo "→ Rsyncing to $REMOTE_HOST:$REMOTE_WEBPATH…"
rsync -avz --delete \
  "$WWWROOT_DIR/" \
  "${REMOTE_USER}@${REMOTE_HOST}:${REMOTE_WEBPATH}/"

echo "✅ Deployment complete!"
