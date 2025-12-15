# Timeline Editor Integration Issues & Solutions

## Overview

This document outlines three critical issues preventing the timeline editor from working correctly in Unity, along with detailed solutions for each.

---

## Issue 1: Recording Data Not Loading into Timeline When Recording Stops

### Problem Description

When recording stops, the timeline editor should automatically load with the generated task JSON and recording data. Currently, this is not happening.

### Root Cause Analysis

1. **Message Sending Issue**: In `WebViewManager.cs`, the `PostMessage` method is being called but the message may not be reaching the WebView if:
   - The WebView isn't fully initialized when `DisplayJSON()` is called
   - The message is sent before the WebView's JavaScript message listener is ready
   - The WebView component reference is null or invalid

2. **Timing Issue**: `OnRecordingStopped()` in `SimpleInteractionUIController.cs` calls `GenerateAndDisplayJSON()` immediately, but the WebView may not be ready to receive messages yet.

3. **Edit Mode Not Active**: The timeline editor is only visible when edit mode is active, but `GenerateAndDisplayJSON()` is called when recording stops, which may be before edit mode is activated.

### Solution Strategy

1. **Ensure WebView Initialization Before Sending Messages**:
   - Add a check to verify the WebView is initialized before sending messages
   - Wait for WebView initialization if needed (use async/await or coroutine)
   - Add a queue system to store messages that arrive before WebView is ready

2. **Delay Message Sending**:
   - Add a small delay (0.5-1 second) after WebView initialization before sending data
   - Use Unity's `Invoke()` or coroutine to delay the message sending

3. **Verify Message Format**:
   - Ensure the JSON escaping is correct (double escaping may be breaking the JSON)
   - Test the message format by logging it before sending
   - Verify the message listener in the timeline editor is correctly parsing the message

4. **Connect to Edit Mode Activation**:
   - Move the `GenerateAndDisplayJSON()` call to when edit mode is activated, not when recording stops
   - OR ensure edit mode is automatically activated when recording stops
   - Store the recording data and send it to the timeline editor when edit mode panel becomes visible

### Implementation Steps

1. **Add WebView Ready Check**:
   - Create a boolean flag `isWebViewReady` in `WebViewManager`
   - Set it to true after WebView initialization completes
   - Only send messages if `isWebViewReady` is true

2. **Add Message Queue**:
   - Create a queue to store pending messages
   - When WebView becomes ready, send all queued messages
   - Clear the queue after sending

3. **Modify SimpleInteractionUIController**:
   - Move `GenerateAndDisplayJSON()` call from `OnRecordingStopped()` to `OnEditButtonClicked()` when entering edit mode
   - OR add a call in `UpdateUIState()` when edit mode panel becomes active

4. **Add Debug Logging**:
   - Log when messages are sent
   - Log when messages are received in the timeline editor
   - Add console.log in timeline editor to verify message reception

---

## Issue 2: Finding the Procedural JSON Generation System

### Problem Description

The user wants to locate the exact system that generates procedural JSON (PickUp/PlaceExact actions) for the playback system, which should already be provided to the WebView.

### Root Cause Analysis

1. **JSON Generation Location**: The procedural JSON generation happens in `TaskInstructionGenerator.GenerateFromRecording()`:
   - Located in `Assets/Scripts/InteractionRecording/TaskInstructionGenerator.cs`
   - Converts `RecordingData` (grab/release events) into `TaskInstruction` (PickUp/PutDown steps)
   - Called from `SimpleInteractionUIController.GenerateAndDisplayJSON()`

2. **Current Flow**:
   - Recording stops → `OnRecordingStopped()` fires
   - `GenerateAndDisplayJSON()` is called
   - `TaskInstructionGenerator.GenerateFromRecording()` creates task from recording
   - `TaskInstructionGenerator.ToFormattedJSON()` converts to JSON string
   - `WebViewManager.DisplayJSON()` sends to WebView

3. **Why It's Not Working**:
   - The JSON is being generated correctly (as evidenced by previous working state)
   - The issue is likely in the message delivery (see Issue 1)
   - OR the timeline editor isn't receiving/parsing the message correctly

### Solution Strategy

1. **Verify JSON Generation**:
   - Add debug logging in `TaskInstructionGenerator.GenerateFromRecording()` to verify it's being called
   - Log the generated JSON before sending to WebView
   - Verify the JSON structure matches what the timeline editor expects

2. **Verify Message Delivery**:
   - Check that `DisplayJSON()` is being called with valid data
   - Verify the message format sent to WebView matches what the timeline editor expects
   - Check that the WebView's message listener is correctly set up

3. **Test Standalone**:
   - Test the timeline editor standalone (localhost) to verify it can load JSON correctly
   - Compare the JSON format from Unity with the sample JSON files

### Implementation Steps

1. **Add Comprehensive Logging**:
   - Log in `GenerateFromRecording()` when task is created
   - Log in `ToFormattedJSON()` the JSON string length
   - Log in `DisplayJSON()` the message being sent
   - Log in timeline editor JavaScript when message is received

2. **Verify Data Flow**:
   - Ensure `currentRecording` is not null when `GenerateAndDisplayJSON()` is called
   - Ensure `objectStateManager` is found and not null
   - Ensure the generated task has steps (not empty)

3. **Check Message Format**:
   - The message should be: `{"type":"loadTimelineData","taskJSON":"...","recordingJSON":"...","totalDuration":...}`
   - Verify JSON escaping is correct (may need to use `JsonUtility.ToJson()` for the entire message instead of string concatenation)

---

## Issue 3: Cannot Interact with Website (Buttons Not Clickable)

### Problem Description

The user can select text in the WebView but cannot click buttons. This suggests the WebView is receiving pointer events for text selection but not for button clicks.

### Root Cause Analysis

1. **Canvas Sorting Order Issue**:
   - The WebView's Canvas has `sortingOrder = -1` (behind other UI elements)
   - This may be preventing pointer events from reaching the WebView
   - Other UI elements (like the slider) may be blocking clicks

2. **Pointer Events Blocked**:
   - The Quad GameObject may be blocking pointer events
   - The WebView may be positioned behind other UI elements that are intercepting clicks
   - The Canvas's GraphicRaycaster may not be enabled on the WebView's Canvas

3. **WebView Configuration**:
   - Vuplex WebView may need specific settings for pointer events
   - The WebView's RectTransform may not be set up correctly for interaction
   - The WebView may need to be on a separate Canvas with proper raycast settings

4. **CSS Pointer Events**:
   - The timeline editor HTML may have CSS that's blocking pointer events
   - Elements may have `pointer-events: none` set
   - Z-index issues in CSS may be preventing clicks

### Solution Strategy

1. **Fix Canvas Sorting Order**:
   - Increase the WebView Canvas `sortingOrder` to be above other UI elements (but below modal dialogs)
   - Set it to `0` or `1` instead of `-1`
   - Ensure it's above the slider but below any popup UI

2. **Ensure GraphicRaycaster is Enabled**:
   - Check that the WebView's Canvas has a `GraphicRaycaster` component
   - Ensure it's enabled
   - Verify the Canvas has `RenderMode` set correctly (likely `ScreenSpaceOverlay` or `ScreenSpaceCamera`)

3. **Check Quad Positioning**:
   - Ensure the Quad is behind the WebView in sibling order
   - Verify the Quad doesn't have a collider or GraphicRaycaster that's blocking events
   - The Quad should be purely visual (no interaction components)

4. **Verify WebView RectTransform**:
   - Ensure the WebView's RectTransform covers the entire interactive area
   - Check that anchors and sizeDelta are set correctly
   - Verify the WebView is not clipped or hidden

5. **Check CSS**:
   - Review timeline-editor.html CSS for `pointer-events: none` on interactive elements
   - Ensure buttons have `cursor: pointer` and proper z-index
   - Verify no overlay elements are blocking clicks

### Implementation Steps

1. **Adjust Canvas Sorting Order**:
   - In `PositionWebViewToMatchQuad()`, change `sortingOrder` from `-1` to `0` or `1`
   - In `TryCreateWebViewComponent()`, do the same
   - Test to ensure WebView is above slider but below other UI

2. **Add GraphicRaycaster Check**:
   - In `PositionWebViewToMatchQuad()`, check for GraphicRaycaster on WebView Canvas
   - Add it if missing, ensure it's enabled
   - Verify Canvas RenderMode is appropriate

3. **Verify Quad Setup**:
   - Ensure Quad has no colliders
   - Ensure Quad has no GraphicRaycaster
   - Set Quad's sibling index to be after WebView

4. **Review CSS**:
   - Check timeline-editor.html for any `pointer-events: none` on buttons
   - Ensure buttons have proper z-index values
   - Verify no absolute positioned overlays are blocking clicks

5. **Test Interaction**:
   - Test clicking buttons in the timeline editor
   - Test dragging timeline blocks
   - Test using the action selector popup
   - Verify all interactive elements work

---

## Implementation Priority

1. **High Priority**: Issue 3 (Button Interaction) - Blocks all user interaction with the timeline editor
2. **High Priority**: Issue 1 (Data Loading) - Prevents the timeline editor from displaying data
3. **Medium Priority**: Issue 2 (JSON Generation) - Verification and debugging of existing system

---

## Testing Checklist

After implementing fixes:

- [ ] Recording stops → Timeline editor loads with data automatically
- [ ] Edit mode activates → Timeline editor shows recording data
- [ ] All buttons in timeline editor are clickable
- [ ] Timeline blocks can be dragged
- [ ] Action selector popup works
- [ ] JSON output updates when timeline is edited
- [ ] Unity slider updates timeline playhead position
- [ ] No console errors in Unity or browser console

---

## Additional Notes

- The WebView integration uses Vuplex WebView for Unity
- The timeline editor is a standalone HTML/JavaScript application
- Communication between Unity and WebView uses `PostMessage` / `MessageEmitted` events
- JSON data is double-escaped when embedded in the message string (may need to use proper JSON serialization instead)

