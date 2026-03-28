#!/usr/bin/env bash
set -euo pipefail

dotnet dev-certs https --trust >/dev/null 2>&1 || true

echo
echo "Codespace ready."
echo "Expected ports:"
echo "  18888  Aspire Dashboard"
echo "  5173   React/Vite frontend"
echo "  5000   API HTTP"
echo "  5001   API HTTPS"
echo
echo "Codespaces should normally set ConnectionStrings__cosmos."
echo "Set UseContainerEmulators=true only when a local container runtime is available."
echo "If AppHost still fails, verify src/AppHost/Trace.AppHost.csproj includes Aspire.AppHost.Sdk and rerun dotnet restore."