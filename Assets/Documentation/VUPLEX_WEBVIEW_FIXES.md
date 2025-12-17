# Vuplex WebView Integration Fixes

## Project Context

**XR System**: XR Interaction Toolkit (primary) with Meta Quest support
**WebView**: Vuplex CanvasWebViewPrefab (for UI-based WebViews in VR)
**Target Platform**: Meta Quest 3

## Key Fixes Applied

### 1. Canvas Sorting Order & Interaction (Issue 3 - Button Clicks)

**Problem**: WebView Canvas had `sortingOrder = -1`, placing it behind other UI elements and blocking pointer events.

**Fix**:
- Changed `sortingOrder` from `-1` to `1` (above slider, below modals)
- Added `GraphicRaycaster` component check and auto-enable
- Applied in both `PositionWebViewToMatchQuad()` and `TryCreateWebViewComponent()`

**Files Modified**:
- `WebViewManager.cs` - Lines 329-338, 485-495

### 2. WebView Initialization & Message Queue (Issue 1 - Data Loading)

**Problem**: Messages were being sent before WebView was fully initialized, causing them to be lost.

**Fix**:
- Added `isWebViewReady` flag to track initialization state
- Added `pendingMessages` queue to store messages sent before ready
- Added `MarkWebViewReady()`, `ProcessPendingMessages()`, and `SendMessageToWebViewInternal()` methods
- Fixed initialization order: `WaitUntilInitialized` → Get WebView property → Load URL → Set up MessageEmitted → Mark ready
- Added retry logic with `RetryWebViewInitialization()`

**Files Modified**:
- `WebViewManager.cs` - Added message queue system and proper initialization flow

### 3. Message Format & Serialization

**Problem**: Messages were using string concatenation which could break with special characters.

**Fix**:
- Created `SerializableMessage` class for proper JSON serialization
- Changed from string concatenation to `JsonUtility.ToJson()`
- Improved error handling in timeline editor message parsing

**Files Modified**:
- `WebViewManager.cs` - Added SerializableMessage class, updated DisplayJSON()
- `timeline-editor.html` - Improved message parsing with better error handling

### 4. Edit Mode Integration

**Problem**: JSON was generated when recording stopped, but edit mode might not be active yet.

**Fix**:
- Moved `GenerateAndDisplayJSON()` call to edit mode activation
- Added 0.5s delay to ensure WebView is ready before sending data

**Files Modified**:
- `SimpleInteractionUIController.cs` - Added GenerateAndDisplayJSON call in OnEditButtonClicked()

## Vuplex-Specific Implementation Details

### CanvasWebViewPrefab Structure

1. **CanvasWebViewPrefab** (the prefab component)
   - Has `MessageEmitted` event (for receiving messages FROM JavaScript)
   - Has `WebView` property (the actual WebView instance)
   - Has `WaitUntilInitialized()` method (must wait before accessing WebView)

2. **WebView** (the actual webview instance, accessed via `.WebView` property)
   - Has `PostMessage(string)` method (for sending messages TO JavaScript)
   - Has `LoadUrl(string)` and `LoadHtml(string)` methods

### Correct Initialization Flow

```
1. Wait for CanvasWebViewPrefab.WaitUntilInitialized()
2. Get CanvasWebViewPrefab.WebView property
3. Load URL/HTML using WebView.LoadUrl() or LoadHtml()
4. Set up MessageEmitted event handler on CanvasWebViewPrefab
5. Mark as ready and process pending messages
```

### Message Handling

- **Unity → JavaScript**: Use `WebView.PostMessage(jsonString)`
- **JavaScript → Unity**: Listen to `CanvasWebViewPrefab.MessageEmitted` event
- **Message Format**: JSON string (properly serialized, not concatenated)

## Remaining Potential Issues

### 1. WebView Not Loading in Editor

**Issue**: Mock WebView in editor doesn't support `streaming-assets://` URLs
**Current Fix**: Falls back to `LoadHtml()` in editor
**Note**: This is expected behavior - WebView will only work fully on device

### 2. Message Timing

**Issue**: JavaScript message listener might not be ready when Unity sends first message
**Current Fix**: Added 1-2 second delay before marking WebView as ready
**Potential Improvement**: JavaScript could send a "ready" message to Unity

### 3. Canvas Setup

**Issue**: CanvasWebViewPrefab must be on a Canvas GameObject
**Current Fix**: Auto-creates Canvas if needed, checks for Canvas in parent
**Verification Needed**: Ensure Canvas is properly configured in scene

### 4. Pointer Events in VR

**Issue**: VR controllers might not trigger standard pointer events
**Current Fix**: Added GraphicRaycaster (should work with XR Interaction Toolkit)
**Potential Issue**: May need XR-specific raycasting setup

## Testing Checklist

- [ ] WebView loads in VR (Quest 3)
- [ ] Buttons are clickable with VR controllers
- [ ] Timeline editor receives data when edit mode activates
- [ ] Timeline editor receives data when recording stops
- [ ] Unity slider updates timeline playhead
- [ ] Messages are sent and received correctly
- [ ] No console errors in Unity or browser console

## Debugging Tips

1. **Check Unity Console** for WebViewManager logs:
   - Look for "WebView initialized!"
   - Look for "Message sent to WebView"
   - Look for "MessageEmitted event handler set up"

2. **Check Browser Console** (if accessible):
   - Look for "Received message from Unity"
   - Look for "Task data loaded from Unity"
   - Look for any JSON parsing errors

3. **Verify Canvas Setup**:
   - CanvasWebViewPrefab must be a child of a Canvas
   - Canvas should have GraphicRaycaster component
   - Canvas sortingOrder should be appropriate (1 for WebView)

4. **Verify WebView Initialization**:
   - WaitUntilInitialized() should complete
   - WebView property should not be null
   - URL should load successfully

## References

- Vuplex XR Interaction Example: `git@github.com:vuplex/xr-interaction-webview-example.git`
- Vuplex Meta XR Example: `git@github.com:vuplex/meta-xr-webview-example.git`
- Vuplex Documentation: https://developer.vuplex.com/webview/overview

