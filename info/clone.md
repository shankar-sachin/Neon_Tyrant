# Clone and Setup

## 1. Clone
```bash
git clone https://github.com/atom-bowl/TyrantErr.git
cd TyrantErr
```

## 2. Requirements
- Windows 10/11 recommended
- .NET 8 SDK or newer
- Python 3
- Optional: MSVC Build Tools (`cl.exe`) for native C/C++ modules

## 3. Build
Windows cmd:
```bat
scripts\build.bat
```

PowerShell:
```powershell
./scripts/build.ps1
```

Git Bash / WSL:
```bash
./scripts/build.sh
```

## 4. Run
Windows cmd:
```bat
scripts\run.bat
```

PowerShell:
```powershell
./scripts/run.ps1
```

## 5. Clean
```bat
scripts\clean.bat
```

## Optional Native Build Behavior
- If `cl.exe` is found, native modules are built:
  - `physics.dll` (C++)
  - `score_store.exe` (C)
- If `cl.exe` is missing, managed fallback mode is used.
