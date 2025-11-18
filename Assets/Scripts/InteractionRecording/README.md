# VR Interaction Recording & Playback System

This system allows you to record VR interactions and play them back as visual cues to guide users.

## Scripts Overview

### Core Scripts

1. **RecordingData.cs**
   - Data structures for storing recorded interactions
   - Serializable classes for saving/loading recordings

2. **ObjectStateManager.cs**
   - Tracks all interactable objects in the scene
   - Manages initial states for reset functionality
   - Provides object ID management

3. **InteractionRecordingManager.cs**
   - Records object movements and interactions
   - Captures grab/release events
   - Stores transform snapshots at regular intervals

4. **InteractionPlaybackManager.cs**
   - Controls playback of recorded interactions
   - Manages highlighting and ghost object display
   - Handles interaction completion tracking

5. **VisualCueManager.cs**
   - Manages object highlighting (outline, color, or material)
   - Creates and displays ghost objects at target locations
   - Handles visual feedback animations

6. **InteractablePlaybackHook.cs**
   - Component to attach to XR Grab Interactables
   - Hooks into playback system to notify when objects are grabbed/released

7. **InteractionRecordingUIController.cs**
   - Manages UI panels and buttons
   - Controls recording/playback state
   - Updates status displays

## Setup Instructions

### Step 1: Add Managers to Scene

1. Create an empty GameObject named "InteractionRecordingSystem"
2. Add the following components:
   - `ObjectStateManager`
   - `InteractionRecordingManager`
   - `InteractionPlaybackManager`
   - `VisualCueManager`

### Step 2: Configure ObjectStateManager

1. In the Inspector, assign the "Interactables" GameObject (ID: 817075155) to the `Interactables Container` field
2. Enable `Auto Find Interactables` (should be on by default)

### Step 3: Link Managers

1. In `InteractionRecordingManager`, assign the `ObjectStateManager` reference
2. In `InteractionPlaybackManager`, assign both `ObjectStateManager` and `VisualCueManager` references
3. In `VisualCueManager`, configure:
   - `Highlight Method`: Choose Outline, Color, or Material
   - `Highlight Color`: Yellow by default
   - `Ghost Color`: Green with transparency by default
   - `Ghost Scale`: 1.0 (same size as original)

### Step 4: Add Playback Hooks to Interactables

For each XR Grab Interactable in your scene:

1. Select the interactable GameObject
2. Add Component → `InteractablePlaybackHook`
3. This will automatically hook into the playback system

**OR** you can add this programmatically in a script if you prefer.

### Step 5: Setup UI

1. Find or create UI buttons for:
   - Record Button
   - Playback Button
   - Reset Button

2. Create or repurpose UI panels for:
   - Recording Panel (shown during recording)
   - Playback Panel (shown during playback)
   - Idle Panel (shown when idle)

3. Create TextMeshPro text elements for:
   - Status Text
   - Progress Text
   - Instruction Text

4. Create an empty GameObject named "InteractionRecordingUI"
5. Add the `InteractionRecordingUIController` component
6. Assign all UI references in the Inspector:
   - Managers (will auto-find if left null)
   - Buttons
   - Text elements
   - Panels

### Step 6: Create Ghost Material (Optional)

For better ghost object appearance:

1. Create a new Material
2. Set Shader to "Universal Render Pipeline/Unlit"
3. Set Surface Type to "Transparent"
4. Set Base Color to green with alpha ~0.5
5. Assign this material to `VisualCueManager` → `Ghost Material`

### Step 7: Test

1. Enter Play mode
2. Click "Start Recording"
3. Pick up and move an object
4. Click "Stop Recording"
5. Click "Start Playback"
6. The object should be highlighted
7. When you grab it, a green ghost should appear at the target location

## Usage

### Recording an Interaction

1. Click "Start Recording" button
2. Interact with objects naturally (pick up, move, place)
3. Click "Stop Recording" when done
4. The recording is stored in memory (can be extended to save to file)

### Playing Back an Interaction

1. Ensure you have a recorded interaction
2. Click "Start Playback" button
3. Objects reset to initial positions
4. The first object to interact with is highlighted
5. When you grab the highlighted object, a green ghost appears at the target location
6. Place the object at the ghost location
7. The system moves to the next interaction

### Resetting

Click "Reset" button to:
- Stop any active recording/playback
- Reset all objects to initial positions
- Clear all visual cues

## Customization

### Highlighting Methods

- **Outline**: Changes object material to highlight color (simple but effective)
- **Color**: Tints existing materials with highlight color
- **Material**: Uses a custom highlight material (requires setup)

### Recording Frequency

Adjust `Recording Frequency` in `InteractionRecordingManager`:
- Higher = more accurate but more data (default: 30fps)
- Lower = less data but less accurate

### Ghost Object Appearance

- Adjust `Ghost Color` in `VisualCueManager` for different colors
- Adjust `Ghost Scale` to make ghosts larger/smaller
- Create custom ghost material for advanced effects

## Troubleshooting

### Objects not being tracked
- Ensure `ObjectStateManager` has the correct `Interactables Container` assigned
- Check that objects have `XRGrabInteractable` component
- Verify `Auto Find Interactables` is enabled

### Ghost objects not appearing
- Check that `VisualCueManager` is assigned in `InteractionPlaybackManager`
- Verify `InteractablePlaybackHook` is on the interactable objects
- Check console for errors

### UI not responding
- Ensure all UI references are assigned in `InteractionRecordingUIController`
- Check that buttons have `XR Poke Interactor` or similar for VR interaction
- Verify managers are found (they auto-find if references are null)

### Highlighting not visible
- Try different `Highlight Method` in `VisualCueManager`
- Adjust `Highlight Color` for better visibility
- Check that objects have renderers

## Future Enhancements

- Save/load recordings to files
- Multiple recording slots
- Timeline scrubber for playback
- Path visualization (trail showing movement)
- Audio cues
- Step-by-step breakdown display

