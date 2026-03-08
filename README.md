# Zantes Tweak

Zantes Tweak is a proprietary Windows optimization suite built for profile-based system tuning, live telemetry, benchmarking, Discord session integration, and controlled public release delivery.

## Repository

- GitHub owner: `arapxd`
- GitHub repo: `ZantesTweak`
- Release page used by the in-app updater:
  `https://github.com/arapxd/ZantesTweak/releases`

## Licensing

This repository is public for viewing only.

- Source code is **not open source**
- Usage, redistribution, reuse, resale, and derivative works are prohibited without written permission
- See [LICENSE.txt](LICENSE.txt) and [EULA.txt](EULA.txt)

## Build

```powershell
dotnet build .\ZantesEngine\ZantesEngine.csproj
```

## Release Build

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-release-package.ps1
```

Installer output:

```text
release\installer\ZantesTweak-Setup-<version>.exe
```

## First Push

```powershell
git init
git branch -M main
git remote add origin https://github.com/arapxd/ZantesTweak.git
git add .
git commit -m "Initial release-ready import"
git push -u origin main
```

## Release Flow

1. Update `<Version>` in `ZantesEngine/ZantesEngine.csproj`
2. Run the release build script
3. Create a GitHub release with tag `vX.Y.Z`
4. Upload the generated installer to the release assets
5. The app updater will detect the latest public release
