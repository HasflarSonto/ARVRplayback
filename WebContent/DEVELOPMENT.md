# WebContent Development Guide

## Quick Start

### Start Development Server

```bash
cd WebContent
python3 -m http.server 8000
```

Then open in your browser:
- **Unity WebView Display**: http://localhost:8000/index.html
- **Timeline Editor**: http://localhost:8000/timeline-editor.html (NEW!)
- **Test Messages**: http://localhost:8000/test-messages.html

### Alternative: Use VS Code Live Server

1. Install "Live Server" extension in VS Code
2. Right-click on `index.html`
3. Select "Open with Live Server"

## File Structure

```
WebContent/
├── index.html              # Main HTML file (Unity WebView display)
├── timeline-editor.html    # Timeline editor (NEW - for editing tasks)
├── test-messages.html      # Test page for Unity messages
├── samples/
│   ├── sample-task.json    # Sample task instruction JSON
│   └── sample-recording.json # Sample recording data
└── DEVELOPMENT.md          # This file
```

## Testing Unity Messages

Since the website receives messages from Unity, you can test the message handling by opening the browser console and manually calling:

```javascript
// Simulate slider value update
receiveMessageFromUnity(JSON.stringify({
    type: 'sliderValue',
    value: 0.5,
    currentTime: 5.0,
    totalDuration: 10.0
}));

// Simulate JSON display
receiveMessageFromUnity(JSON.stringify({
    type: 'displayJSON',
    json: '{"taskName":"Test Task","steps":[{"stepNumber":1,"action":"PickUp","objectId":"square","timestamp":0.0}]}',
    totalDuration: 10.0
}));
```

## Development Workflow

1. **Edit `index.html`** - Make your changes
2. **Refresh browser** - See changes immediately
3. **Test in Unity** - Copy changes to `Assets/StreamingAssets/WebContent/index.html` when ready

## Copying to Unity

After making changes, copy the file to Unity's StreamingAssets:

```bash
# From project root
cp WebContent/index.html Assets/StreamingAssets/WebContent/index.html
```

Or manually copy the file in Finder/File Explorer.

## Features

### index.html (Unity WebView Display)
- **JSON Syntax Highlighting** - Automatic color coding
- **Step Highlighting** - Highlights current step based on timeline
- **Responsive Layout** - Works in VR WebView
- **Message Handling** - Receives messages from Unity via Vuplex

### timeline-editor.html (Timeline Editor - NEW!)
- **3-Panel Layout**: Properties (left), Timeline (center), JSON Output (right)
- **Visual Timeline**: Drag action blocks to adjust timing
- **Properties Editor**: Edit action details (type, position, rotation, etc.)
- **Collapsible JSON**: View output JSON with expand/collapse
- **Playback Controls**: Play/pause/scrub timeline
- **Export**: Download refined JSON for Unity playback
- **Sample Data**: Automatically loads sample task and recording data

## Browser Compatibility

Tested in:
- Chrome/Edge (recommended)
- Firefox
- Safari

Note: The Unity WebView uses the platform's native browser engine, so test in multiple browsers.

