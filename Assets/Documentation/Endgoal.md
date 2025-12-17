Proposed System for AR playback:
The goal of this system is to let a user perform a task once in VR and convert that demonstration into clear, step by step instructions. The core idea is to automatically extract only the simplest, most reliable events, then let the user refine and enrich them in a VR based editor.

Recording Phase: 
The system captures all relevant interaction data in VR including hand and controller transforms, object transforms, grab and release events, collision states, and object to object spatial relationships to produce a precise record of the demonstrated task.

Instruction Extraction Phase (Rule-Based): 
The recording is segmented into basic steps using explicit event triggers such as pick up and release. From this, the system only generates a linear sequence of primitive actions: PickUp, Move, Place, and any direct interactions that occur between pick up and placement such as contacts or overlaps with other objects. This minimal sequence already supports a simple step by step playback where the user sees what to grab and where to put it, but without any higher level abstractions or inferred intent.

Instruction Editing and Playback Phase: 
Inside VR, the user refines these primitive steps through a timeline editor. Here they can convert raw placements into PlaceZone actions, define AlignRelative relationships between objects, create MotionRelative steps such as shaking over a bowl, and group repeated placements into a set where order can be flexible or explicitly enforced.
They can also adjust placement zones, ghost orientations, and ordering using direct manipulation tools. They can also choose which movements to include, and clip pieces to serve as target movements.
Playback then renders the final instructions using highlights, ghost objects, motion cues, and zone indicators, with steps completing when the defined spatial or motion conditions are met.




Usage Samples:

1. Shaking a Salt Shaker Over a Bowl (MotionRelative example)
Recording:
 The user picks up the shaker, moves it above a bowl, and performs a repeated shaking motion before placing it down.
Extraction:
 Automatically generates: PickUp(shaker), Move(shaker), PlaceExact(shaker).
 Shaking is not automatically inferred.
Editing:
 The user clips the repeated motion segment and converts it into a MotionRelative action. They define the bowl as the reference object and adjust the shaking zone above the bowl. The placement at the end remains PlaceExact.
Playback:
 The shaker is highlighted, a ghost appears above the bowl showing the target zone, and an animation guide prompts the shaking motion. The step completes once the system detects the required oscillation within the defined region.
2. Building a Toy Castle (Order Dependent assembly)
Recording:
 The user picks up a base block, places it. Then picks up a second block, stacks it on the first. Then adds turrets and decorative pieces in a strict order.
Extraction:
 Automatically generates: PickUp(block), Move(block), PlaceExact(block) for each action in sequence.
Editing:
 The user keeps all steps in strict order because each placement depends on the previous structure. They refine placements by adjusting ghost orientations, add AlignRelative steps for snapping turrets to corners, and add PlaceExact corrections for precise alignment.
Playback:
 Ghosts show the exact position for each block. Steps must be completed in sequence. Each placement confirms when the object matches the target pose within tolerance.

3. Placing Balls into a Bin (Order Agnostic loop)
Recording:
 The user picks up several balls one by one, puts them into a bin, then stops.
Extraction:
 Automatically generates: PickUp(ball), Move(ball), PlaceExact(ball) for each ball.
Editing:
 The user converts each exact placement into a PlaceZone action tied to the bin’s interior volume. They group all ball placements into a loop and mark the loop as order agnostic. The user also expands the zone to include the full opening of the bin.
Playback:
 Any ball can be picked up first. The bin is highlighted with a glowing zone, and a ball placement is counted complete once it enters the valid region. The loop ends once all balls are placed.
