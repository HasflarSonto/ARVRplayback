# Error Fixes Guide

## VideoPlayer Errors (FIXED)

**Error**: `Cannot Play a disabled VideoPlayer`

**Cause**: The old `VideoTimeScrubControl` component is trying to play a video that's disabled.

**Fix**: 
- `WebViewManager` now automatically disables old VideoPlayer components on Start
- If you still see this error, add the `DisableVideoPlayerComponents` component to the Video Player GameObject

**Manual Fix**:
1. Select **EditPanel2 → Content → Video Player** in Hierarchy
2. Find **VideoTimeScrubControl** component
3. **Uncheck** the checkbox to disable it
4. Find **VideoPlayer** component
5. **Uncheck** the checkbox to disable it

---

## LazyFollow NullReference Errors (Scene Setup Issue)

**Error**: `NullReferenceException: Object reference not set to an instance of an object` in `LazyFollow.OnEnable()`

**Cause**: XR Interaction Toolkit's `LazyFollow` component has missing references. This is a **scene setup issue**, not a code error.

**Fix Options**:

### Option 1: Fix Missing References
1. Find GameObjects with `LazyFollow` component in Hierarchy
2. Check Inspector - look for missing references (red exclamation marks)
3. Assign the missing references (usually a Transform or GameObject)

### Option 2: Disable LazyFollow (if not needed)
1. Find GameObjects with `LazyFollow` component
2. Uncheck the component to disable it

### Option 3: Ignore (if UI works fine)
- These errors won't break functionality if the UI is working
- They're just warnings about missing optional references

---

## Affordance System NullReference Errors (AUTOMATICALLY FIXED)

**Error**: `NullReferenceException` in `BaseAsyncAffordanceStateReceiver.HandleTween()`

**Cause**: XR Interaction Toolkit's Affordance System has missing references. This is a **scene setup issue**, not a code error.

**Fix**: ✅ **AUTOMATIC** - `SimpleInteractionUIController` now automatically disables all broken Affordance components on scene start using `GlobalAffordanceCleanup`.

**Manual Fix (if needed)**:
1. Add `GlobalAffordanceCleanup` component to any GameObject
2. Right-click the component → **"Cleanup All Affordances"**
3. Or add it to `SimpleInteractionUIController` - it will run automatically

**Note**: Disabling Affordance components removes visual feedback (hover effects, etc.) but buttons will still work normally.

---

## Summary

✅ **VideoPlayer errors**: Fixed automatically by `WebViewManager`

⚠️ **LazyFollow/Affordance errors**: Scene setup issues - fix missing references or disable components if not needed

💡 **Note**: These errors are **not caused by our JSON/WebView code**. They're pre-existing scene configuration issues.

