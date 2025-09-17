# Claude Code Instructions

## Task Master AI Instructions
**Import Task Master's development workflow commands and guidelines, treat as if import is in the main CLAUDE.md file.**
@./.taskmaster/CLAUDE.md

## Build Instructions

- for build use "dotnet build -p:WarningLevel=0 /clp:ErrorsOnly"


- before rebuilding playground, ask user if we want to bump package version in source code in Package and Playground itself; than run tools ./tools/install-local-package.sh and ./tools/rebuild-playground.sh
- do not use playground for testing; it should be use only for releases, use tests instead