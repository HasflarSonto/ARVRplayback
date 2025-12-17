# Quick Build Speedup Guide - Quest 3 from Mac

## 🚀 Top 3 Quick Wins (Do These First!)

### 1. Enable Incremental IL2CPP Builds (BIGGEST IMPACT - 5x faster!)

**Steps**:
1. Open Unity
2. Go to **Edit → Project Settings → Player**
3. Select **Android** tab (top)
4. Scroll down to **Other Settings**
5. Expand **Configuration** section
6. Find **"Incremental IL2CPP Build"**
7. **CHECK THE BOX** ✅

**Result**: 
- First build: Still slow (full compile)
- Subsequent builds: **2-5 minutes instead of 15-30 minutes** for code changes

### 2. Use "Build" Instead of "Build and Run"

**Steps**:
1. Open **File → Build Settings**
2. Click **"Build"** button (NOT "Build and Run")
3. This skips ADB installation/launch (saves 1-2 minutes)

**Then manually deploy when ready**:
```bash
adb install -r path/to/your.apk
adb shell am start -n com.DefaultCompany.VRTemplate/com.unity3d.player.UnityPlayerActivity
```

### 3. Disable Development Build When Not Debugging

**Steps**:
1. Open **File → Build Settings**
2. **UNCHECK** "Development Build" (unless you need profiler/debugging)
3. This removes debug symbols and profiler code (faster build + faster runtime)

## Additional Quick Optimizations

### 4. Disable Auto Refresh (Saves Editor Time)

**Steps**:
1. **Edit → Preferences** (Mac: Unity → Preferences)
2. Go to **Asset Pipeline**
3. Set **Auto Refresh** to **Disabled**
4. Manually refresh with **Cmd+R** when needed

### 5. Close Other Apps During Builds

IL2CPP compilation is CPU/RAM intensive. Close:
- Chrome/Browser
- Xcode (if open)
- Other Unity instances
- Heavy applications

### 6. Exclude Build Folders from Antivirus

If you have antivirus software:
- Add your Unity project folder to exclusions
- Add your build output folder to exclusions
- This prevents file scanning during compilation

## Expected Time Savings

| Optimization | Time Saved |
|-------------|------------|
| Incremental IL2CPP Builds | **10-15 minutes** per build |
| Build Only (vs Build and Run) | **1-2 minutes** per build |
| Disable Development Build | **2-3 minutes** per build |
| **Total Potential Savings** | **13-20 minutes** per build |

## After Enabling Incremental Builds

**First build after enabling**: Still slow (needs to build cache)
**All subsequent builds**: Much faster (only compiles changed code)

## Troubleshooting

**If incremental builds don't work**:
- Make sure you're building to the same location
- Don't delete the `Temp` or `Library` folders
- Check Unity Console for IL2CPP errors

**If builds are still slow**:
- Check Mac Activity Monitor - is something else using CPU?
- Try building to file first, then deploying separately
- Consider using a faster Mac if available (M1/M2/M3 are much faster)

## Pro Tip: Use ADB Install Only

For fastest iteration when only code changes:
1. Build once with incremental IL2CPP
2. For subsequent code changes, just rebuild (fast with incremental)
3. Use `adb install -r` to quickly redeploy without full build process

