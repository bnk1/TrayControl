#!/usr/bin/env bash
set -euo pipefail

SOLUTION_ROOT="${1:-.}"
APP_PROJ="${2:-./CompactAppWinForms/CompactAppWinForms.csproj}"
INSTALLER_PROJ="${3:-./Installer/CompactApp.wixproj}"
CONFIGURATION="${4:-Release}"

echo "Build Installer Script"
echo "SolutionRoot: $SOLUTION_ROOT"
echo "AppProj: $APP_PROJ"
echo "InstallerProj: $INSTALLER_PROJ"
echo "Configuration: $CONFIGURATION"
echo ""

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet CLI not found. Install .NET SDK (8.0+) or add dotnet to PATH." >&2
  exit 1
fi

pushd "$SOLUTION_ROOT" >/dev/null

echo "Restoring NuGet packages..."
dotnet restore

echo "Building application project..."
dotnet build "$APP_PROJ" -c "$CONFIGURATION"

echo "Building installer project..."
dotnet build "$INSTALLER_PROJ" -c "$CONFIGURATION"

INSTALLER_DIR=$(dirname "$INSTALLER_PROJ")
SEARCH_PATH="$INSTALLER_DIR/bin/$CONFIGURATION"
echo "Searching for MSI in: $SEARCH_PATH"

MSI_PATH=$(find "$SEARCH_PATH" -type f -iname '*.msi' -print0 2>/dev/null | xargs -0 ls -1t 2>/dev/null | head -n1 || true)

if [ -z "$MSI_PATH" ]; then
  echo "No MSI found under $SEARCH_PATH. Check build output or project OutputName." >&2
  popd >/dev/null
  exit 1
fi

echo ""
echo "MSI built successfully:"
echo "$MSI_PATH"

popd >/dev/null
