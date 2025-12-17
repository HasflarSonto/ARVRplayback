# WebView White Screen Fix

## Problem
After recent changes, the WebView shows a white screen instead of the timeline editor.

## Root Cause
The WebView instance (`actualWebView`) was null or not properly initialized before calling `LoadUrl()` or `LoadHtml()`.

## Fix Applied

### 1. Proper WebView Instance Access
- **Before**: Using `webViewComponent` (the prefab) directly for LoadUrl/LoadHtml
- **After**: Getting the `WebView` property from `CanvasWebViewPrefab` and using that instance

### 2. Null Check Added
- Added check to ensure `actualWebView` is not null before attempting to load content
- Shows diagnostic message if WebView instance is null

### 3. Correct Method Target
- **LoadUrl/LoadHtml** must be called on the `WebView` property (IWebView instance)
- **NOT** on the `CanvasWebViewPrefab` prefab itself

## How Vuplex CanvasWebViewPrefab Works

```
CanvasWebViewPrefab (prefab component)
  ├── WaitUntilInitialized() - Wait for this first
  ├── WebView (property) - The actual IWebView instance
  │   ├── LoadUrl(string) - Use this to load URLs
  │   ├── LoadHtml(string) - Use this to load HTML directly
  │   └── PostMessage(string) - Use this to send messages to JavaScript
  └── MessageEmitted (event) - Listen to this for messages FROM JavaScript
```

## Testing Steps

1. **Check Unity Console** for these messages:
   - `[WebViewManager] WebView initialized!`
   - `[WebViewManager] Got WebView property, type: ...`
   - `[WebViewManager] ✅ LoadUrl called successfully`

2. **If you see "WebView instance is null"**:
   - The WebView hasn't finished initializing
   - Check that `WaitUntilInitialized()` completed successfully
   - Increase the delay before marking as ready

3. **If you see "LoadUrl called successfully" but still white screen**:
   - Check that the HTML file exists in `StreamingAssets/WebContent/timeline-editor.html`
   - Check that the URL is correct (`streaming-assets://WebContent/timeline-editor.html`)
   - Try the HTML fallback (should happen automatically if LoadUrl fails)

## Reference: Vuplex Sample

For the correct implementation, reference:
- **Repository**: `xr-interaction-webview-example` (since you're using XR Interaction Toolkit)
- **Key File**: Look at how they initialize and load content in their demo scripts

## Next Steps if Still Not Working

1. **Pull the Vuplex sample**:
   ```bash
   git clone git@github.com:vuplex/xr-interaction-webview-example.git
   ```

2. **Compare initialization**:
   - Check how they wait for initialization
   - Check how they get the WebView property
   - Check how they call LoadUrl/LoadHtml

3. **Check diagnostic messages**:
   - The on-screen diagnostic text should show what's happening
   - Look for error messages in red

4. **Verify HTML file**:
   - Ensure `Assets/StreamingAssets/WebContent/timeline-editor.html` exists
   - Check file size (should be > 0 bytes)
   - Try opening it in a browser to verify it's valid HTML

