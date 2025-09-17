#!/bin/bash

# Clean and rebuild the Playground project to test the latest analyzer changes
# Usage: ./tools/rebuild-playground.sh

set -e  # Exit on any error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
PLAYGROUND_PROJECT="$PROJECT_ROOT/src/Playground"

echo "🧹 Cleaning Playground project..."
cd "$PLAYGROUND_PROJECT"
dotnet clean

# echo "🔄 Clearing NuGet cache to ensure latest analyzer is used..."
# dotnet nuget locals all --clear --verbosity quiet

echo "🔨 Building Playground with latest analyzer..."
dotnet build

echo ""
echo "✅ Playground rebuild complete!"
echo "📊 Check the warnings above to see the analyzer in action"
echo ""
echo "To run the playground:"
echo "  cd src/Playground && dotnet run"