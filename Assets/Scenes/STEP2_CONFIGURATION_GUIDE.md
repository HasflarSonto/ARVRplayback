# Step 2: Configure Managers - Detailed Guide

This guide shows you exactly how to configure each manager component in Unity.

## Prerequisites
You should have already completed Step 1:
- Created "InteractionRecordingSystem" GameObject
- Added all 4 components to it

## How to Configure Each Manager

### 1. ObjectStateManager Configuration

1. **Select "InteractionRecordingSystem"** in the Hierarchy window
2. **In the Inspector**, find the **ObjectStateManager** component
3. **Find the "Interactables Container" field**
4. **In Hierarchy**, find the GameObject named **"Interactables"**
   - It should be at the root level or under "UI"
   - If you can't find it, search for "Interactables" in the Hierarchy search box
5. **Drag "Interactables"** from Hierarchy into the **"Interactables Container"** field in Inspector
6. **Make sure "Auto Find Interactables"** is checked ✓ (should be by default)

**What this does:** Tells the system where to find all your interactable objects.

---

### 2. InteractionRecordingManager Configuration

1. **Still on "InteractionRecordingSystem"**, find the **InteractionRecordingManager** component in Inspector
2. **Find "Stop After First Release"** checkbox
3. **Make sure it's checked ✓** (this should be enabled by default)
   - This makes recording auto-stop after you release an object
4. **"Recording Frequency"** can stay at 30 (default is fine)
5. **"Record Continuous Transforms"** should be checked ✓ (default)

**What this does:** Sets up automatic recording stop after one interaction.

**Note:** The "Object State Manager" field can be left empty - it will auto-find it.

---

### 3. InteractionPlaybackManager Configuration

1. **Still on "InteractionRecordingSystem"**, find the **InteractionPlaybackManager** component
2. **You have two options:**

   **Option A: Auto-Find (Easiest)**
   - Leave both fields empty (null)
   - The system will automatically find ObjectStateManager and VisualCueManager
   - This works if all managers are on the same GameObject

   **Option B: Manual Assignment**
   - **"Object State Manager"** field: Drag "InteractionRecordingSystem" into it
   - **"Visual Cue Manager"** field: Drag "InteractionRecordingSystem" into it
   - (Since all managers are on the same GameObject, this is the same object)

**What this does:** Links the playback system to the state and visual cue managers.

---

### 4. VisualCueManager Configuration

1. **Still on "InteractionRecordingSystem"**, find the **VisualCueManager** component
2. **Find "Highlight Method"** dropdown
   - Click the dropdown
   - Select **"Color"** (this is the easiest method to start with)
3. **Find "Highlight Color"**
   - Click the color box
   - Set to **Yellow** (or any bright color you prefer)
   - Default yellow (255, 255, 0) is fine
4. **Find "Ghost Color"**
   - Click the color box
   - Set to **Green with transparency**
   - **R:** 0
   - **G:** 255 (or 1.0)
   - **B:** 0
   - **A:** 128 (or 0.5 for 50% transparency)
5. **"Ghost Scale"** can stay at 1.0 (same size as original)
6. **"Enable Pulsing"** can stay checked if you want animated highlights

**What this does:** Sets how objects are highlighted and how ghost objects look.

---

## Visual Guide (What You Should See)

### In Unity Inspector (InteractionRecordingSystem selected):

```
┌─────────────────────────────────────────┐
│ InteractionRecordingSystem               │
├─────────────────────────────────────────┤
│ Transform                                │
│                                          │
│ ┌─ ObjectStateManager ─────────────────┐│
│ │ Interactables Container: [Interact...]││ ← Drag "Interactables" here
│ │ Auto Find Interactables: ✓            ││
│ └──────────────────────────────────────┘│
│                                          │
│ ┌─ InteractionRecordingManager ────────┐│
│ │ Object State Manager: [empty]         ││ ← Can leave empty
│ │ Recording Frequency: 30               ││
│ │ Record Continuous Transforms: ✓      ││
│ │ Stop After First Release: ✓          ││ ← Make sure this is checked!
│ └──────────────────────────────────────┘│
│                                          │
│ ┌─ InteractionPlaybackManager ─────────┐│
│ │ Object State Manager: [empty]         ││ ← Can leave empty (auto-finds)
│ │ Visual Cue Manager: [empty]          ││ ← Can leave empty (auto-finds)
│ └──────────────────────────────────────┘│
│                                          │
│ ┌─ VisualCueManager ───────────────────┐│
│ │ Highlight Material: None              ││
│ │ Ghost Material: None                 ││
│ │ Highlight Method: Color ▼            ││ ← Select "Color"
│ │ Highlight Color: [Yellow]            ││
│ │ Ghost Color: [Green 0.5 alpha]       ││ ← Set to green transparent
│ │ Ghost Scale: 1                        ││
│ │ Enable Pulsing: ✓                    ││
│ └──────────────────────────────────────┘│
└─────────────────────────────────────────┘
```

---

## Quick Checklist

After configuring, verify:

- [ ] ObjectStateManager has "Interactables" assigned
- [ ] InteractionRecordingManager has "Stop After First Release" checked
- [ ] VisualCueManager has "Highlight Method" set to "Color"
- [ ] VisualCueManager has "Ghost Color" set to green (0, 1, 0, 0.5)

---

## Troubleshooting

**"I can't find the Interactables GameObject"**
- Use the Hierarchy search box (top of Hierarchy window)
- Type "Interactables" and it should appear
- It might be under "UI" or at root level

**"The fields are grayed out"**
- Make sure you're in the Scene view, not Prefab mode
- Make sure "InteractionRecordingSystem" is selected in Hierarchy

**"I don't see the component"**
- Make sure you added all 4 components in Step 1
- Check the Inspector window is visible (Window → General → Inspector)

**"Auto-find isn't working"**
- Make sure all managers are on the same GameObject ("InteractionRecordingSystem")
- Or manually assign them by dragging "InteractionRecordingSystem" into the fields

---

## Next Step

Once Step 2 is complete, move to **Step 3: Create UI Buttons**

