# WebView Interaction Fix - Can't Click Buttons

## Problem
The WebView displays correctly, but buttons and interactive elements cannot be clicked in VR.

## Root Cause
According to the Vuplex XR Interaction Toolkit example, the Canvas needs:
1. **TrackedDeviceGraphicRaycaster** (not just GraphicRaycaster) for XR Interaction Toolkit
2. **Event Camera** set to the scene's main camera
3. **XRUIInputModule** in the scene (should already be present)

## Fix Applied

### 1. Added TrackedDeviceGraphicRaycaster
- **Before**: Only `GraphicRaycaster` was added
- **After**: Checks for XR Interaction Toolkit and adds `TrackedDeviceGraphicRaycaster` instead
- **Location**: Both `PositionWebViewToMatchQuad()` and `TryCreateWebViewComponent()` methods

### 2. Set Event Camera
- **Before**: Event Camera might not be set correctly
- **After**: Automatically sets Canvas `worldCamera` to the main camera
- **Required for**: Screen Space Camera and World Space render modes

## Key Requirements from Vuplex Example

From `xr-interaction-webview-example` README:

> **Troubleshooting**: If your CanvasWebViewPrefab and CanvasKeyboard aren't responding to clicks or scrolling, then that indicates that your scene is not configured correctly. I recommend using this project as a reference and verifying the following settings in your scene:
> 
> - The canvas must have a **TrackedDeviceGraphicRaycaster** attached to it.
> - The canvas's **Event Camera** must be set to the scene's main camera.

## Code Changes

### In `PositionWebViewToMatchQuad()`:
- Added check for `TrackedDeviceGraphicRaycaster` type
- Adds `TrackedDeviceGraphicRaycaster` if XR Interaction Toolkit is available
- Falls back to `GraphicRaycaster` if XR Interaction Toolkit not found
- Sets Event Camera to main camera

### In `TryCreateWebViewComponent()`:
- Same changes as above
- Ensures newly created WebView prefabs have correct setup

## Verification Steps

1. **Check Unity Console** for these messages:
   - `✅ Added TrackedDeviceGraphicRaycaster to WebView Canvas (required for XR Interaction Toolkit)`
   - `✅ Set WebView Canvas Event Camera to: [Camera Name]`

2. **In Unity Inspector**:
   - Select the Canvas that contains the WebView
   - Check that it has **TrackedDeviceGraphicRaycaster** component (not just GraphicRaycaster)
   - If using Screen Space Camera or World Space mode, check that **Event Camera** is set to your main camera

3. **Test in VR**:
   - Build to Quest 3
   - Try clicking buttons in the WebView
   - Should now work!

## If Still Not Working

1. **Check EventSystem**:
   - Make sure there's an EventSystem in the scene
   - Make sure it has **XRUIInputModule** (not StandaloneInputModule)

2. **Check Canvas Render Mode**:
   - For VR, typically use **Screen Space - Camera** or **World Space**
   - Make sure Event Camera is set correctly

3. **Check Canvas Parent**:
   - Canvas should NOT be a child of XR Origin
   - Canvas should be at root level or under a static GameObject

4. **Reference the Example**:
   - Open `xr-interaction-webview-example` project
   - Check how their Canvas is configured
   - Compare with your setup

## References

- Vuplex XR Interaction Toolkit Example: `xr-interaction-webview-example/`
- Vuplex Documentation: https://support.vuplex.com/articles/clicking
- XR Interaction Toolkit Docs: https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/api/UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster.html

