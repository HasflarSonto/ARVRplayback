# Edit Mode Troubleshooting Guide

## Panel "Running Away" / Moving When Clicked

**If the EditModePanel moves or "runs away" when you click on it, this is the fix:**

### Quick Fix: Add PanelPositionLock Component

1. **Select EditModePanel** in the Hierarchy
2. **Add Component** → Search for `PanelPositionLock`
3. **The component will automatically:**
   - Lock the panel's position
   - Ensure the Canvas is not a child of XR Origin
   - Change World Space Canvas to Screen Space Overlay (if needed)
   - Prevent the panel from being reparented

**That's it!** The panel should now stay completely still when clicked.

### Alternative: Use UIPanelAnchor

1. **Select EditModePanel** or its **Canvas**
2. **Add Component** → Search for `UIPanelAnchor`
3. **In Inspector**, ensure:
   - ✅ **Anchor To Current User**: Enabled
   - ✅ **Lock Position**: Enabled

### Manual Fix: Check Canvas Parent

If the above doesn't work, manually check:

1. **Select the Canvas** that contains EditModePanel
2. **Check its parent** in the Hierarchy:
   - ❌ **BAD**: Canvas is a child of "XR Origin", "Rig", "Camera", or "Player"
   - ✅ **GOOD**: Canvas is at root level or child of a static GameObject
3. **If Canvas is under XR Origin:**
   - Drag the Canvas out of XR Origin to root level
   - Or use `EditModePanelFixer` component (right-click → "Fix Panel Setup")

### Check Canvas Render Mode

1. **Select the Canvas**
2. **In Inspector**, check **Render Mode**:
   - ✅ **Best**: "Screen Space - Overlay" (stays with current user)
   - ⚠️ **OK**: "Screen Space - Camera" (if using static camera)
   - ❌ **Problem**: "World Space" (can move with recorded player)

---

## Panel Glitching/Hiding Issues

If the EditModePanel is glitching, flickering, or hiding unexpectedly, try these solutions:

### Solution 1: Check Canvas Settings

1. **Select the Canvas** that contains EditModePanel
2. **In Inspector**, check:
   - **Render Mode**: Should be "Screen Space - Overlay" or "Screen Space - Camera"
   - **Pixel Perfect**: Can be enabled or disabled (try both)
   - **Sort Order**: Make sure it's not conflicting with other canvases

### Solution 2: Check Panel Hierarchy

1. **EditModePanel** should be a direct child of the **Canvas** (or a UI container)
2. Make sure it's not being parented/unparented by other scripts
3. Check that no other scripts are modifying the panel's active state

### Solution 3: Disable Layout Components

If the panel has Layout components (Layout Group, Content Size Fitter, etc.):

1. **Select EditModePanel**
2. **In Inspector**, temporarily disable:
   - **Layout Group** components
   - **Content Size Fitter** components
   - **Aspect Ratio Fitter** components
3. Test if the glitching stops
4. If it does, the layout component might be causing the issue

### Solution 4: Check RectTransform

1. **Select EditModePanel**
2. **In Inspector**, check **RectTransform**:
   - **Anchors**: Should be set properly (not moving)
   - **Pivot**: Should be (0.5, 0.5) for centered
   - **Position**: Should be stable
3. Make sure **RectTransform** isn't being modified by animations or other scripts

### Solution 5: Canvas Scaler Issues

1. **Select the Canvas**
2. **In Inspector**, find **Canvas Scaler** component
3. Try changing:
   - **UI Scale Mode**: Try "Constant Pixel Size" or "Scale With Screen Size"
   - **Reference Resolution**: Make sure it matches your target resolution

### Solution 6: VR-Specific Issues

If using VR (Quest 3):

1. **Check Canvas Render Mode**:
   - Should be "World Space" for VR UI
   - Or "Screen Space - Camera" with XR Camera assigned
2. **Check XR UI Input Module**:
   - Make sure EventSystem has XRUIInputModule
   - Not conflicting with StandaloneInputModule

### Solution 7: Multiple Event Systems

1. **Search for "EventSystem"** in Hierarchy
2. Make sure there's only **ONE** EventSystem
3. Multiple EventSystems can cause UI conflicts

### Solution 8: Check for Conflicting Scripts

1. Search for scripts that might be modifying UI panels
2. Check if any other UI controllers are active
3. Look for scripts that use `SetActive()` on UI elements

### Solution 9: Panel Position/Scale

If the panel is "hiding" (might be off-screen or scaled to zero):

1. **Select EditModePanel**
2. **In Scene view**, check if it's visible
3. **In Inspector**, check:
   - **RectTransform Position**: Should be on-screen
   - **RectTransform Scale**: Should be (1, 1, 1)
   - **Canvas Group Alpha**: Should be 1.0 (if using CanvasGroup)

### Solution 10: Force Panel State

If nothing else works, you can add a debug script to lock the panel state:

1. Create a simple script that keeps the panel active when in edit mode
2. Add it to EditModePanel
3. It should override any other SetActive calls

## Common Causes

- **Layout components** recalculating positions every frame
- **Canvas Scaler** changing panel size/position
- **Multiple UI systems** conflicting
- **VR camera** not properly assigned to Canvas
- **EventSystem** conflicts
- **Animations** or **tweening** on the panel

## Quick Fix

Try this first:
1. **Select EditModePanel**
2. **In Inspector**, disable any **Layout Group** components
3. **Set RectTransform** anchors to stretch (min: 0,0 max: 1,1)
4. **Set Position** to (0, 0, 0)
5. Test if glitching stops

If it works, the issue is likely a Layout component. You can then re-enable and configure it properly.

