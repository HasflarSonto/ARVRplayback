# Canvas Setup Explained - VR/XR Requirements

## 1. "Canvas is not a child of XR Origin"

### What does this mean?

In Unity's Hierarchy (the scene tree), your Canvas GameObject should **NOT** be inside the "XR Origin" GameObject.

### Visual Example:

**❌ BAD (Canvas inside XR Origin):**
```
Scene
  └── XR Origin
      ├── Camera Offset
      │   └── Main Camera
      ├── Left Controller
      ├── Right Controller
      └── Canvas  ← BAD! Canvas is a child of XR Origin
          └── EditModePanel
              └── WebView
```

**✅ GOOD (Canvas at root level):**
```
Scene
  ├── XR Origin
  │   ├── Camera Offset
  │   │   └── Main Camera
  │   ├── Left Controller
  │   └── Right Controller
  └── Canvas  ← GOOD! Canvas is at root level
      └── EditModePanel
          └── WebView
```

### Why is this important?

- **XR Origin moves** with the player's headset/controllers
- If Canvas is a child of XR Origin, it will **move with the player**
- This causes the UI to "follow" the player around, which is usually not what you want
- For VR UI, you typically want the Canvas to stay in **world space** or **screen space**, not attached to the player

### How to check:

1. **Open Unity Hierarchy** (left panel)
2. **Find your Canvas** (the one with EditModePanel/WebView)
3. **Look at its parent** (the GameObject it's nested under)
4. **If parent is "XR Origin" or "Rig"**: ❌ Bad - move it out
5. **If Canvas is at root level** (no parent, or parent is "Scene"): ✅ Good

### How to fix:

1. **Select the Canvas** in Hierarchy
2. **Drag it** out of XR Origin to root level
3. **Or** use `EditModePanelFixer` component (right-click → "Fix Panel Setup")

---

## 2. "Canvas Render Mode is set correctly"

### What is Canvas Render Mode?

This is a setting on the Canvas component that determines **how the Canvas is rendered** in 3D space.

### The Three Render Modes:

#### 1. **Screen Space - Overlay** (Simplest)
- Canvas is **always in front** of everything
- Doesn't need a camera
- **Best for**: Simple UI that should always be visible
- **Works in VR**: ✅ Yes, but may have issues with depth

#### 2. **Screen Space - Camera** (Recommended for VR)
- Canvas is rendered **through a specific camera**
- Canvas appears at a fixed distance from the camera
- **Best for**: VR UI that should stay with the player's view
- **Requires**: Event Camera must be set to your main camera
- **Works in VR**: ✅ Yes, this is what you want!

#### 3. **World Space** (For 3D UI)
- Canvas exists as a **3D object in the world**
- You can walk around it, it has a position in 3D space
- **Best for**: UI panels that are part of the 3D environment
- **Requires**: Event Camera must be set
- **Works in VR**: ✅ Yes, but more complex

### How to check:

1. **Select your Canvas** in Hierarchy
2. **In Inspector**, find the **Canvas** component
3. **Look for "Render Mode"** dropdown
4. **Current setting**: Should be "Screen Space - Camera" or "World Space" for VR

### Visual Guide:

```
Canvas Component (Inspector)
├── Render Mode: [Screen Space - Camera ▼]  ← Check this!
├── Pixel Perfect: ☐
├── Sort Order: 0
├── Target Display: Display 1
└── Additional Shader Channels: ...
```

### For VR/Quest 3, use:

**✅ Screen Space - Camera** (Recommended)
- Set **Event Camera** to your Main Camera
- Canvas will follow the player's view
- Works well with XR Interaction Toolkit

**✅ World Space** (Alternative)
- Canvas is a 3D object in the world
- Can position it anywhere
- Also needs Event Camera set

**⚠️ Screen Space - Overlay** (Not recommended for VR)
- May have interaction issues
- Doesn't work well with XR Interaction Toolkit

### How to set it:

1. **Select Canvas** in Hierarchy
2. **In Inspector**, find **Canvas** component
3. **Click "Render Mode"** dropdown
4. **Select "Screen Space - Camera"**
5. **Set "Render Camera"** (or "Event Camera") to your **Main Camera**
   - Drag Main Camera from Hierarchy into the "Render Camera" field

---

## Complete Checklist for VR Canvas Setup

### ✅ Canvas Hierarchy:
- [ ] Canvas is **NOT** a child of XR Origin
- [ ] Canvas is at **root level** or under a static GameObject
- [ ] Canvas has **EditModePanel** (or your UI) as a child

### ✅ Canvas Component Settings:
- [ ] **Render Mode**: "Screen Space - Camera" or "World Space"
- [ ] **Event Camera** (or Render Camera): Set to Main Camera
- [ ] **Sort Order**: Set appropriately (0 for base UI, higher for modals)

### ✅ Canvas Components:
- [ ] Has **TrackedDeviceGraphicRaycaster** component (for XR Interaction Toolkit)
- [ ] Has **CanvasScaler** component (for UI scaling)
- [ ] Has **GraphicRaycaster** component (fallback, or if not using XR)

### ✅ Event System:
- [ ] Scene has **EventSystem** GameObject
- [ ] EventSystem has **XRUIInputModule** component (not StandaloneInputModule)
- [ ] Only **ONE** EventSystem in the scene

---

## Common Issues

### Issue: Canvas moves with player
**Cause**: Canvas is a child of XR Origin
**Fix**: Move Canvas to root level

### Issue: Can't click buttons in WebView
**Cause**: Missing TrackedDeviceGraphicRaycaster or wrong Event Camera
**Fix**: Add TrackedDeviceGraphicRaycaster, set Event Camera to Main Camera

### Issue: Canvas is invisible
**Cause**: Wrong Render Mode or missing Event Camera
**Fix**: Set Render Mode to "Screen Space - Camera", set Event Camera

### Issue: Canvas is too small/large
**Cause**: CanvasScaler settings
**Fix**: Adjust CanvasScaler "Scale Factor" or "Reference Resolution"

---

## Quick Reference

| Setting | Value for VR |
|---------|--------------|
| **Canvas Parent** | Root level (not XR Origin) |
| **Render Mode** | Screen Space - Camera |
| **Event Camera** | Main Camera |
| **GraphicRaycaster** | TrackedDeviceGraphicRaycaster (for XR) |
| **Sort Order** | 0 (or higher for modals) |

---

## Still Having Issues?

1. **Check the Vuplex example**: Open `xr-interaction-webview-example` project
2. **Compare Canvas settings**: See how their Canvas is configured
3. **Check Unity Console**: Look for errors about Canvas, EventSystem, or Raycaster
4. **Use EditModePanelFixer**: Right-click on EditModePanel → "Fix Panel Setup"

