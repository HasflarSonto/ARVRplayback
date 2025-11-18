# UI Setup Guide - Step by Step

This guide will help you repurpose your existing UI elements for the Interaction Recording system.

## Location of Setup Guide

The detailed setup guide is located at:
- **Assets/Scripts/InteractionRecording/SETUP_GUIDE.md**
- **Assets/Scripts/InteractionRecording/README.md**

## Step-by-Step UI Setup

### Step 1: Identify Your Existing UI Elements

Based on your scene, you have these UI elements you can repurpose:

#### Buttons Available:
1. **Button Front** (ID: 16980) - Can be used for Record button
2. **Button Back** (ID: 17074) - Can be used for Playback button  
3. **Text Poke Button Special** (ID: 17218) - Can be used for Reset button

#### Text Elements Available:
1. **Text (TMP)** - Multiple instances available
2. **Modal Text** - Can be used for instructions
3. **Step Indicator** text elements

#### UI Cards/Panels:
- **Card 1-7** - You can repurpose these for different UI states
- **CoachingCardRoot** - Main UI container

### Step 2: Find UI Elements in Unity

1. **Open your SampleScene in Unity**
2. **In the Hierarchy window**, expand the **UI** GameObject (ID: 2046629158)
3. **Look for these GameObjects:**
   - "Button Front" or search for buttons
   - "Button Back" 
   - "Text Poke Button Special"
   - Text elements (search for "Text (TMP)")

### Step 3: Repurpose Existing Buttons (Option A - Recommended)

**If you want to use existing buttons:**

1. **Find "Button Front"** in Hierarchy
   - This will become your **Record Button**
   - Note its location/path

2. **Find "Button Back"** in Hierarchy
   - This will become your **Playback Button**
   - Note its location/path

3. **Find "Text Poke Button Special"** or create a new button
   - This will become your **Reset Button**
   - Note its location/path

### Step 4: Create New Buttons (Option B - If you prefer)

**If you want to create new buttons:**

1. **Right-click in Hierarchy** → **UI** → **Button - TextMeshPro**
2. **Name it "RecordButton"**
3. **Repeat for:**
   - "PlaybackButton"
   - "ResetButton"
4. **Position them** where you want in your UI

### Step 5: Create Text Elements

1. **Right-click in Hierarchy** → **UI** → **Text - TextMeshPro**
2. **Create these text elements:**
   - **"StatusText"** - Will show "RECORDING", "PLAYBACK", or "IDLE"
   - **"ProgressText"** - Will show recording duration or playback status
   - **"InstructionText"** - Will show instructions to user

3. **Position them** in your UI layout

### Step 6: Create UI Panels (Optional)

**If you want different panels for different modes:**

1. **Right-click in Hierarchy** → **UI** → **Panel**
2. **Create:**
   - **"RecordingPanel"** - Shown during recording
   - **"PlaybackPanel"** - Shown during playback
   - **"IdlePanel"** - Shown when idle

3. **Or repurpose existing Cards:**
   - Use Card 1 for Recording mode
   - Use Card 2 for Playback mode
   - Use Card 3 for Idle mode

### Step 7: Create the UI Controller GameObject

1. **Right-click in Hierarchy** → **Create Empty**
2. **Name it "InteractionRecordingUI"**
3. **Add Component** → **InteractionRecordingUIController**

### Step 8: Assign UI References

1. **Select "InteractionRecordingUI"** in Hierarchy
2. **In the Inspector**, find the **InteractionRecordingUIController** component
3. **Expand each section** and drag/drop:

#### Managers Section:
- **Recording Manager**: Drag "InteractionRecordingSystem" (or leave null to auto-find)
- **Playback Manager**: Drag "InteractionRecordingSystem" (or leave null to auto-find)

#### UI Buttons Section:
- **Record Button**: Drag your Record button (Button Front or new button)
- **Playback Button**: Drag your Playback button (Button Back or new button)
- **Reset Button**: Drag your Reset button (Text Poke Button Special or new button)

#### UI Text Elements Section:
- **Status Text**: Drag your StatusText GameObject
- **Progress Text**: Drag your ProgressText GameObject
- **Instruction Text**: Drag your InstructionText GameObject

#### UI Panels Section (Optional):
- **Recording Panel**: Drag your RecordingPanel (or Card 1)
- **Playback Panel**: Drag your PlaybackPanel (or Card 2)
- **Idle Panel**: Drag your IdlePanel (or Card 3)

### Step 9: Update Button Text (Optional)

**To change button labels:**

1. **Select each button** in Hierarchy
2. **Find the TextMeshProUGUI component** on the button (usually a child object)
3. **Change the text:**
   - Record Button → "Start Recording"
   - Playback Button → "Start Playback"
   - Reset Button → "Reset"

### Step 10: Test the UI

1. **Press Play**
2. **Click the Record button** - Should start recording
3. **Click the Playback button** - Should start playback (if you've recorded something)
4. **Click the Reset button** - Should reset everything

## Quick Reference: Finding UI Elements

### In Unity Hierarchy:

1. **Open Hierarchy window**
2. **Expand "UI" GameObject**
3. **Search for:**
   - Type "Button" in search box → Find buttons
   - Type "Text" in search box → Find text elements
   - Type "Card" in search box → Find card panels

### Using Scene View:

1. **Select "UI" GameObject** in Hierarchy
2. **Look in Scene view** - UI elements should be visible
3. **Click on elements** to identify them

## Troubleshooting

### "Can't find buttons"
- Make sure you're looking in the **UI** GameObject hierarchy
- Use the **search box** in Hierarchy window
- Check if buttons are **disabled** (grayed out in hierarchy)

### "Buttons not working in VR"
- Make sure buttons have **XR Poke Interactor** or similar VR interaction component
- Check that buttons are on a **Canvas** with **XR UI Input Module**
- Verify the **EventSystem** has **XRUIInputModule** component

### "Text not updating"
- Make sure TextMeshProUGUI components are assigned
- Check that text GameObjects are **active** (not disabled)
- Verify the UI Controller is finding the managers

### "Can't assign references"
- Make sure you're dragging from **Hierarchy**, not Project
- Check that GameObjects are in the **same scene**
- Verify component names match (Button, TextMeshProUGUI, etc.)

## Next Steps

Once UI is set up:
1. Test recording functionality
2. Test playback functionality
3. Adjust UI positioning/layout as needed
4. Customize colors and styling

## Need More Help?

If you're still having trouble:
1. Take a screenshot of your Hierarchy window showing the UI structure
2. Let me know which specific elements you can't find
3. I can create a helper script to automatically find and assign UI elements

