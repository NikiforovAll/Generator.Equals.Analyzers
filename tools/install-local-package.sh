#!/bin/bash

# Build and install the GeneratorEquals.Analyzer package to local NuGet source
# Usage: ./tools/install-local-package.sh

set -e  # Exit on any error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
PACKAGE_PROJECT="$PROJECT_ROOT/src/GeneratorEquals.Analyzer.Package"
LOCAL_NUGET_DIR="$PROJECT_ROOT/../local-nuget-packages"

echo "🔨 Building GeneratorEquals.Analyzer package..."
cd "$PACKAGE_PROJECT"
dotnet build

echo "📦 Finding generated package..."
PACKAGE_FILE=$(find "$PACKAGE_PROJECT/bin/Debug" -name "Nall.Generator.Equals.Analyzers.*.nupkg" | sort -V | tail -1)

if [ ! -f "$PACKAGE_FILE" ]; then
    echo "❌ Error: Package file not found!"
    exit 1
fi

echo "📁 Ensuring local NuGet directory exists..."
mkdir -p "$LOCAL_NUGET_DIR"

echo "📋 Copying package ($PACKAGE_FILE) to local NuGet source..."
cp "$PACKAGE_FILE" "$LOCAL_NUGET_DIR/"

PACKAGE_NAME=$(basename "$PACKAGE_FILE")
echo "✅ Successfully installed: $PACKAGE_NAME"

echo ""
echo "🎉 Package installation complete!"
echo "📍 Package location: $LOCAL_NUGET_DIR/$PACKAGE_NAME"