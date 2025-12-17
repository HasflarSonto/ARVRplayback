# Task JSON Structure Specification

## Overview

This document defines the JSON structure for storing VR task instructions. The structure starts with basic PickUp/PutDown actions (currently implemented) and is designed to be extensible for future features.

## Current Implementation (Phase 1)

### Basic Actions
- **PickUp**: Object is grabbed
- **PutDown**: Object is placed at exact position/rotation

### File Format
- **Location**: `Application.persistentDataPath/Tasks/`
- **Naming**: `task_[taskname]_[timestamp].json`
- **Example**: `task_BuildCastle_20240101120000.json`

---

## JSON Structure

### Top-Level Structure

```json
{
  "taskName": "Build Simple Structure",
  "version": "1.0",
  "createdAt": "2024-01-01T12:00:00Z",
  "lastModified": "2024-01-01T12:00:00Z",
  "totalDuration": 12.5,
  "steps": []
}
```

### Field Descriptions

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `taskName` | string | Yes | Human-readable name of the task |
| `version` | string | Yes | Format version (for compatibility) |
| `createdAt` | string (ISO 8601) | Yes | When task was created |
| `lastModified` | string (ISO 8601) | Yes | When task was last edited |
| `totalDuration` | float | Yes | Total recording duration in seconds |
| `steps` | array | Yes | List of instruction steps |

---

## Instruction Steps (Phase 1 - Current)

### Step Structure (Base)

All steps share these common fields:

```json
{
  "stepNumber": 1,
  "action": "PickUp",
  "objectId": "817075155",
  "objectName": "Square Block",
  "timestamp": 0.5
}
```

### Common Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `stepNumber` | integer | Yes | Sequential step number (1-based) |
| `action` | string | Yes | Action type (see Action Types below) |
| `objectId` | string | Yes | Unique object identifier (Unity instance ID) |
| `objectName` | string | Yes | Human-readable object name |
| `timestamp` | float | Yes | Time in recording when action occurs (seconds) |

---

## Action Types

### 1. PickUp

**Description**: Object is grabbed/picked up by the user.

**Fields**:
- All common fields
- No additional fields required

**Example**:
```json
{
  "stepNumber": 1,
  "action": "PickUp",
  "objectId": "817075155",
  "objectName": "Square Block",
  "timestamp": 0.5
}
```

---

### 2. PutDown

**Description**: Object is placed at an exact position and rotation.

**Fields**:
- All common fields
- `position` (required): 3D position where object is placed
- `rotation` (required): Quaternion rotation of object
- `tolerance` (optional): Placement accuracy thresholds

**Example**:
```json
{
  "stepNumber": 2,
  "action": "PutDown",
  "objectId": "817075155",
  "objectName": "Square Block",
  "timestamp": 3.2,
  "position": {
    "x": 1.0,
    "y": 0.0,
    "z": 2.0
  },
  "rotation": {
    "x": 0.0,
    "y": 0.0,
    "z": 0.0,
    "w": 1.0
  },
  "tolerance": {
    "distance": 0.1,
    "rotation": 15.0
  }
}
```

**Tolerance Fields** (optional):
- `distance`: Maximum distance from target position (meters)
- `rotation`: Maximum rotation difference (degrees)

---

## Complete Example (Phase 1)

```json
{
  "taskName": "Build Simple Structure",
  "version": "1.0",
  "createdAt": "2024-01-01T12:00:00Z",
  "lastModified": "2024-01-01T12:00:00Z",
  "totalDuration": 12.5,
  "steps": [
    {
      "stepNumber": 1,
      "action": "PickUp",
      "objectId": "817075155",
      "objectName": "Square Block",
      "timestamp": 0.5
    },
    {
      "stepNumber": 2,
      "action": "PutDown",
      "objectId": "817075155",
      "objectName": "Square Block",
      "timestamp": 3.2,
      "position": {
        "x": 1.0,
        "y": 0.0,
        "z": 2.0
      },
      "rotation": {
        "x": 0.0,
        "y": 0.0,
        "z": 0.0,
        "w": 1.0
      },
      "tolerance": {
        "distance": 0.1,
        "rotation": 15.0
      }
    },
    {
      "stepNumber": 3,
      "action": "PickUp",
      "objectId": "817075156",
      "objectName": "Triangle Block",
      "timestamp": 4.0
    },
    {
      "stepNumber": 4,
      "action": "PutDown",
      "objectId": "817075156",
      "objectName": "Triangle Block",
      "timestamp": 6.8,
      "position": {
        "x": 1.5,
        "y": 0.5,
        "z": 2.0
      },
      "rotation": {
        "x": 0.0,
        "y": 0.707,
        "z": 0.0,
        "w": 0.707
      }
    }
  ]
}
```

---

## Future Extensions (Phase 2+)

The structure is designed to be extensible. Future action types will follow the same pattern:

### Reserved Action Types (Not Yet Implemented)

- `Move`: Object movement between PickUp and PutDown
- `PlaceZone`: Zone-based placement (instead of exact position)
- `AlignRelative`: Alignment relative to another object
- `MotionRelative`: Motion pattern (shake, stir, etc.)
- `Contact`: Object contacts another object
- `Overlap`: Object overlaps with another object

### Future Top-Level Fields

These will be added when needed:

```json
{
  "zones": [],        // Zone definitions for PlaceZone actions
  "groups": [],       // Loop/group definitions
  "metadata": {}      // Additional task metadata
}
```

---

## Data Type Definitions

### Vector3
```json
{
  "x": 1.0,
  "y": 0.0,
  "z": 2.0
}
```

### Quaternion
```json
{
  "x": 0.0,
  "y": 0.0,
  "z": 0.0,
  "w": 1.0
}
```

**Note**: Unity uses (x, y, z, w) format for quaternions.

---

## Generation Workflow

### When Recording Finishes

1. Extract grab/release events from `RecordingData`
2. Match grab-release pairs (current system already does this)
3. For each pair:
   - Create `PickUp` step from grab event
   - Create `PutDown` step from release event (with position/rotation)
4. Build `TaskInstruction` object
5. Serialize to JSON
6. Save to `Application.persistentDataPath/Tasks/`

### When User Edits

1. Load TaskData JSON
2. User modifies steps (future: convert actions, adjust positions, etc.)
3. Update `lastModified` timestamp
4. Save updated JSON

---

## Playback Mapping

### JSON → Playback System

The JSON structure maps directly to the current playback system:

- **PickUp** → Highlight object, wait for user to grab
- **PutDown** → Show ghost at position/rotation, validate placement

### Current System Compatibility

The JSON structure is designed to work with:
- `InteractionPlaybackManager` - Uses `InteractionSequence` (grab-release pairs)
- `VisualCueManager` - Shows highlights and ghost objects
- `RecordingPlaybackEditor` - Timeline playback

---

## Version History

### Version 1.0 (Current)
- Basic PickUp/PutDown actions
- Position and rotation data
- Tolerance settings
- Extensible structure for future features

---

## Notes

- All timestamps are in seconds from recording start
- Object IDs use Unity's instance ID (as string)
- JSON is human-readable for debugging
- Structure is forward-compatible (new fields can be added)
- Missing optional fields use default values

---

## Implementation Checklist

- [x] Define basic structure (PickUp/PutDown)
- [x] Design extensible format
- [ ] Create C# data classes
- [ ] Implement JSON serialization
- [ ] Implement JSON deserialization
- [ ] Generate JSON after recording
- [ ] Load JSON for playback
- [ ] Load JSON for editing
- [ ] Save JSON after editing

