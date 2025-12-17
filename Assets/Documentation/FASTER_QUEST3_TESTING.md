# Faster Quest 3 Testing Methods

## Current Problem
Building and running to Quest 3 takes 15-30 minutes each time, making iteration very slow.

## Quick Testing Methods

### 1. Use Unity Play Mode (Fastest - No Build Needed) ⚡

**Best for**: Testing UI, WebView display, logic changes

**Steps**:
1. Open your scene in Unity
2. Press **Play** button (▶️)
3. Test in editor (use VR simulator or desktop mode)
4. **No build needed!**

**Limitations**:
- WebView won't render in editor (shows mock view)
- XR interactions may not work perfectly
- Some Quest-specific features won't work

**When to use**: Testing code logic, UI layout, data flow

---

### 2. Use ADB Install Only (Skip Build Step)

**Best for**: When you've already built once and only changed code/assets

**Steps**:
1. **Build once** (full build - 15-30 min)
2. For subsequent changes:
   - Make your code/asset changes
   - **Build again** (with incremental IL2CPP: 2-5 min)
   - **OR** just install existing APK if no code changed:
     ```bash
     adb install -r path/to/your.apk
     adb shell am start -n com.DefaultCompany.VRTemplate/com.unity3d.player.UnityPlayerActivity
     ```

**Time saved**: 10-20 minutes per iteration

---

### 3. Use Oculus Link / Air Link (Test in Editor with Headset)

**Best for**: Testing VR interactions without building

**Setup**:
1. Connect Quest 3 to PC via USB (Link) or WiFi (Air Link)
2. Enable Link/Air Link on Quest 3
3. In Unity, use **Play Mode** with headset connected
4. Unity will render to your headset!

**Limitations**:
- Requires Link/Air Link setup
- Some performance differences vs. native build
- WebView may still not work perfectly

**When to use**: Testing VR interactions, hand tracking, UI positioning

---

### 4. Use Incremental Builds (Already Documented)

**Best for**: Code changes only

**Steps**:
1. Enable **Incremental IL2CPP Builds** (see `QUICK_BUILD_SPEEDUP.md`)
2. First build: Still slow (15-30 min)
3. Subsequent builds: Fast (2-5 min)

**Time saved**: 10-25 minutes per build

---

### 5. Use Unity Cloud Build (Alternative)

**Best for**: Automated builds, team collaboration

**Setup**:
1. Set up Unity Cloud Build (if available)
2. Connect to your Git repository
3. Builds happen on Unity's servers (much faster)
4. Download APK when ready

**Limitations**:
- Requires Unity Cloud Build subscription
- Still need to install on device

**When to use**: Automated builds, CI/CD pipeline

---

### 6. Use Development Build with Profiler (Selective)

**Best for**: When you need debugging but want faster builds

**Steps**:
1. **Disable** Development Build (faster build)
2. Build normally
3. If you need debugging:
   - Enable Development Build
   - Build again (only when needed)

**Time saved**: 2-3 minutes per build when not debugging

---

### 7. Test WebView Changes Separately (No Unity Build)

**Best for**: HTML/CSS/JavaScript changes to timeline editor

**Steps**:
1. Make changes to `WebContent/timeline-editor.html`
2. Test in browser:
   ```bash
   cd WebContent
   python3 -m http.server 8000
   # Open http://localhost:8000/timeline-editor.html in browser
   ```
3. Only rebuild Unity when HTML is finalized

**Time saved**: Test web UI without any Unity build!

**Note**: Copy to `StreamingAssets/WebContent/` before final Unity build

---

### 8. Use APK Analyzer (Check Build Without Installing)

**Best for**: Verifying build size, checking assets

**Steps**:
1. Build APK
2. Use Android Studio's APK Analyzer or:
   ```bash
   aapt dump badging your.apk
   ```
3. Check if build is correct before installing

**Time saved**: Catch issues before deployment

---

## Recommended Workflow

### For Code Changes:
1. **Test in Play Mode** first (fastest)
2. **Build with Incremental IL2CPP** (2-5 min)
3. **Install via ADB** (30 seconds)

### For WebView/UI Changes:
1. **Test in browser** (`python3 -m http.server`)
2. **Copy to StreamingAssets** when ready
3. **Build Unity** (only when needed)

### For VR Interaction Changes:
1. **Use Oculus Link + Play Mode** (if available)
2. **OR** build with Incremental IL2CPP (2-5 min)
3. **Install and test on device**

## Time Comparison

| Method | Time | Use Case |
|-------|------|----------|
| Full Build + Run | 15-30 min | First build, major changes |
| Incremental Build + Run | 2-5 min | Code changes only |
| Play Mode | 0 min | Logic/UI testing |
| ADB Install Only | 30 sec | Redeploy existing APK |
| Browser Test (WebView) | 0 min | HTML/CSS/JS changes |
| Oculus Link + Play | 0 min | VR interaction testing |

## Pro Tips

1. **Keep a "test" APK** - Build once, reuse for quick tests
2. **Use Git branches** - Test risky changes in separate branch
3. **Test WebView separately** - Don't rebuild Unity for every HTML change
4. **Enable Incremental Builds** - Biggest time saver
5. **Close other apps** - Faster builds when Mac isn't busy
6. **Use USB 3.0** - Faster ADB deployment

## Troubleshooting

### "ADB install fails"
- Check USB connection
- Enable USB debugging on Quest 3
- Try `adb kill-server && adb start-server`

### "Play Mode doesn't work"
- Check XR settings in Project Settings
- Some features require actual build

### "Incremental builds still slow"
- First build after enabling is always slow (builds cache)
- Subsequent builds should be fast
- Check Unity Console for errors

