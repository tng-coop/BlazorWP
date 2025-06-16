#!/usr/bin/env bash
set -euo pipefail

# ------------------------------------------------------------------
# deploy-cms-plugins.sh
#
# Installs & activates WordPress plugins remotely using WP-CLI.
# Ensures JWT Authentication plugin is always installed.
# Usage:
#   ./deploy-cms-plugins.sh [plugin-slug ...]
#   (or list slugs in plugins.txt, one per line, then run without args)
# Requires environment variables:
#   Server__User           - SSH username
#   Server__Host           - SSH hostname
#   Server__RemoteCmsDir   - Remote directory for WordPress CMS root
#   Server__Group          - Group for file ownership on remote
# ------------------------------------------------------------------

# Ensure required environment variables are set
: "${Server__User:?Environment variable Server__User must be set}"
: "${Server__Host:?Environment variable Server__Host must be set}"
: "${Server__RemoteCmsDir:?Environment variable Server__RemoteCmsDir must be set}"
: "${Server__Group:?Environment variable Server__Group must be set}"

# Mandatory plugin slug
JWT_SLUG="jwt-authentication-for-wp-rest-api"

# Build plugin list from args or plugins.txt; default to JWT if none provided
PLUGINS=()
if [ $# -gt 0 ]; then
  PLUGINS=("$@")
elif [ -f plugins.txt ]; then
  mapfile -t PLUGINS < <(grep -E '^[^#[:space:]]+' plugins.txt)
else
  echo "ℹ️ No plugins specified; defaulting to mandatory plugin: ${JWT_SLUG}"
  PLUGINS=("${JWT_SLUG}")
fi

# Ensure JWT plugin is always included
if [[ ! " ${PLUGINS[*]} " =~ " ${JWT_SLUG} " ]]; then
  PLUGINS+=("${JWT_SLUG}")
  echo "ℹ️ Added mandatory plugin: ${JWT_SLUG}"
fi

echo "🌐 Deploying to ${Server__User}@${Server__Host}:${Server__RemoteCmsDir}"

echo "🔒 Running remote commands via SSH..."
ssh "${Server__User}@${Server__Host}" bash -s <<EOF
set -euo pipefail

echo "→ Entering CMS directory: ${Server__RemoteCmsDir}"
cd "${Server__RemoteCmsDir}"

# Check for WP-CLI
if ! command -v wp &>/dev/null; then
  echo "Error: wp (WP-CLI) not found on remote." >&2
  exit 1
fi

echo
printf "Starting plugin installation/activation...\n"
for slug in ${PLUGINS[*]}; do
  printf "→ Processing plugin: %s\n" "\$slug"
  if ! wp plugin is-installed "\$slug" --quiet; then
    printf "   • Installing %s...\n" "\$slug"
    wp plugin install "\$slug" --activate
  elif ! wp plugin is-active "\$slug" --quiet; then
    printf "   • Activating %s...\n" "\$slug"
    wp plugin activate "\$slug"
  else
    printf "   • Already installed & active: %s\n" "\$slug"
  fi
  printf "\n"
done

echo "🔄 Flushing rewrite rules..."
wp rewrite flush --hard

echo
printf "🛡️ Setting group ownership to %s...\n" "${Server__Group}"
chgrp -R "${Server__Group}" .
EOF

echo "✅ CMS plugin deployment complete!"
