#!/usr/bin/env bash
set -euo pipefail

echo "Installing Aspire CLI..."
curl -fsSL https://aspire.dev/install.sh | bash

# Make Aspire CLI available in future shells if needed.
if [ -d "$HOME/.aspire/bin" ]; then
  LINE='export PATH="$HOME/.aspire/bin:$PATH"'
  grep -qxF "$LINE" "$HOME/.bashrc" || echo "$LINE" >> "$HOME/.bashrc"
fi

corepack enable || true

dotnet --info
node --version
npm --version