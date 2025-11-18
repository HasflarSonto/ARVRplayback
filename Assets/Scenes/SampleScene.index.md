# Unity Scene Index: SampleScene.unity

**Generated:** 2024-12-19  
**File:** `Assets/Scenes/SampleScene.unity`  
**Total Lines:** 17,905  
**Total GameObjects:** 183  
**Total Components:** 770

---

## Scene Overview

This is a VR (Virtual Reality) scene built with Unity's XR Interaction Toolkit. The scene contains UI elements, interactable objects, lighting setup, environment objects, and teleportation areas.

---

## Scene Settings

### Occlusion Culling Settings (ID: 1)
- **Smallest Occluder:** 5
- **Smallest Hole:** 0.25
- **Backface Threshold:** 100

### Render Settings (ID: 2)
- **Fog Enabled:** Yes
- **Fog Mode:** Exponential Squared (3)
- **Fog Density:** 0.05
- **Fog Color:** RGB(0.5, 0.5, 0.5)
- **Ambient Sky Color:** RGB(0.206, 0.461, 1.0)
- **Ambient Intensity:** 1.0
- **Default Reflection Resolution:** 128
- **Reflection Bounces:** 1

### Lightmap Settings (ID: 3)
- **Bake On Scene Load:** No
- **Baked Lightmaps:** Enabled
- **Realtime Lightmaps:** Disabled
- **Resolution:** 2
- **Bake Resolution:** 40
- **Atlas Size:** 1024
- **Bake Backend:** Enlighten (1)
- **Mixed Bake Mode:** 2

### NavMesh Settings (ID: 4)
- **Agent Type ID:** 0
- **Agent Radius:** 0.5
- **Agent Height:** 2
- **Agent Slope:** 45°
- **Agent Climb:** 0.4
- **Cell Size:** 0.167
- **Tile Size:** 256

---

## Root GameObjects

The scene has **8 root GameObjects** (objects with no parent):

1. **EventSystem** (ID: 610590590)
   - Layer: Default (0)
   - Components: Transform, EventSystem, StandaloneInputModule, XRUIInputModule

2. **Interactables** (ID: 817075155)
   - Layer: UI (5)
   - Contains all interactable objects in the scene
   - Children: Multiple interactable prefab instances

3. **UI** (ID: 2046629158)
   - Layer: UI (5)
   - Contains UI elements and canvases
   - Children: Multiple UI components

4. **Environment** (ID: 1783572213)
   - Layer: Default (0)
   - Contains environment objects
   - Children: Multiple environment elements

5. **Lighting** (ID: 1703568385)
   - Layer: Default (0)
   - Contains lighting setup
   - Children: Light Probe Group, Directional Light, Reflection Probe, Post Process Volume

6. **Teleport Area Setup** (ID: 1565887663878291440)
   - Layer: Default (0)
   - Contains teleportation area configuration
   - Children: Teleport Area GameObject

7. **Root ID: 1373153106** (Component reference, not GameObject)
8. **Root ID: 1825945902** (Component reference, not GameObject)

---

## Key GameObjects by Category

### UI Elements
- **Mask Background** (ID: 15718880) - UI masking element
- **Image Bounds** - Multiple instances for UI layout
- **Text (TMP)** - TextMeshPro text elements
- **Step Indicator** - Progress indicators
- **Card 1-7** - UI card elements
- **Modal Text** - Modal dialog text
- **CoachingCardRoot** (ID: 996) - Coaching UI root
- **Grid** (ID: 15122) - Grid layout container
- **Background** (ID: 10690) - Background elements
- **PanelOutline** (ID: 14455) - Panel outline graphics

### Interactable Objects
- **Interactables** (ID: 817075155) - Root container
  - Contains multiple interactable prefab instances
  - Various interactable objects for VR interaction

### Affordance Elements
- **Outline Affordance** - Visual feedback for interactions
- **Interaction Affordance** - Interaction indicators
- **BlendShape Affordance** - Blend shape animations
- **Color Affordance** - Color change feedback
- **Audio Affordance** - Audio feedback elements

### Navigation & Dots
- **Dot** - Navigation dots (multiple instances numbered 1-6)
- **Highlighted Dot** - Active/selected state indicators

### Lighting & Environment
- **Lighting** (ID: 1703568385)
  - **Light Probe Group** (ID: 3076)
  - **Directional Light** (ID: 13024)
  - **Reflection Probe** (ID: 12029)
  - **Post Process Volume** (ID: 11623)

- **Environment** (ID: 1783572213)
  - Contains environment geometry and objects

### Teleportation
- **Teleport Area Setup** (ID: 1565887663878291440)
  - **Teleport Area** (ID: 16643) - VR teleportation destination

### Buttons
- **Button Front** (ID: 16980)
- **Button Back** (ID: 17074)
- **Text Poke Button Special** (ID: 17218)

---

## Component Types

The scene uses various Unity and XR Interaction Toolkit components:

### Unity Core Components
- **Transform** - All GameObjects
- **RectTransform** - UI elements
- **Canvas** - UI canvases
- **CanvasRenderer** - UI rendering
- **Image** - UI images
- **TextMeshProUGUI** - Text rendering
- **Button** - UI buttons
- **EventSystem** - Input event handling
- **StandaloneInputModule** - Standard input
- **XRUIInputModule** - XR input handling

### XR Interaction Toolkit Components
- **XR Interaction Manager**
- **XR Ray Interactor**
- **XR Direct Interactor**
- **XR Interactable**
- **XR Grab Interactable**
- **XR Simple Interactable**
- **XR Socket Interactor**
- **XR Teleportation Area**
- **XR Teleportation Anchor**

### Visual & Audio
- **Light** - Lighting components
- **Light Probe Group** - Light probe setup
- **Reflection Probe** - Reflection mapping
- **Post Process Volume** - Post-processing effects
- **AudioSource** - Audio playback
- **Animator** - Animation controllers

### Physics & Collision
- **Rigidbody** - Physics bodies
- **Collider** - Collision detection
- **BoxCollider** - Box-shaped colliders
- **MeshCollider** - Mesh-based colliders

### Rendering
- **MeshRenderer** - Mesh rendering
- **MeshFilter** - Mesh data
- **SkinnedMeshRenderer** - Skinned mesh rendering
- **LineRenderer** - Line rendering

---

## Scene Hierarchy Summary

```
SampleScene
├── EventSystem (610590590)
├── Interactables (817075155)
│   ├── [Multiple interactable prefab instances]
│   └── [Various interactable objects]
├── UI (2046629158)
│   ├── [UI Canvas and elements]
│   ├── Cards (Card 1-7)
│   ├── Step Indicators
│   ├── Modal Text elements
│   └── [Various UI components]
├── Environment (1783572213)
│   └── [Environment objects and geometry]
├── Lighting (1703568385)
│   ├── Light Probe Group
│   ├── Directional Light
│   ├── Reflection Probe
│   └── Post Process Volume
└── Teleport Area Setup (1565887663878291440)
    └── Teleport Area
```

---

## Notable Features

1. **VR Interaction System**
   - Full XR Interaction Toolkit integration
   - Multiple interactable object types
   - Teleportation system

2. **UI System**
   - TextMeshPro for text rendering
   - Multiple UI cards and modals
   - Step-by-step indicators
   - Coaching UI elements

3. **Visual Feedback**
   - Multiple affordance types (outline, color, blend shape, audio)
   - Highlighted states for navigation
   - Visual indicators for interactions

4. **Lighting Setup**
   - Baked lightmaps enabled
   - Light probes for dynamic objects
   - Reflection probes for realistic reflections
   - Post-processing effects

5. **Navigation System**
   - Dot-based navigation indicators
   - Step indicators for multi-step processes
   - Modal dialogs

---

## File Structure Notes

- **Total Objects:** 183 GameObjects
- **Component Count:** 770 total components
- **Prefab Instances:** Multiple prefab instances are used throughout
- **Stripped Objects:** Some objects are marked as "stripped" (prefab references)

---

## Quick Reference: GameObject IDs

### Root Objects
- EventSystem: `610590590`
- Interactables: `817075155`
- UI: `2046629158`
- Environment: `1783572213`
- Lighting: `1703568385`
- Teleport Area Setup: `1565887663878291440`

### Key UI Elements
- Mask Background: `15718880`
- CoachingCardRoot: `996`
- Grid: `15122`
- Background: `10690`

### Lighting
- Light Probe Group: `3076`
- Directional Light: `13024`
- Reflection Probe: `12029`
- Post Process Volume: `11623`

---

*This index was automatically generated from the Unity scene file. For detailed component properties, refer to the scene file directly.*

