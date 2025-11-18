# VR Interaction Cue Recording & Playback System - Implementation Plan

## Project Overview
Create a Meta Quest 3 Unity application that allows designers to record **single VR interactions** and play them back as visual cues to guide users.

**Simplified Scope:**
- Record ONE interaction: grab an object, move it, release it
- Auto-stop recording after first release
- Playback shows highlight + green ghost at target location
- Three buttons: Record, Playback, Reset

---

## Phase 1: Core Recording System

### 1.1 Interaction Recording Manager
**Purpose:** Capture and store interaction data during recording mode.

**Key Components Needed:**
- **Recording Manager Script**
  - Toggle recording on/off
  - Track all interactable objects in the scene
  - Capture transform data (position, rotation, scale) at regular intervals
  - Record **ONE** grab/release event pair (single interaction mode)
  - **Auto-stop recording after first release**
  - Store initial state of all objects before recording starts
  - Save recording data to a serializable format

**Data Structure to Record:**
- Object ID/Reference
- Initial transform state
- Timeline of transform changes during recording
- Grab events (when object was picked up, by which hand)
- Release events (when object was dropped, at what position)
- Total recording duration

### 1.2 Object State Manager
**Purpose:** Track and manage the state of all interactable objects.

**Key Components:**
- **Object Registry**
  - Maintain a list of all XR Grab Interactables in the scene
  - Store initial positions/rotations for reset functionality
  - Assign unique IDs to each interactable for recording reference

- **State Snapshot System**
  - Capture complete scene state before recording
  - Store per-object initial transforms
  - Enable quick reset to initial state

---

## Phase 2: Playback System

### 2.1 Playback Controller
**Purpose:** Control playback of recorded interactions as visual cues.

**Key Features:**
- **Playback Manager Script**
  - Load recorded interaction data
  - Control playback state (play, pause, stop, reset)
  - Manage timeline synchronization
  - Trigger visual cues at appropriate times

- **Reset Functionality**
  - Restore all objects to their initial recorded positions
  - Reset object states (grabbed/ungrabbed)
  - Clear any visual indicators from previous playback

### 2.2 Visual Cue System

#### 2.2.1 Object Highlighting
**Purpose:** Indicate which object should be picked up next.

**Implementation Approach:**
- **Highlight Manager**
  - Use existing affordance system or create new highlighting component
  - Options for highlighting:
    - Outline shader (if available in project)
    - Glow effect using emission materials
    - Pulsing animation
    - Color change (brighten or tint)
    - Particle effects around object
  - Should be clearly visible but not obstructing
  - Toggle highlight on/off based on playback state

#### 2.2.2 Ghost Object System
**Purpose:** Show where the object should be placed (green ghost at target location).

**Implementation Approach:**
- **Ghost Object Manager**
  - Create duplicate mesh of the target object (ghost version)
  - Apply green transparent material/shader
  - Position ghost at the recorded drop location
  - Show ghost only when:
    - Playback is active
    - The highlighted object is currently being grabbed by the user
  - Hide ghost when object is released or playback stops
  - Consider using a semi-transparent shader with green tint
  - Optionally add subtle animation (pulsing, rotation) to make it more noticeable

**Ghost Object Properties:**
- Same mesh as original object
- Green tinted material (semi-transparent)
- No colliders (non-interactable)
- Positioned at recorded drop location
- Slightly offset or scaled to distinguish from real object

---

## Phase 3: UI System Reprogramming

### 3.1 UI Panel Structure Analysis
**Current UI Elements Available:**
- Multiple UI Cards (Card 1-7)
- Step Indicators
- Modal Text elements
- Video Players (already in scene)
- Panels with buttons
- CoachingCardRoot structure

### 3.2 New UI Layout Design

#### 3.2.1 Main Control Panel
**Purpose:** Primary interface for recording and playback controls.

**UI Structure:**
- Uses **Text Poke Button Special** structure
  - "Text Poke Button Special" contains "Button Front" and "Button Back" (one button, two parts)
  - Create 3 Text Poke Button Special buttons:
    1. **Record Button** (Text Poke Button Special)
    2. **Playback Button** (Text Poke Button Special)
    3. **Reset Button** (Text Poke Button Special)

**UI Elements Needed:**
- **Record Button** (Text Poke Button Special)
  - Start recording (auto-stops after first release)
  - Visual indicator when recording is active
  - Disable during playback mode

- **Playback Button** (Text Poke Button Special)
  - Start playback of recorded interaction
  - Disable during recording mode
  - Only enabled when recording exists

- **Reset Button** (Text Poke Button Special)
  - Reset all objects to initial positions
  - Clear current recording/playback state
  - Always available

- **Status Display** (TextMeshPro)
  - Show current mode (Recording/Playback/Idle)
  - Display simple status messages

- **Instruction Text** (TextMeshPro)
  - Show instructions to user
  - "Ready to record", "Recording...", "Pick up highlighted object", etc.

#### 3.2.2 Simplified UI (No Complex Panels Needed)
**Purpose:** Simple, clean interface for single interaction mode.

**UI Elements:**
- Status text (Recording/Playback/Idle)
- Instruction text (what to do next)
- Three buttons (Record, Playback, Reset)
- No complex panels needed for single interaction mode

### 3.3 Video Player Integration
**Purpose:** Utilize existing video players for demonstration or tutorial content.

**Integration Points:**
- Use video players to show example interactions (optional)
- Display recorded interaction playback as video (future enhancement)
- Tutorial videos explaining the system
- Consider using VideoTimeScrubControl script as reference for timeline control

### 3.4 UI Reprogramming Strategy
**Approach:**
1. **Repurpose Existing Cards/Panels**
   - Use Card 1-7 structure for different UI sections
   - Modify StepManager or create new manager for UI state
   - Keep existing visual style but change functionality

2. **Button Reassignment**
   - Reassign existing buttons (Button Front, Button Back) to new functions
   - Use existing button prefabs but change onClick events
   - Maintain VR interaction compatibility (XR Poke Interactor)

3. **Panel Visibility Management**
   - Show/hide panels based on mode (Recording vs Playback)
   - Use existing UI structure but control visibility programmatically
   - Maintain smooth transitions between states

---

## Phase 4: Integration with XR Interaction Toolkit

### 4.1 XR Grab Interactable Integration
**Purpose:** Ensure recording works with existing XR interaction system.

**Key Integration Points:**
- **Event Hooking**
  - Subscribe to XR Grab Interactable events:
    - OnSelectEntered (when object is grabbed)
    - OnSelectExited (when object is released)
  - Capture hand/controller information
  - Record interaction timing

- **Non-Intrusive Recording**
  - Recording should not interfere with normal interaction
  - Objects should behave normally during recording
  - Recording should be transparent to the user

### 4.2 Interaction State Management
**Purpose:** Track interaction states during recording and playback.

**States to Track:**
- Object is idle (not grabbed)
- Object is being grabbed
- Object is being moved
- Object is released
- Object is in target position (during playback)

---

## Phase 5: System Architecture

### 5.1 Core Scripts Structure

**Main Scripts Needed:**

1. **InteractionRecordingManager.cs**
   - Main controller for recording system
   - Manages recording state
   - Coordinates with other systems

2. **InteractionPlaybackManager.cs**
   - Main controller for playback system
   - Manages playback state
   - Controls visual cues

3. **ObjectStateManager.cs**
   - Tracks all interactable objects
   - Manages initial state storage
   - Handles reset functionality

4. **VisualCueManager.cs**
   - Manages highlighting system
   - Controls ghost object display
   - Handles visual feedback

5. **RecordingData.cs (ScriptableObject or Serializable Class)**
   - Data structure for storing recordings
   - Contains all recorded interaction data
   - Serializable for save/load functionality

6. **UIController.cs**
   - Manages UI state
   - Handles button interactions
   - Updates UI displays

### 5.2 Communication Flow

**Recording Mode:**
```
User Action → XR Grab Interactable Event → Recording Manager → Data Storage
```

**Playback Mode:**
```
Playback Manager → Visual Cue Manager → Highlight Object → User Grabs → Show Ghost → User Drops → Next Cue
```

---

## Phase 6: Implementation Sequence

### Step 1: Foundation (Week 1)
1. Create basic recording system
   - Object state tracking
   - Transform recording
   - Basic data storage

2. Implement reset functionality
   - Store initial states
   - Restore positions

### Step 2: Visual Cues (Week 1-2)
1. Implement highlighting system
   - Choose highlighting method
   - Apply to target objects
   - Test visibility in VR

2. Create ghost object system
   - Duplicate mesh creation
   - Green transparent material
   - Position at target location
   - Show/hide logic

### Step 3: UI Integration (Week 2)
1. Reprogram existing UI panels
   - Modify button functions
   - Update panel visibility
   - Create new UI controllers

2. Integrate with recording/playback systems
   - Connect buttons to managers
   - Update status displays
   - Test UI interactions in VR

### Step 4: Polish & Testing (Week 2-3)
1. Test complete workflow
   - Record interaction
   - Playback with cues
   - Reset functionality

2. Optimize for Meta Quest 3
   - Performance optimization
   - Visual clarity in VR
   - User experience refinement

---

## Phase 7: Technical Considerations

### 7.1 Performance Optimization
- **Recording Frequency:** Balance between accuracy and performance
  - Consider recording at fixed intervals (e.g., 30fps) rather than every frame
  - Use delta compression for transform data
  - Limit recording duration or number of objects

- **Ghost Object Rendering:**
  - Use simple materials (unlit shader)
  - Consider LOD for complex meshes
  - Limit number of visible ghosts simultaneously

### 7.2 VR-Specific Considerations
- **Visual Clarity:**
  - Ensure highlights are visible in VR environment
  - Test ghost visibility at various distances
  - Consider depth perception in VR

- **Interaction Feedback:**
  - Provide haptic feedback when appropriate
  - Clear visual feedback for state changes
  - Audio cues (optional) for mode changes

### 7.3 Meta Quest 3 Specifics
- **Platform Optimization:**
  - Test performance on Quest 3 hardware
  - Optimize draw calls
  - Consider mobile GPU limitations
  - Test hand tracking vs controller input

---

## Phase 8: Future Enhancements (Optional)

### 8.1 Advanced Features
- Save/load recordings to files
- Multiple recording slots
- Edit recorded interactions
- Playback speed control
- Multiple object interaction sequences
- Path visualization (trail showing movement path)
- Audio narration during playback
- Step-by-step breakdown of complex interactions

### 8.2 UI Enhancements
- Timeline scrubber for playback (like video player)
- Visual timeline showing interaction events
- Preview mode (show recording without requiring user interaction)
- Recording library browser

---

## Key Design Decisions Needed

1. **Highlighting Method:** Choose between outline shader, glow effect, color change, or combination
2. **Ghost Material:** Determine exact green color, transparency level, and animation style
3. **Recording Format:** ScriptableObject vs JSON vs custom binary format
4. **UI Layout:** Which existing panels to repurpose and how to organize controls
5. **Recording Granularity:** Frame-by-frame vs fixed timestep vs event-based recording
6. **Multi-Object Support:** How to handle multiple objects being moved in sequence

---

## Dependencies & Prerequisites

### Required Unity Packages
- XR Interaction Toolkit (already installed)
- XR Hands (for hand tracking, already installed)
- Input System (for VR input)
- TextMeshPro (for UI text, already installed)

### Required Assets
- Existing interactable prefabs (Cube, Sphere, Tapered, etc.)
- UI panels and buttons (already in scene)
- Video players (already in scene)
- Shaders for highlighting and ghost objects (may need to create)

---

## Testing Checklist

- [ ] Record single object interaction
- [ ] Record multiple object interactions
- [ ] Playback resets objects correctly
- [ ] Highlighting is visible and clear
- [ ] Ghost appears when object is grabbed during playback
- [ ] Ghost disappears when object is released
- [ ] UI buttons work correctly in VR
- [ ] System works with both controllers and hand tracking
- [ ] Performance is acceptable on Quest 3
- [ ] No visual glitches or artifacts
- [ ] Reset functionality works correctly
- [ ] Multiple recordings can be made sequentially

---

## Notes

- This system should be non-intrusive - users should be able to interact naturally
- Visual cues should be clear but not overwhelming
- Consider accessibility - ensure cues are visible to users with different visual capabilities
- The system should work seamlessly with existing XR Interaction Toolkit setup
- Maintain compatibility with existing scene structure and prefabs

