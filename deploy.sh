#!/usr/bin/env bash
set -e

# Ensure required environment variables are set
: "${Server__User:?Environment variable Server__User must be set}"
: "${Server__Host:?Environment variable Server__Host must be set}"
: "${Server__RemoteBlazorDir:?Environment variable Server__RemoteBlazorDir must be set}"
: "${Server__Group:?Environment variable Server__Group must be set}"

# Build settings
PROJECT="BlazorWP.csproj"
PUBLISH_ROOT="./publish"
STATIC_DIR="$PUBLISH_ROOT/wwwroot"

# Build the project
echo "🔨  Building $PROJECT..."
dotnet publish "$PROJECT" -c Release -o "$PUBLISH_ROOT"

# Deploy static assets using environment variables directly
echo "📤  Syncing static assets to ${Server__User}@${Server__Host}:${Server__RemoteBlazorDir}..."
rsync -avz --delete "$STATIC_DIR/" "${Server__User}@${Server__Host}:${Server__RemoteBlazorDir}"

# ——— UPDATE .htaccess ———
REMOTE_WEB_ROOT="$(dirname "$Server__RemoteBlazorDir")"
echo "🔄 Updating .htaccess in $REMOTE_WEB_ROOT"
ssh "$Server__User@$Server__Host" "cat > '$REMOTE_WEB_ROOT/.htaccess'" << 'EOF'
<IfModule mod_rewrite.c>
  RewriteEngine On
  RewriteBase /

  # (a) Serve .wasm with correct MIME
  AddType application/wasm .wasm

  # (b) Let /cms/* be handled by WordPress
  RewriteRule ^cms(/|$) - [L]

  # (c) If the request matches a real file or dir under web/blazor, serve it:
  RewriteCond %{DOCUMENT_ROOT}/blazor%{REQUEST_URI} -f [OR]
  RewriteCond %{DOCUMENT_ROOT}/blazor%{REQUEST_URI} -d
  RewriteRule ^ blazor%{REQUEST_URI} [L]
</IfModule>
EOF

# ——— PERMISSIONS ———
echo "🛡️ Setting group ownership to $Server__Group"
ssh "$Server__User@$Server__Host" "chgrp -R $Server__Group '$Server__RemoteBlazorDir' '$REMOTE_WEB_ROOT/.htaccess'"

echo "✅ Deployment complete!"
