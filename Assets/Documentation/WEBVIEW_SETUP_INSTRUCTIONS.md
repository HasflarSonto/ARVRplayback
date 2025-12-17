# WebView Setup Instructions

## Quick Start

### Step 1: Add WebViewManager Component

1. **In Unity Hierarchy**, find **EditPanel2 → Content → Video Player**
2. **Select "Video Player"** GameObject
3. **Add Component** → Search for `WebViewManager`
4. **In Inspector**, configure:
   - **Web View URL**: `http://localhost:8000` (for development)
   - **Web View Display Quad**: Drag the "Quad" child object here
   - **Timeline Slider**: Drag your "Video Player Slider" here
   - **Update Frequency**: 30 (updates per second)

### Step 2: Start Local Web Server

1. **Open Terminal/Command Prompt**
2. **Navigate to project root**:
   ```bash
   cd /Users/antonioli/Desktop/Vrtest
   ```
3. **Start HTTP server**:
   ```bash
   python3 -m http.server 8000
   ```
   Or if you have Node.js:
   ```bash
   npx http-server -p 8000
   ```

4. **Test in browser**: Open `http://localhost:8000/WebContent/index.html`
   - You should see "VR Task Editor" with "0.00"

### Step 3: Test Without WebView (Fallback Mode)

The WebViewManager will automatically create a **test display** if no WebView component is found:

- A TextMeshPro text will appear on the Quad
- It will show the slider value (0.00 to 1.00)
- This works immediately without any WebView asset!

### Step 4: Install Vuplex WebView (Optional - For Full Functionality)

1. **Open Unity Asset Store**
2. **Search**: "Vuplex WebView"
3. **Purchase/Download** (~$200)
4. **Import** into project
5. **Add CanvasWebViewPrefab** to Video Player GameObject
6. **WebViewManager will automatically detect it**

---

## How It Works

### Without WebView (Test Mode)

- WebViewManager creates a TextMeshPro on the Quad
- Displays slider value as text
- Updates in real-time as you move the slider

### With Vuplex WebView

- WebViewManager finds the WebView component
- Loads HTML from `http://localhost:8000/WebContent/index.html`
- Sends slider value updates to JavaScript
- JavaScript updates the number display

---

## File Structure

```
Vrtest/
├── Assets/
│   └── Scripts/
│       └── InteractionRecording/
│           └── WebViewManager.cs
└── WebContent/          (outside Unity)
    └── index.html
```

**Note**: `WebContent` folder is **outside** the Unity Assets folder so you can edit it without Unity recompiling.

---

## Development Workflow

### 1. Edit HTML/JavaScript

1. **Edit** `WebContent/index.html` in your code editor
2. **Save** the file
3. **Refresh** in browser to test: `http://localhost:8000/WebContent/index.html`
4. **In Unity**: WebView will automatically reload (or refresh manually)

### 2. Test Changes

- **Browser**: Test HTML/JS in browser first (faster iteration)
- **Unity**: Test in Unity Play mode with WebView
- **VR**: Test in VR headset (Quest 3)

---

## Troubleshooting

### Number Not Updating

- **Check**: Timeline Slider is assigned in WebViewManager
- **Check**: WebViewManager component is on Video Player GameObject
- **Check**: Console for error messages

### WebView Not Loading

- **Check**: HTTP server is running (`python3 -m http.server 8000`)
- **Check**: URL is correct (`http://localhost:8000`)
- **Check**: Browser can access `http://localhost:8000/WebContent/index.html`
- **Try**: Use file path instead: `file:///path/to/WebContent/index.html`

### Test Display Not Showing

- **Check**: Quad GameObject exists under Video Player
- **Check**: Quad has a Renderer component
- **Check**: WebViewManager has Quad assigned in Inspector

---

## Next Steps

Once basic display works:

1. **Enhance HTML**: Add more UI elements
2. **Add JavaScript**: Create interactive features
3. **Two-way communication**: Send messages from web to Unity
4. **Timeline integration**: Show task steps in web UI

---

## Notes

- **WebContent folder** is intentionally outside Unity Assets
- This allows editing without Unity recompilation
- HTML/JS can be version controlled separately
- Multiple developers can work on web and Unity simultaneously

