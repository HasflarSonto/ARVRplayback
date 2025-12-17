# Faster Unity Builds to Quest 3 from Mac

## Current Build Configuration

- **Scripting Backend**: IL2CPP (required for Quest 3)
- **Target Architecture**: ARM64 (AndroidTargetArchitectures: 2)
- **Platform**: Android → Quest 3

## Optimization Strategies

### 1. Enable Incremental IL2CPP Builds ⚡ (BIGGEST IMPACT)

**What it does**: Only recompiles changed code instead of rebuilding everything.

**How to enable**:
1. Open **Edit → Project Settings → Player**
2. Select **Android** platform tab
3. Expand **Other Settings**
4. Under **Configuration**, find **Incremental IL2CPP Build**
5. **Enable** it

**Impact**: Can reduce build times from 10+ minutes to 2-3 minutes for code-only changes.

**Note**: First build will still be slow (full compile), but subsequent builds will be much faster.

### 2. Use Development Builds Only When Needed

**Development Builds** are slower because they include:
- Debug symbols
- Profiler support
- Development-only code paths

**For faster iteration**:
- Use **Release Builds** for testing (faster build, faster runtime)
- Only use **Development Builds** when you need:
  - Profiler data
  - Detailed stack traces
  - Debug logging

**How to toggle**: In Build Settings, uncheck "Development Build" when not needed.

### 3. Optimize IL2CPP Compiler Configuration

**Settings Location**: Edit → Project Settings → Player → Android → Other Settings → Configuration

**Optimizations**:
- **IL2CPP Compiler Configuration**: Set to **Master** (fastest builds, but less debugging info)
  - For development: Use **Debug** or **Release**
  - For fastest builds: Use **Master**
- **IL2CPP Code Generation**: Set to **Faster runtime** (faster builds)
- **IL2CPP Stacktrace Information**: Set to **Disabled** (faster builds, less debugging)

### 4. Reduce Build Size (Faster Deployment)

**APK Size Reduction**:
- **Android Minify**: Enable for Release builds
  - Location: Player Settings → Android → Minify
  - Set **Release** to **Proguard** or **R8**
- **Strip Engine Code**: Enable managed code stripping
  - Location: Player Settings → Android → Other Settings → Managed Stripping Level
  - Set to **Low** or **Medium** (High can break some code)

**Impact**: Smaller APKs deploy faster over ADB.

### 5. Use Build and Run vs. Build Only

**Build and Run** (default):
- Builds APK
- Installs via ADB
- Launches app
- **Slower** because it does everything

**Build Only** (faster for iteration):
- Builds APK only
- You manually install/launch when ready
- **Faster** because it skips deployment

**How to use**:
- In Build Settings, click **Build** instead of **Build and Run**
- Or use command line: `Unity -buildTarget Android -buildPath path/to/build.apk`

### 6. Optimize Asset Processing

**Reduce Asset Import Time**:
- **Disable Auto Refresh**: Edit → Preferences → Asset Pipeline → Auto Refresh → Disabled
  - Manually refresh with `Ctrl+R` (Cmd+R on Mac) when needed
- **Exclude Build Folders**: Add build output folders to antivirus exclusions
- **Use Asset Database Cache**: Unity caches asset data - don't clear it unnecessarily

### 7. Use Unity Cloud Build (Alternative)

**For very slow Mac builds**:
- Consider Unity Cloud Build (if available)
- Builds happen on Unity's servers (much faster)
- Can trigger builds from Git commits
- Free tier available for small projects

### 8. Hardware Optimizations

**Mac-Specific**:
- **Close other apps** during builds (especially Xcode, Chrome, etc.)
- **Use SSD** for project and build folders (not external HDD)
- **Increase RAM** if possible (IL2CPP compilation is memory-intensive)
- **Use faster Mac** if available (M1/M2/M3 Macs are much faster than Intel)

### 9. Skip Unnecessary Build Steps

**For Development Iteration**:
- **Don't rebuild** if only changing scripts (use IL2CPP incremental builds)
- **Don't rebuild** if only changing WebContent (it's in StreamingAssets, loaded at runtime)
- **Use Unity's Play Mode** for testing UI/WebView changes when possible

### 10. Optimize Project Structure

**Reduce Build Time**:
- **Remove unused assets** from project
- **Compress textures** (use ASTC for Quest 3)
- **Simplify scenes** (fewer GameObjects = faster builds)
- **Use Addressables** for large assets (loads at runtime, not build time)

## Quick Win Checklist

For immediate speed improvements:

- [ ] **Enable Incremental IL2CPP Builds** (biggest impact)
- [ ] **Disable Development Build** when not debugging
- [ ] **Use Build Only** instead of Build and Run
- [ ] **Close other applications** during builds
- [ ] **Exclude build folders** from antivirus scans
- [ ] **Disable Auto Refresh** in Unity preferences

## Expected Build Times

**First Build** (full compile):
- Without optimizations: 15-30 minutes
- With optimizations: 10-20 minutes

**Incremental Builds** (code changes only):
- Without incremental IL2CPP: 10-15 minutes
- With incremental IL2CPP: 2-5 minutes

**Asset-Only Changes** (no code changes):
- Should be very fast (1-2 minutes) if assets are already imported

## Troubleshooting Slow Builds

### If builds are still slow after optimizations:

1. **Check Unity Console** for warnings/errors during build
2. **Check IL2CPP compilation** - this is usually the slowest part
3. **Check ADB deployment** - slow network/USB can slow deployment
4. **Check Mac Activity Monitor** - see what's using CPU/RAM
5. **Try building to file** instead of directly to device (faster)

### Common Issues:

- **IL2CPP compilation** taking 10+ minutes → Normal for first build, should be faster with incremental
- **Asset import** taking long → Check for large uncompressed textures/models
- **ADB deployment** taking long → Use USB 3.0 cable, check USB connection speed
- **Mac running hot/slow** → Close other apps, check thermal throttling

## Advanced: Command Line Builds

For even faster iteration, you can use command line builds:

```bash
# Build only (no deployment)
/Applications/Unity/Hub/Editor/[VERSION]/Unity.app/Contents/MacOS/Unity \
  -quit -batchmode -projectPath /path/to/project \
  -buildTarget Android \
  -buildPath /path/to/build.apk

# Then deploy manually when ready
adb install -r /path/to/build.apk
adb shell am start -n com.DefaultCompany.VRTemplate/com.unity3d.player.UnityPlayerActivity
```

## References

- Unity IL2CPP Documentation: https://docs.unity3d.com/Manual/IL2CPP.html
- Unity Android Build Optimization: https://docs.unity3d.com/Manual/android-BuildProcess.html
- Incremental IL2CPP Builds: https://docs.unity3d.com/Manual/IL2CPP-Incremental.html

