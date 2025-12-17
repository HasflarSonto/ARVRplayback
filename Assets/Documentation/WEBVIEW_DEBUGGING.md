# WebView Debugging Guide

## How to Check if WebView is Loading HTML

### Step 1: Check Unity Console Logs

When the scene starts, look for these log messages:

```
[WebViewManager] Starting initialization...
[WebViewManager] Configured URL: http://localhost:8000/WebContent/index.html
[WebViewManager] WebView component found - initializing
[WebViewManager] WebView type: CanvasWebViewPrefab
[WebViewManager] Attempting to load URL: http://localhost:8000/WebContent/index.html
[WebViewManager] LoadUrl called successfully
```

**If you see "No WebView component found"**, it means:
- Vuplex WebView is not installed
- The WebView component is not on the GameObject
- The system is using the fallback TextMeshPro display

### Step 2: Test WebView from Inspector

1. **Select the GameObject** with `WebViewManager` component (usually "Video Player" in EditPanel2)
2. **In Inspector**, right-click on `WebViewManager` component
3. **Click "Test WebView Load"** from the context menu
4. **Check Console** for detailed debug info

### Step 3: Check if Local Server is Running

The default URL is `http://localhost:8000/WebContent/index.html`

**To start a local server:**

1. **Open Terminal**
2. **Navigate to project root:**
   ```bash
   cd /Users/antonioli/Desktop/Vrtest
   ```
3. **Start HTTP server:**
   ```bash
   python3 -m http.server 8000
   ```
   Or with Node.js:
   ```bash
   npx http-server -p 8000
   ```
4. **Test in browser:** Open `http://localhost:8000/WebContent/index.html`
   - You should see "Task Instructions" with "Ready" status

### Step 4: Check WebView URL Configuration

1. **Select GameObject** with `WebViewManager`
2. **In Inspector**, check **"Web View URL"** field
3. **Should be:** `http://localhost:8000/WebContent/index.html`
4. **If using file path** (for Quest 3), use:
   ```
   file:///path/to/WebContent/index.html
   ```

### Step 5: Verify HTML File Exists

Check that the file exists:
```
/Users/antonioli/Desktop/Vrtest/WebContent/index.html
```

### Step 6: Common Issues

#### Issue: Purple/Blank Screen
**Possible causes:**
- WebView not loading HTML (check logs)
- HTML has CSS errors (check browser console)
- WebView component not initialized

**Solution:**
- Check Unity Console for errors
- Verify local server is running
- Check WebView URL is correct

#### Issue: "No WebView component found"
**This means:**
- Using fallback TextMeshPro display (should still work)
- Vuplex WebView not installed
- WebView component not attached

**Solution:**
- The system will use TextMeshPro as fallback (black text on white background)
- This works without any WebView asset
- To use actual WebView, install Vuplex WebView asset

#### Issue: HTML not loading
**Check:**
1. Is local server running? (Step 3)
2. Can you access URL in browser? (Step 3)
3. Are there CORS errors in browser console?
4. Is the URL path correct?

### Step 7: Enable Debug Logging

1. **Select GameObject** with `WebViewManager`
2. **In Inspector**, check **"Enable Debug Log"**
3. **Run scene** - you'll see detailed logs in Console

### Step 8: Test Without WebView (Fallback Mode)

If WebView isn't working, the system automatically uses TextMeshPro:

1. **JSON will display** as black text on white background
2. **Slider values** will show as numbers
3. **No HTML/JavaScript** - just simple text display

This is the **test mode** and works immediately without any setup.

---

## Quick Checklist

- [ ] Unity Console shows WebView initialization logs
- [ ] Local server is running (if using localhost)
- [ ] HTML file exists at correct path
- [ ] WebView URL is configured correctly
- [ ] "Enable Debug Log" is checked in Inspector
- [ ] Test WebView Load shows component found

---

## Next Steps

If WebView still isn't loading:
1. Share the Unity Console logs
2. Check if you have Vuplex WebView installed
3. Try the fallback TextMeshPro mode (should work automatically)
4. Verify the HTML file is correct by opening it in a browser

