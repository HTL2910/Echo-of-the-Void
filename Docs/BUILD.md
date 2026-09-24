# Build Guide - Echo of the Void

Build instructions for creating standalone executables for macOS and Windows platforms.

## Build Checklist (Before Building)

- [ ] All tests pass: `EOTV_AGENT=claude Tools/run_tests_isolated.sh`
- [ ] No compilation errors in Unity Editor
- [ ] No missing asset references (search console for "Missing" warnings)
- [ ] Game plays through from MainMenu → Level → can pause/settings/resume
- [ ] All input bindings work (keyboard + gamepad)
- [ ] Audio system responds to volume settings
- [ ] No performance drops (target 60 FPS)

## Build Locations

- **macOS**: `Builds/macOS/Echo of the Void.app`
- **Windows**: `Builds/Windows/Echo of the Void.exe`

Both require a `Builds/` folder at project root (ignored by git).

## Prerequisites

- **Unity Hub** with **Unity 6000.3.13f1** installed
- **macOS builds** require: macOS 10.13+, Xcode command-line tools (`xcode-select --install`)
- **Windows builds** require: Visual Studio Build Tools or Visual Studio Community (C++ workload)

## Building via Unity Editor

### macOS (Standalone)

1. Open the project in Unity Editor
2. **File** → **Build Settings**
3. **Platform** list: select **macOS**
4. **Architecture**: Intel 64-bit + Apple Silicon (universal binary recommended)
5. **Player Settings**:
   - **Product Name**: `Echo of the Void`
   - **Company Name**: leave as-is
   - **Version**: `1.0.0` (or current version)
   - **macOS minimum version**: `10.13`
6. **Build** → save to `Builds/macOS/`
7. Wait for build to complete (~2-5 min depending on machine)

**Post-build:** Codesign the app if distributing:
```bash
codesign -s - Builds/macOS/Echo\ of\ the\ Void.app
```

### Windows (Standalone)

1. Open the project in Unity Editor
2. **File** → **Build Settings**
3. **Platform** list: select **Windows, Mac, Linux**
4. **Architecture**: x86_64 (64-bit)
5. **Player Settings**:
   - **Product Name**: `Echo of the Void`
   - **Company Name**: leave as-is
   - **Version**: `1.0.0`
6. **Build** → save to `Builds/Windows/`
7. Wait for build to complete (~2-5 min)

## Building via Command Line (Headless)

**Requires Unity installed and in PATH.**

### macOS Build

```bash
Unity -batchmode -nographics -quit \
  -projectPath "$(pwd)" \
  -buildOSXUniversalPlayer "$(pwd)/Builds/macOS/Echo of the Void.app" \
  -logFile - 2>&1 | tail -20
```

### Windows Build

```bash
Unity -batchmode -nographics -quit \
  -projectPath "$(pwd)" \
  -buildWindowsPlayer "$(pwd)/Builds/Windows/Echo of the Void.exe" \
  -logFile - 2>&1 | tail -20
```

## Automated Build Script

Create `.claude/hooks/build-all.sh` (or run manually):

```bash
#!/bin/bash
set -e

PROJECT_PATH="$(cd "$(dirname "$0")/../.." && pwd)"
UNITY_PATH="${UNITY_PATH:-/Applications/Unity/Hub/Editors/6000.3.13f1/Unity.app/Contents/MacOS/Unity}"

echo "🔨 Building Echo of the Void..."
echo "Project: $PROJECT_PATH"

# Run tests first
echo "✓ Running tests..."
cd "$PROJECT_PATH"
EOTV_AGENT=claude Tools/run_tests_isolated.sh > /dev/null 2>&1 || {
  echo "❌ Tests failed. Aborting build."
  exit 1
}

# Build macOS
echo "✓ Building macOS universal binary..."
"$UNITY_PATH" -batchmode -nographics -quit \
  -projectPath "$PROJECT_PATH" \
  -buildOSXUniversalPlayer "$PROJECT_PATH/Builds/macOS/Echo of the Void.app" \
  -logFile - 2>&1 | grep -E "(Building|Finished|Error|Warning)" || true

# Build Windows (on macOS, requires Windows installed; skip if not available)
if [ "$(uname)" = "Darwin" ]; then
  echo "⚠️  Windows build not available on macOS. Use Windows machine or CI."
else
  echo "✓ Building Windows..."
  "$UNITY_PATH" -batchmode -nographics -quit \
    -projectPath "$PROJECT_PATH" \
    -buildWindowsPlayer "$PROJECT_PATH/Builds/Windows/Echo of the Void.exe" \
    -logFile - 2>&1 | grep -E "(Building|Finished|Error|Warning)" || true
fi

echo "✓ Builds complete!"
echo "  macOS: $PROJECT_PATH/Builds/macOS/Echo of the Void.app"
echo "  Windows: $PROJECT_PATH/Builds/Windows/Echo of the Void.exe"
```

## Testing the Build

### Launch the Game

**macOS:**
```bash
open "Builds/macOS/Echo of the Void.app"
```

**Windows:**
```bash
Builds/Windows/Echo\ of\ the\ Void.exe
```

### Verify Gameplay

1. **Launch** → MainMenu appears
2. **Menu navigation** → keyboard/gamepad works
3. **New Game** → loads Prototype_Level1
4. **Controls** → A/D run, Space jump, J attack, K dash, Shift realm, U resonance, Esc pause
5. **Settings** → can adjust volume, rebind controls
6. **Pause/Resume** → works correctly
7. **Quit** → returns to menu, then exits cleanly

### Performance Checks

Use **Frame Debugger** (Editor) or **Profiler** (in-game):

- **Target**: 60 FPS on standard hardware (GPU: Intel HD 630, CPU: i5-8400)
- **Memory**: < 500 MB resident
- **CPU frame time**: < 16.6 ms (60 FPS target)
- **GC allocations**: 0 bytes/frame in gameplay loop

## Troubleshooting

### Build Fails with "Missing Assets"

- Check Console for "Missing" warnings
- Verify all scene references are valid
- Ensure `MainMenu.unity` and `Prototype_Level1.unity` exist in `Assets/Scenes/`

### Build Succeeds but Crashes on Launch

- Check `Player.log` (location depends on OS):
  - **macOS**: `~/Library/Logs/Unity/Player.log`
  - **Windows**: `%APPDATA%\LocalLow\DefaultCompany\Echo of the Void\Player.log`
- Verify AudioMixer is assigned in `AudioMixerController` (check `AudioMixerController` component in scene)
- Test in Editor Play mode to isolate issue

### Slow Build Time

- **Disable antivirus scanning** on build folder during build
- **Close other applications** to free RAM
- **Use SSD** if building to HDD

### macOS: "Cannot open app" / "Damaged"

- Codesign the app: `codesign -s - "Builds/macOS/Echo of the Void.app"`
- Or remove quarantine: `xattr -d com.apple.quarantine "Builds/macOS/Echo of the Void.app"`

### Windows: Missing DLLs

- Verify Visual C++ Runtime is installed (Windows Update or [vcredist downloads](https://support.microsoft.com/en-us/help/2977003))
- Check `Builds/Windows/` for `UnityPlayer.dll`, `vcruntime140.dll`

## Distribution

### itch.io / Steam

1. **Test build on clean machine** (no Unity or development tools)
2. **Create package**:
   - macOS: Zip the `.app` folder
   - Windows: Zip the entire `Builds/Windows/` folder
3. **Upload** with version tag (e.g., `v1.0.0`)
4. **Add CREDITS.md** to package (see `Assets/CREDITS.md`)

### Versioning

Update version in **Player Settings** → **Project Settings** → **Version**:
- `1.0.0` for initial release
- `1.0.1`, `1.0.2`... for patches
- `1.1.0`, `2.0.0`... for major/minor updates

## Notes

- **64-bit only**: macOS and Windows builds are 64-bit. 32-bit support was dropped.
- **Input System**: Game uses New Input System. Ensure **Edit** → **Project Settings** → **Player** → **Input System Package** is active.
- **Resolution**: Default 1920×1080 (16:9). Can be overridden in game settings.
- **Audio**: Requires AudioMixer asset. If missing, game runs but audio is silent.

---

**Last updated:** 2026-09-24  
**Built with:** Unity 6000.3.13f1  
**Target platforms:** macOS 10.13+, Windows 10+  
**Tested on:** (Add test results after first build)
