# Edit Mode Setup Guide

## Overview
The Edit Mode allows you to view recorded interactions with a timeline scrubber, similar to a video player. You can see a model of the person (headset and controllers) moving objects, pause, and scrub through the recording.

## UI Setup Instructions

### Step 1: Create Timeline UI Panel

1. **In Unity Hierarchy**, right-click → **UI → Panel** → Name it "EditModePanel"
   - Unity will automatically create a Canvas if one doesn't exist
   - The panel will be created as a child of the Canvas
2. Set EditModePanel to **inactive** (unchecked in Inspector) - it will be shown when Edit mode is active
3. **IMPORTANT - Canvas Setup:**
   - **Select the Canvas** that contains EditModePanel (should be auto-created or find existing one)
   - **Render Mode:** Should be **"Screen Space - Overlay"** (recommended) - this keeps UI with current user
   - **The Canvas should NOT be a child of XR Origin, Camera, or any moving object**
   - If Canvas is a child of XR Origin, **move it to root level** (drag it out of XR Origin in Hierarchy)
4. **Configure RectTransform:**
   - Select EditModePanel
   - Set **Anchors** to bottom-center (or your preferred position)
   - Set initial **Width** and **Height** (e.g., 800x300)
   - **Position** it where you want the panel to appear
5. **If Panel is Not Showing:**
   - **Add `EditModePanelFixer` component** to EditModePanel
   - **Right-click the component** → Select **"Fix Panel Setup"**
   - This will automatically ensure the panel is a child of a Canvas and properly configured
6. **Optional - Add UIPanelAnchor component:**
   - Add `UIPanelAnchor` component to EditModePanel or its Canvas
   - This ensures the panel stays with the current user, not the recorded player

### Step 2: Create Timeline Scrubber (Progress Bar)

1. **Inside EditModePanel**, right-click → **UI → Slider**
2. Name it "TimelineSlider"
3. **Configure the Slider:**
   - **Min Value:** 0
   - **Max Value:** 1
   - **Whole Numbers:** Unchecked
   - **Value:** 0
4. **Position it** at the bottom of the panel (like a video player timeline)

### Step 3: Create Control Buttons

1. **Inside EditModePanel**, create buttons:
   - **PlayButton** - Play/Pause toggle
   - **RewindButton** - Jump backward (optional)
   - **ForwardButton** - Jump forward (optional)
2. **Position them** near the timeline slider

### Step 4: Create Time Display Text

1. **Inside EditModePanel**, create **UI → Text - TextMeshPro**
2. Name it "TimeDisplayText"
3. **Position it** next to the timeline slider
4. Will show "00:00 / 05:23" format

### Step 5: Create Player Model Container

1. **In Hierarchy** (not in UI), create empty GameObject "RecordingPlayerModel"
2. This will hold the visual representation of the person during playback
3. **Add child GameObjects:**
   - "HeadsetModel" - Visual representation of headset (can be a simple sphere or cube)
   - "LeftControllerModel" - Visual representation of left controller
   - "RightControllerModel" - Visual representation of right controller

### Step 6: Add Resize Handle (Optional but Recommended)

1. **Inside EditModePanel**, right-click → **UI → Image** (or create empty GameObject)
2. Name it "ResizeHandle"
3. **Add Component** → `PanelResizeHandle` script
4. **Configure RectTransform:**
   - **Anchors:** Min (0, 0), Max (1, 0) - anchors to bottom
   - **Pivot:** (0.5, 0.5)
   - **Size:** Width = 0 (stretches), Height = 8 pixels
   - **Position:** Y = 4 (slightly above bottom edge)
5. **In Inspector**, configure `PanelResizeHandle`:
   - **Target Panel:** Assign EditModePanel (or leave null to auto-detect)
   - **Min Height:** 100 (minimum panel height)
   - **Max Height:** 800 (maximum panel height)
6. **Optional:** Add an Image component with a semi-transparent color to make the handle visible

### Step 7: Create RecordingPlaybackEditor GameObject

1. **In Hierarchy**, create empty GameObject → Name it "RecordingPlaybackEditor"
2. **Add Component** → `RecordingPlaybackEditor`
3. **In Inspector**, assign:
   - **Edit Mode Panel:** Drag EditModePanel GameObject here (for preventing layout jumps)
   - **Timeline Slider**: Drag your TimelineSlider from EditModePanel
   - **Play Pause Button**: Drag your PlayButton from EditModePanel
   - **Time Display Text**: Drag your TimeDisplayText from EditModePanel
   - **Play Pause Button Text**: (Optional) Drag TextMeshPro component from PlayButton if you want text on the button
   - **Player Model Container**: (Optional) Create or assign a GameObject to hold player models (will be auto-created if null)
   - **Timeline Markers Container**: (Optional) Leave null - will be auto-created as child of slider
   - **Grab Marker Color**: Red by default (for grab event markers on timeline)
   - **Release Marker Color**: Green by default (for release event markers on timeline)
   - **Marker Size**: Default is (4, 20) - width and height of timeline markers

### Step 7: Assign References to SimpleInteractionUIController

1. **Select "InteractionRecordingUI"** GameObject
2. **Find SimpleInteractionUIController** component
3. **In the "Edit Mode" section**, assign:
   - **Edit Button**: Drag your EditButton GameObject
   - **Edit Mode Panel**: Drag your EditModePanel GameObject
   - **Playback Editor**: Drag the "RecordingPlaybackEditor" GameObject you created in Step 6

## Visual Model Setup (Optional but Recommended)

### Create Simple Visual Models:

1. **For HeadsetModel:**
   - Create a **Sphere** or **Cube** (3D object, not UI)
   - Scale it to represent headset size
   - Add a **Material** with a distinct color (e.g., blue)

2. **For Controller Models:**
   - Create **Cubes** for left and right controllers
   - Scale them appropriately
   - Add materials with different colors (e.g., green for left, red for right)

3. **Parent them** to "RecordingPlayerModel" GameObject

## How It Works

1. **Click Edit Button** → Edit mode activates
2. **Recording plays back** showing:
   - Objects moving from start to end positions
   - Headset and controller models moving
3. **Timeline slider** shows progress
4. **Click Play/Pause** to control playback
5. **Drag timeline slider** to scrub through recording
6. **Time display** shows current time / total time

## Component Structure

**SimpleInteractionUIController** (on "InteractionRecordingUI" GameObject):
- Edit Button
- Edit Mode Panel
- Playback Editor (reference to RecordingPlaybackEditor GameObject)

**RecordingPlaybackEditor** (on separate "RecordingPlaybackEditor" GameObject):
- Timeline Slider
- Play Pause Button
- Time Display Text
- Play Pause Button Text (optional)
- Player Model Container (optional - auto-created if null)
- Edit Mode Panel (reference to EditModePanel GameObject - for preventing layout jumps)

## Notes

- The system uses the existing transform snapshots (recorded at 30fps) for smooth playback
- Headset and controller positions are recorded during recording
- All visual models are optional - the system will work even with simple primitives
- Player models (headset/controllers) are automatically created as simple primitives if not provided

