# Quick Setup Guide

## What You Need to Do

### 1. Create the Manager GameObject

1. In Unity, right-click in Hierarchy → Create Empty
2. Name it "InteractionRecordingSystem"
3. Add these 4 components:
   - `ObjectStateManager`
   - `InteractionRecordingManager`
   - `InteractionPlaybackManager`
   - `VisualCueManager`

### 2. Find Your Interactables Container

1. In the Hierarchy, find the GameObject named "Interactables" (ID: 817075155)
2. In `ObjectStateManager` component, drag "Interactables" to the `Interactables Container` field
3. Make sure `Auto Find Interactables` is checked ✓

### 3. Link the Managers Together

In the Inspector on "InteractionRecordingSystem":

1. **InteractionRecordingManager**:
   - Drag "InteractionRecordingSystem" to `Object State Manager` field (or leave null to auto-find)

2. **InteractionPlaybackManager**:
   - Drag "InteractionRecordingSystem" to `Object State Manager` field
   - Drag "InteractionRecordingSystem" to `Visual Cue Manager` field

3. **VisualCueManager**:
   - Set `Highlight Method` to "Color" (easiest to start)
   - `Highlight Color` can stay yellow
   - `Ghost Color` should be green with transparency (0, 1, 0, 0.5)

### 4. Add Hooks to Your Interactables

**Option A: Manual (for a few objects)**
1. Select each interactable object in your scene
2. Add Component → `InteractablePlaybackHook`

**Option B: Automatic (recommended)**
I can create a helper script that automatically adds this to all interactables. Let me know if you want this!

### 5. Setup UI (Repurpose Existing)

You mentioned you want to reprogram the existing UI. Here's what to do:

1. Find your existing UI panels (Card 1-7, etc.)
2. Create or find buttons for:
   - Record Button
   - Playback Button  
   - Reset Button

3. Create TextMeshPro text elements for:
   - Status display
   - Progress/Time display
   - Instructions

4. Create an empty GameObject named "InteractionRecordingUI"
5. Add Component → `InteractionRecordingUIController`
6. In the Inspector, assign:
   - All the buttons you created
   - All the text elements
   - The panels (if you want different panels for different modes)

**Note:** The managers will auto-find each other if you leave those fields null, but it's better to assign them explicitly.

### 6. Test It!

1. Press Play
2. Click "Start Recording" (your button)
3. Grab and move an object
4. Click "Stop Recording"
5. Click "Start Playback"
6. The object should highlight
7. Grab it → green ghost appears
8. Place it at ghost location

## What I Need From You

1. **Confirm the Interactables container**: Is "Interactables" (ID: 817075155) the right container? Or is there a different one?

2. **UI Setup**: 
   - Do you want me to create a helper script to automatically setup the UI?
   - Or do you prefer to manually assign the existing UI elements?

3. **Interactable Hooks**: 
   - Should I create a script that automatically adds `InteractablePlaybackHook` to all interactables?
   - Or will you add them manually?

4. **Testing**: 
   - Once you've done the setup, test it and let me know what works/doesn't work!

## Common Issues & Quick Fixes

**"ObjectStateManager not found" error:**
- Make sure the GameObject is named correctly
- Check that the component is actually added

**"No interactables found":**
- Verify the Interactables container is assigned
- Check that objects have `XRGrabInteractable` component

**Ghost objects not showing:**
- Make sure `InteractablePlaybackHook` is on the objects
- Check that `VisualCueManager` is assigned in `InteractionPlaybackManager`

**UI buttons not working:**
- Make sure buttons are assigned in `InteractionRecordingUIController`
- Check that buttons have proper VR interaction (XR Poke Interactor, etc.)

Let me know when you've done the setup and we can test together!

