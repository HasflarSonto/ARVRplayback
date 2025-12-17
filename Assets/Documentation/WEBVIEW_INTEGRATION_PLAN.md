# WebView Integration Plan

## Overview

This plan outlines how to replace the Video Player component in EditPanel2 with a WebView that can display HTML/JavaScript content and communicate with Unity. This allows for rich, interactive UI development outside of Unity.

---

## Goals

1. Replace Video Player with WebView in EditPanel2
2. Display HTML/JavaScript content in VR
3. Enable two-way communication between web content and Unity
4. Control timeline/playback from web interface
5. Allow web development workflow (edit HTML/JS without Unity)

---

## Recommended Solution: Vuplex WebView

### Why Vuplex?

- ✅ **Works on Quest 3** (Android support)
- ✅ **JavaScript ↔ Unity communication**
- ✅ **Good VR performance**
- ✅ **Active development and support**
- ✅ **Well-documented**

### Alternative: 3D WebView

- Similar features to Vuplex
- Also paid asset
- Good alternative if Vuplex doesn't fit

---

## Architecture

### Component Structure

```
EditPanel2
└── WebView Container (renamed from "Video Player")
    ├── WebView Component (Vuplex)
    ├── Video Player Slider (timeline - keep)
    └── Play Pause Button (keep)
```

### Communication Flow

```
Unity ←→ WebView ←→ JavaScript
  ↓         ↓           ↓
Timeline  Display   UI Controls
Playback  Content   Interactions
```

---

## Implementation Phases

### Phase 1: Setup and Basic Display

**Goal**: Get WebView displaying HTML content

**Steps**:
1. Purchase/import Vuplex WebView asset
2. Add WebView component to EditPanel2
3. Create basic HTML file
4. Load HTML in WebView
5. Verify display in VR

**Deliverables**:
- WebView displaying static HTML
- Basic styling visible in VR

---

### Phase 2: JavaScript ↔ Unity Communication

**Goal**: Enable two-way communication

**Steps**:
1. Set up Unity message handler
2. Set up JavaScript message handler
3. Test sending messages from Unity → JavaScript
4. Test sending messages from JavaScript → Unity
5. Create message protocol/API

**Deliverables**:
- Working message system
- Example: Unity sends timeline position, JavaScript updates UI

---

### Phase 3: Timeline Integration

**Goal**: Connect timeline slider to web content

**Steps**:
1. Unity sends timeline position to JavaScript
2. JavaScript updates timeline display in web UI
3. JavaScript sends scrub commands to Unity
4. Unity updates playback position
5. Sync play/pause state

**Deliverables**:
- Timeline slider controls web UI
- Web UI can scrub timeline
- Play/pause buttons work

---

### Phase 4: Task Data Display

**Goal**: Show task steps/actions in web UI

**Steps**:
1. Unity sends task JSON data to JavaScript
2. JavaScript parses and displays steps
3. Create step list UI in HTML
4. Highlight current step
5. Show step details

**Deliverables**:
- Web UI shows task steps
- Current step highlighted
- Step details visible

---

### Phase 5: Advanced Features

**Goal**: Rich editing interface in web

**Steps**:
1. Edit step properties in web UI
2. Reorder steps (drag and drop)
3. Convert action types (PlaceExact → PlaceZone)
4. Define zones in web interface
5. Save changes back to Unity

**Deliverables**:
- Full editing capabilities in web UI
- Changes sync to Unity
- Save/load from web interface

---

## File Structure

### Unity Side

```
Assets/
├── Scripts/
│   └── InteractionRecording/
│       ├── WebViewManager.cs (new)
│       └── WebViewMessageHandler.cs (new)
└── WebContent/ (new folder)
    └── (HTML/JS files - not in Unity, external)
```

### Web Content (External)

```
WebContent/
├── index.html
├── css/
│   └── styles.css
├── js/
│   ├── main.js
│   ├── timeline.js
│   └── unity-bridge.js
└── assets/
    └── (images, etc.)
```

**Note**: Web content can be developed separately and loaded from:
- Local file path
- HTTP server (localhost during development)
- Embedded resources (packaged with app)

---

## Communication Protocol

### Message Format

```json
{
  "type": "timelinePosition",
  "data": {
    "currentTime": 5.2,
    "totalDuration": 12.5,
    "normalizedTime": 0.416
  }
}
```

### Message Types

#### Unity → JavaScript

| Type | Purpose | Data |
|------|---------|------|
| `timelinePosition` | Update timeline | `{currentTime, totalDuration, normalizedTime}` |
| `playbackState` | Play/pause state | `{isPlaying: true/false}` |
| `taskData` | Load task JSON | `{taskName, steps: [...]}` |
| `stepUpdate` | Current step changed | `{stepNumber, action, objectName}` |

#### JavaScript → Unity

| Type | Purpose | Data |
|------|---------|------|
| `scrubTimeline` | User scrubbed timeline | `{normalizedTime: 0.5}` |
| `togglePlayPause` | User clicked play/pause | `{play: true/false}` |
| `selectStep` | User selected step | `{stepNumber: 2}` |
| `editStep` | User edited step | `{stepNumber, changes: {...}}` |

---

## Development Workflow

### Option 1: Local HTTP Server (Recommended for Development)

1. **Develop web content**:
   - Edit HTML/JS files in your editor
   - Run local HTTP server (e.g., `python -m http.server 8000`)
   - Test in browser first

2. **Test in Unity**:
   - WebView loads from `http://localhost:8000`
   - See changes immediately (refresh WebView)
   - No Unity recompilation needed

3. **Deploy**:
   - Package web files with Unity build
   - Load from `Application.streamingAssetsPath`

### Option 2: File-Based (Production)

1. **Develop web content** (same as above)
2. **Package with Unity**:
   - Place in `Assets/StreamingAssets/WebContent/`
   - Load from `Application.streamingAssetsPath`
3. **Update requires rebuild** (or use file replacement)

---

## Web UI Design Considerations

### VR-Specific

- **Large text**: Readable from distance
- **High contrast**: Clear visibility
- **Touch-friendly**: Large buttons for VR controllers
- **Minimal scrolling**: Fit content in view
- **Clear hierarchy**: Easy to scan

### Layout Suggestions

```
┌─────────────────────────┐
│   Timeline Controls     │
│  [◄] [▶] [⏸]  [00:05]   │
├─────────────────────────┤
│   Step List             │
│   ✓ 1. PickUp Square    │
│   → 2. PutDown Square   │
│     3. PickUp Triangle   │
│     4. PutDown Triangle  │
├─────────────────────────┤
│   Step Details          │
│   Action: PutDown        │
│   Object: Square Block   │
│   Position: (1, 0, 2)    │
└─────────────────────────┘
```

---

## Implementation Details

### Unity WebViewManager Script

**Responsibilities**:
- Initialize WebView component
- Load HTML content
- Send messages to JavaScript
- Receive messages from JavaScript
- Handle WebView lifecycle

**Key Methods**:
- `LoadURL(string url)` - Load web content
- `SendMessage(string type, object data)` - Send to JS
- `OnMessageReceived(string message)` - Receive from JS
- `UpdateTimeline(float time)` - Update timeline in web

### JavaScript Unity Bridge

**Responsibilities**:
- Receive messages from Unity
- Send messages to Unity
- Update UI based on Unity state
- Send user interactions to Unity

**Key Functions**:
- `unity.sendMessage(type, data)` - Send to Unity
- `unity.onMessage(type, callback)` - Receive from Unity
- `updateTimeline(time)` - Update timeline display
- `updateSteps(steps)` - Update step list

---

## Testing Strategy

### Phase 1 Testing
- [ ] WebView displays HTML
- [ ] Content is visible in VR
- [ ] Text is readable
- [ ] Layout is correct

### Phase 2 Testing
- [ ] Unity → JavaScript messages work
- [ ] JavaScript → Unity messages work
- [ ] Message format is correct
- [ ] No message loss

### Phase 3 Testing
- [ ] Timeline syncs correctly
- [ ] Scrubbing works smoothly
- [ ] Play/pause state syncs
- [ ] No lag or stuttering

### Phase 4 Testing
- [ ] Task data displays correctly
- [ ] Steps are formatted properly
- [ ] Current step highlights
- [ ] Step details show correctly

---

## Performance Considerations

### Optimization Tips

1. **Limit update frequency**:
   - Don't send timeline updates every frame
   - Throttle to 10-30 updates per second

2. **Minimize message size**:
   - Only send changed data
   - Compress large data structures

3. **Efficient rendering**:
   - Use CSS transforms for animations
   - Avoid heavy JavaScript computations
   - Optimize images/assets

4. **Memory management**:
   - Unload unused web content
   - Clear message handlers when done

---

## Security Considerations

### Content Security

- **Validate all messages**: Don't trust JavaScript blindly
- **Sanitize input**: Check data from web before using
- **Limit functionality**: Only expose necessary Unity APIs
- **Error handling**: Gracefully handle malformed messages

---

## Migration Path

### From Video Player to WebView

1. **Keep existing components**:
   - Timeline slider (still needed)
   - Play/pause button (still needed)

2. **Replace Video Player**:
   - Remove VideoPlayer component
   - Add WebView component
   - Update references in scripts

3. **Update UI Controller**:
   - Change from video controls to web controls
   - Update message handling

4. **Test thoroughly**:
   - Ensure all functionality works
   - Verify performance is acceptable

---

## Resources

### Vuplex WebView
- **Asset Store**: Search "Vuplex WebView"
- **Documentation**: https://developer.vuplex.com/
- **Pricing**: ~$200 (one-time)

### Alternative: 3D WebView
- **Asset Store**: Search "3D WebView"
- **Documentation**: Check asset page
- **Pricing**: Similar to Vuplex

### Development Tools
- **Local Server**: Python `http.server`, Node.js `http-server`, etc.
- **Web Editor**: VS Code, WebStorm, etc.
- **Testing**: Browser DevTools for initial testing

---

## Timeline Estimate

- **Phase 1** (Setup): 2-4 hours
- **Phase 2** (Communication): 4-6 hours
- **Phase 3** (Timeline): 3-5 hours
- **Phase 4** (Task Display): 4-6 hours
- **Phase 5** (Advanced): 8-12 hours

**Total**: ~21-33 hours of development time

---

## Next Steps

1. **Purchase Vuplex WebView** (or evaluate alternatives)
2. **Set up development environment** (local HTTP server)
3. **Create basic HTML structure** (outside Unity)
4. **Implement Phase 1** (basic display)
5. **Iterate through phases** (one at a time)

---

## Notes

- Web content can be developed completely outside Unity
- Changes to HTML/JS don't require Unity recompilation (if using HTTP server)
- Web UI can be version controlled separately
- Multiple developers can work on web and Unity simultaneously
- Web UI can be tested in browser before VR testing

