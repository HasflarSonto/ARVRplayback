# Simplified Single Interaction Setup Guide

## Overview
This system records **ONE interaction**: grab an object, move it, release it. Recording auto-stops after release.

## UI Structure Understanding

### Text Poke Button Special
- **Text Poke Button Special** = One button GameObject
- Contains **Button Front** and **Button Back** as children
- These are PARTS of one button, not separate buttons
- You need to create 3 Text Poke Button Special buttons:
  1. Record
  2. Playback  
  3. Reset

## Quick Setup Steps

### Step 1: Create Manager System
1. Create empty GameObject "InteractionRecordingSystem"
2. Add components:
   - `ObjectStateManager`
   - `InteractionRecordingManager`
   - `InteractionPlaybackManager`
   - `VisualCueManager`

### Step 2: Configure Managers

**📖 See detailed guide: `STEP2_CONFIGURATION_GUIDE.md`**

Quick steps:
1. **Select "InteractionRecordingSystem"** in Hierarchy
2. **ObjectStateManager**: 
   - Drag "Interactables" GameObject into "Interactables Container" field
   - Make sure "Auto Find Interactables" is checked ✓
3. **InteractionRecordingManager**: 
   - Check "Stop After First Release" ✓ (should be enabled by default)
   - Other fields can stay as default
4. **InteractionPlaybackManager**: 
   - Can leave fields empty (auto-finds managers)
   - OR drag "InteractionRecordingSystem" into both fields
5. **VisualCueManager**: 
   - Set "Highlight Method" dropdown to **"Color"**
   - Set "Ghost Color" to Green with transparency:
     - R: 0, G: 1, B: 0, A: 0.5
   - Click the color box and adjust the alpha slider to ~0.5

### Step 3: Create UI Buttons
You mentioned you can create more Text Poke Button Special buttons. Create 3:

1. **Record Button** (Text Poke Button Special)
   - Copy existing "Text Poke Button Special"
   - Rename to "RecordButton"
   - Position where you want

2. **Playback Button** (Text Poke Button Special)
   - Copy existing "Text Poke Button Special"
   - Rename to "PlaybackButton"
   - Position where you want

3. **Reset Button** (Text Poke Button Special)
   - Copy existing "Text Poke Button Special"
   - Rename to "ResetButton"
   - Position where you want

### Step 4: Create UI Controller
1. Create empty GameObject "InteractionRecordingUI"
2. Add Component → `SimpleInteractionUIController` (NOT the old one!)
3. In Inspector, assign:
   - **Record Button**: Drag your RecordButton GameObject
   - **Playback Button**: Drag your PlaybackButton GameObject
   - **Reset Button**: Drag your ResetButton GameObject
   - **Status Text**: Create or find a TextMeshPro text element
   - **Instruction Text**: Create or find a TextMeshPro text element

### Step 5: Add Playback Hooks
Use SetupHelper:
1. Create empty GameObject "SetupHelper"
2. Add Component → `SetupHelper`
3. Right-click component → "Setup All Interactables with Playback Hooks"

### Step 6: Test!
1. Press Play
2. Click Record button
3. Grab an object, move it, release it
4. Recording should auto-stop
5. Click Playback button
6. Object should highlight
7. Grab it → green ghost appears
8. Place at ghost location
9. Click Reset to try again

## How It Works

### Recording Flow:
1. Click Record → Recording starts
2. Grab any object → Grab event recorded
3. Move object → Transform data recorded
4. Release object → Release event recorded + **Recording auto-stops**

### Playback Flow:
1. Click Playback → Objects reset to initial positions
2. Object that was grabbed gets highlighted
3. User grabs highlighted object → Green ghost appears at target location
4. User places object at ghost → Interaction complete
5. Click Reset to start over

## Key Differences from Original Plan

✅ **Simplified:**
- Single interaction only (one grab-release)
- Auto-stop after release
- No complex multi-step sequences
- Simple UI (3 buttons + 2 text elements)

❌ **Removed:**
- Multiple object interactions
- Manual stop recording
- Complex UI panels
- Step-by-step indicators

## Troubleshooting

**Recording doesn't stop:**
- Check `Stop After First Release` is enabled in InteractionRecordingManager

**Ghost doesn't appear:**
- Make sure `InteractablePlaybackHook` is on the object
- Check VisualCueManager is assigned in PlaybackManager

**Buttons don't work:**
- Make sure you're using `SimpleInteractionUIController` (not the old one)
- Check buttons have Button component (or in children)
- Verify VR interaction components (XR Poke Interactor, etc.)

## Next Steps

Once this works, you can:
- Add more interactions (disable auto-stop, handle multiple)
- Save/load recordings
- Add audio cues
- Customize visual effects

