#!/usr/bin/env bash
set -euo pipefail

echo "Restoring .NET..."
if find . -maxdepth 2 -name "*.sln" | grep -q .; then
  while IFS= read -r sln; do
    dotnet restore "$sln"
  done < <(find . -maxdepth 2 -name "*.sln" | sort)
else
  while IFS= read -r proj; do
    dotnet restore "$proj"
  done < <(find . -name "*.csproj" \
    -not -path "*/bin/*" \
    -not -path "*/obj/*" | sort)
fi

if [ -f ".config/dotnet-tools.json" ]; then
  echo "Restoring local dotnet tools..."
  dotnet tool restore
fi

echo "Installing Node packages..."
while IFS= read -r pkg; do
  dir="$(dirname "$pkg")"

  if [ -f "$dir/package-lock.json" ]; then
    echo "npm ci -> $dir"
    (cd "$dir" && npm ci)
  else
    echo "npm install -> $dir"
    (cd "$dir" && npm install)
  fi
done < <(find . -name "package.json" \
  -not -path "*/node_modules/*" \
  -not -path "*/dist/*" \
  -not -path "*/build/*" | sort)