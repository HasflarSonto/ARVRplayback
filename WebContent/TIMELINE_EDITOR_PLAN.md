# Timeline Editor Website - Development Plan

## Overview
A web-based editor for refining procedurally generated VR task instructions. Combines timeline visualization with JSON editing capabilities.

## Architecture

### Layout (3-Panel Design)
1. **Left Panel (25%)**: Properties Editor
   - Edit selected action properties
   - Convert movement to goals
   - Adjust timestamps, positions, rotations

2. **Center Panel (50%)**: Timeline Editor
   - Visual timeline with action blocks
   - Drag to adjust timing
   - Add/remove/duplicate actions
   - Playhead and playback controls

3. **Right Panel (25%)**: JSON Output Display
   - Collapsible JSON structure
   - Real-time updates
   - Export functionality

## Data Flow

### Input
- **TaskInstruction JSON**: Procedurally generated (PickUp/PutDown)
- **RecordingData JSON**: Full recording data (movement, transforms, etc.)

### Processing
- Load input JSON → Display on timeline
- User edits → Update internal data structure
- Real-time sync → Update output JSON

### Output
- **Refined TaskInstruction JSON**: Final abstract task instructions

## Features

### Phase 1: Core Functionality
- [x] Load input JSON
- [x] Display actions on timeline
- [x] Collapsible JSON viewer
- [x] Basic timeline controls (play/pause/scrub)
- [x] Drag blocks to adjust timing
- [x] Properties panel for selected action

### Phase 2: Editing
- [ ] Add new actions
- [ ] Delete actions
- [ ] Duplicate actions
- [ ] Convert movement to Move/PlaceExact goals
- [ ] Edit action properties (position, rotation, tolerance)

### Phase 3: Advanced
- [ ] Group actions
- [ ] Loop detection
- [ ] Undo/redo
- [ ] Export refined JSON

## Technology Stack
- **HTML5/CSS3**: Layout and styling
- **Vanilla JavaScript**: Core functionality (no frameworks for simplicity)
- **Canvas API**: Timeline rendering
- **LocalStorage**: Auto-save drafts

## File Structure
```
WebContent/
├── index.html              # Main editor page
├── timeline-editor.html    # Timeline editor (new)
├── js/
│   ├── timeline.js         # Timeline rendering
│   ├── json-viewer.js      # Collapsible JSON display
│   ├── properties-editor.js # Properties panel
│   └── data-manager.js     # Data loading/saving
├── css/
│   └── editor.css          # Editor styles
└── samples/
    ├── sample-task.json    # Sample input JSON
    └── sample-recording.json # Sample recording data
```

