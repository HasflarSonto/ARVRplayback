# VR Task Editor - Web Development

This directory contains the web interface for the VR Task Editor that displays in Unity's WebView.

## Quick Start

### Option 1: Python (Recommended - Works on Mac/Linux/Windows)

```bash
# Navigate to this directory
cd WebContent

# Start the development server
python3 -m http.server 8000

# Open in browser
# http://localhost:8000/index.html
```

### Option 2: Node.js

```bash
# Install http-server globally (one time)
npm install -g http-server

# Start the development server
http-server -p 8000

# Open in browser
# http://localhost:8000/index.html
```

### Option 3: VS Code Live Server

1. Install the "Live Server" extension in VS Code
2. Right-click on `index.html`
3. Select "Open with Live Server"

## Testing Without Unity

The `test.html` file simulates Unity messages so you can test the interface without running Unity:

```bash
# Start server (using any method above)
# Then open: http://localhost:8000/test.html
```

## File Structure

- `index.html` - Main web interface (used in Unity)
- `test.html` - Test page with simulated Unity messages
- `README.md` - This file

## Development Workflow

1. **Edit files** in this directory
2. **Test locally** using one of the server methods above
3. **Copy to Unity** when ready:
   ```bash
   cp index.html ../Assets/StreamingAssets/WebContent/index.html
   ```

## Unity Integration

The website communicates with Unity via:
- **Unity → WebView**: `PostMessage()` sends JSON messages
- **WebView → Unity**: `vuplex.postMessage()` or `window.postMessage()`

### Message Format

```javascript
// Unity sends:
{
  "type": "sliderValue",
  "value": 0.5,
  "currentTime": 10.5,
  "totalDuration": 20.0
}

// Or:
{
  "type": "displayJSON",
  "json": "{...}",
  "totalDuration": 20.0
}
```

## Browser Testing

The website is designed for WebView, but you can test in a regular browser:
- Open `test.html` for a simulated Unity environment
- Use browser DevTools (F12) to see console messages
- Test JSON syntax highlighting and step highlighting

